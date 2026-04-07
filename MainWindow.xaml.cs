using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
using System.Threading;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using System.Runtime.InteropServices;
using System.Globalization;

namespace FilmAnalysis
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
    }

    public class FilmRelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Action<object?>? _executeWithParam;
        public FilmRelayCommand(Action execute) { _execute = execute; }
        public FilmRelayCommand(Action<object?> executeWithParam) { _executeWithParam = executeWithParam; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            if (_execute != null) _execute();
            else _executeWithParam?.Invoke(parameter);
        }
    }

    public class AppSettings
    {
        public int FixedWidth { get; set; } = 100;
        public int FixedHeight { get; set; } = 100;
        public bool LastWasFixed { get; set; } = false;
        public string CalibrationsPath { get; set; } = string.Empty;
        public double LastPlateauX { get; set; } = 20;
        public double LastPlateauY { get; set; } = 20;
        public string LastJawMethod { get; set; } = "Maximum";
    }

    public class CalibrationPoint
    {
        public double Dose { get; set; }
        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public ICommand BrowseCommand => new FilmRelayCommand(BrowseImage);
        public ICommand ProcessCommand => new FilmRelayCommand(ProcessImage);
        public ICommand ExportCommand => new FilmRelayCommand(ExportResults);
        public ICommand AlignmentCommand => new FilmRelayCommand(OpenAlignmentWindow);
        public ICommand JawSizeCommand => new FilmRelayCommand(OpenJawSizeWindow);
        public ICommand GammaCommand => new FilmRelayCommand(OpenGammaWindow);
        public ObservableCollection<CalibrationPoint> CalibrationPoints { get; set; }
        public CalibrationConfig CurrentConfig { get; set; }
        private readonly IContentDialogService _dialogService = new ContentDialogService();

        // Application State
        private bool _isSelectingROI = false;
        private bool _isDrawing = false;
        private bool _isFixedMode = false;
        private Point _startPoint;

        // Manual Alignment State
        private bool _isAligning = false;
        private int _alignStep = 0; // 1=Left, 2=Right, 3=Top
        private Point _alignLeft, _alignRight, _alignTop;

        // Crop / ROI Filter State
        private bool _isCropping = false;
        private bool _isROIFiltering = false;
        private AppSettings _settings = new AppSettings();

        // Measurement State
        private enum MeasurementMode { None, ROIDose, Distance, Area }
        private MeasurementMode _activeMeasurementMode = MeasurementMode.None;
        private bool _isAreaRectMode = false;
        private List<Point> _measurementPoints = new();

        // Raw High-Precision Image Data
        private double[,] _redChannel;
        private double[,] _greenChannel;
        private double[,] _blueChannel;
        private int _imgWidth, _imgHeight;
        private double _tiffDpi = 72; // Default DPI
        private string _calibrationsFolder;

        // Dosimetry States
        private double[,] _doseMap;
        private ImageSource _rawImageSource;
        private bool _isShowingDoseMap = false;

        // Undo/Redo History
        private class ImageState
        {
            public double[,] Red, Green, Blue;
            public double[,] DoseMap;
            public int Width, Height;
            public bool ShowingDose;
            public string Description;
        }
        private readonly Stack<ImageState> _undoStack = new();
        private readonly Stack<ImageState> _redoStack = new();

        public MainWindow()
        {
            InitializeComponent();
            _dialogService.SetContentPresenter(RootContentDialogPresenter);
            
            LoadSettings();

            // Setup Calibrations Folder
            _calibrationsFolder = string.IsNullOrEmpty(_settings.CalibrationsPath) 
                ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Calibrations")
                : _settings.CalibrationsPath;

            if (!System.IO.Directory.Exists(_calibrationsFolder)) 
                try { System.IO.Directory.CreateDirectory(_calibrationsFolder); } catch { }

            InitializeCalibrationData();
            RefreshConfigs();
            
            // Register for size changes to keep rulers in sync
            MasterImageContainer.SizeChanged += (s, e) => UpdateRulers();

            // Keyboard shortcuts for Undo/Redo
            InputBindings.Add(new KeyBinding(new RelayCommand(() => Undo_Click(null, null)), Key.Z, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new RelayCommand(() => Redo_Click(null, null)), Key.Y, ModifierKeys.Control));

            InitializeColorMaps();
        }

        private void InitializeColorMaps()
        {
            ColorMapDropDown.Items.Add(new ComboBoxItem { Content = "Jet", IsSelected = true });
            ColorMapDropDown.Items.Add(new ComboBoxItem { Content = "Hot" });
            ColorMapDropDown.Items.Add(new ComboBoxItem { Content = "Viridis" });
            ColorMapDropDown.Items.Add(new ComboBoxItem { Content = "Gray" });
        }

        private void LoadSettings()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
                // Fallback for migration
                if (!System.IO.File.Exists(path))
                {
                    string oldPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "roi_settings.json");
                    if (System.IO.File.Exists(oldPath)) path = oldPath;
                }

                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    _settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { /* Ignore errors on load */ }
        }

        private void SaveSettings()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
                string json = System.Text.Json.JsonSerializer.Serialize(_settings);
                System.IO.File.WriteAllText(path, json);
            }
            catch { /* Ignore errors on save */ }
        }

        private void InitializeCalibrationData()
        {
            CalibrationPoints = new ObservableCollection<CalibrationPoint>();
            CalibrationGrid.ItemsSource = CalibrationPoints;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (e.ClickCount == 2)
            {
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                return;
            }

            try
            {
                this.DragMove();
            }
            catch
            {
                // ignore drag exceptions
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            
            stackPanel.Children.Add(new System.Windows.Controls.TextBlock { 
                Text = "Calibration Configuration Folder", 
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0,0,0,5) 
            });

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pathInput = new System.Windows.Controls.TextBox { 
                Text = _calibrationsFolder, 
                IsReadOnly = true,
                Margin = new Thickness(0,0,5,0)
            };
            
            var browseBtn = new Wpf.Ui.Controls.Button { 
                Content = "Browse...", 
                Padding = new Thickness(10,5,10,5)
            };

            browseBtn.Click += (s, ev) => {
                var dlg = new Microsoft.Win32.OpenFolderDialog();
                dlg.InitialDirectory = _calibrationsFolder;
                if (dlg.ShowDialog() == true)
                {
                    ((System.Windows.Controls.TextBox)pathInput).Text = dlg.FolderName;
                }
            };

            System.Windows.Controls.Grid.SetColumn(pathInput, 0);
            System.Windows.Controls.Grid.SetColumn(browseBtn, 1);
            grid.Children.Add(pathInput);
            grid.Children.Add(browseBtn);
            stackPanel.Children.Add(grid);

            var dialog = new ContentDialog(_dialogService.GetContentPresenter())
            {
                Title = "Application Settings",
                Content = stackPanel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _settings.CalibrationsPath = pathInput.Text;
                _calibrationsFolder = _settings.CalibrationsPath;
                if (!System.IO.Directory.Exists(_calibrationsFolder))
                {
                    try { System.IO.Directory.CreateDirectory(_calibrationsFolder); } catch { }
                }
                SaveSettings();
                RefreshConfigs();
                StatusText.Text = "Settings Updated";
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*";
            var result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    var uri = new Uri(dlg.FileName);
                    var bitmap = new BitmapImage(uri);
                    MainDisplayImage.Source = bitmap; UpdateRulers();
                    
                    // Update Metadata
                    UpdateImageMetadata(dlg.FileName, bitmap.PixelWidth, bitmap.PixelHeight, 72.0); // Default DPI for standard images

                    // Clear photometry and dosimetry data if standard image is loaded
                    _redChannel = null;
                    _greenChannel = null;
                    _blueChannel = null;
                    _doseMap = null;
                    _isShowingDoseMap = false;
                    ShowDoseToggle.IsChecked = false;
                    ShowDoseToggle.IsEnabled = false;
                }
                catch
                {
                    System.Windows.MessageBox.Show("Unable to load the selected image.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void LoadRawFilm_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "TIFF files|*.tif;*.tiff|All files|*.*";
            var result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    ReadTiffData(dlg.FileName);
                    
                    // Update Metadata (DPI and Size are updated inside ReadTiffData or here)
                    long fileSize = new System.IO.FileInfo(dlg.FileName).Length;
                    MetaFileName.Text = System.IO.Path.GetFileName(dlg.FileName);
                    MetaImageSize.Text = $"{_imgWidth} x {_imgHeight}";
                    MetaDPI.Text = _tiffDpi.ToString("F1");
                    MetaFileSize.Text = $"{(fileSize / 1024.0 / 1024.0):F2} MB";

                    StatusText.Text = "Raw Film Loaded!";
                    StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22"));

                    // Reset dosimetry state
                    _doseMap = null;
                    _isShowingDoseMap = false;
                    ShowDoseToggle.IsChecked = false;
                    ShowDoseToggle.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Unable to load the raw TIFF: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void UpdateImageMetadata(string filePath, int width, int height, double dpi)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            MetaFileName.Text = fileInfo.Name;
            MetaImageSize.Text = $"{width} x {height}";
            MetaDPI.Text = dpi.ToString("F1");
            MetaFileSize.Text = $"{(fileInfo.Length / 1024.0 / 1024.0):F2} MB";
            UpdateCropUI();
        }

        private void UpdateCropUI()
        {
            if (CenterCropWidth != null) CenterCropWidth.Text = _imgWidth.ToString();
            if (CenterCropHeight != null) CenterCropHeight.Text = _imgHeight.ToString();
            if (MetaImageSize != null) MetaImageSize.Text = $"{_imgWidth} x {_imgHeight}";
        }

        private void ReadTiffData(string filePath)
        {
            _doseMap = null; // Reset dose map on new data load
            using (Tiff image = Tiff.Open(filePath, "r"))
            {
                if (image == null) throw new Exception("Could not open TIFF file.");

                FieldValue[] res = image.GetField(TiffTag.IMAGEWIDTH);
                _imgWidth = res[0].ToInt();

                res = image.GetField(TiffTag.IMAGELENGTH);
                _imgHeight = res[0].ToInt();

                res = image.GetField(TiffTag.BITSPERSAMPLE);
                short bitsPerSample = res[0].ToShort();

                res = image.GetField(TiffTag.SAMPLESPERPIXEL);
                short samplesPerPixel = res[0].ToShort();

                // DPI extraction
                res = image.GetField(TiffTag.XRESOLUTION);
                if (res != null)
                {
                    float xRes = res[0].ToFloat();
                    res = image.GetField(TiffTag.RESOLUTIONUNIT);
                    short unit = (res != null) ? res[0].ToShort() : (short)2; // 2 = Inch

                    if (unit == 3) // Centimeter
                        _tiffDpi = xRes * 2.54;
                    else
                        _tiffDpi = xRes;
                }

                // Orientation extraction
                res = image.GetField(TiffTag.ORIENTATION);
                BitMiracle.LibTiff.Classic.Orientation orientation = (res != null) ? (BitMiracle.LibTiff.Classic.Orientation)res[0].ToInt() : BitMiracle.LibTiff.Classic.Orientation.TOPLEFT;

                _redChannel = new double[_imgHeight, _imgWidth];
                _greenChannel = new double[_imgHeight, _imgWidth];
                _blueChannel = new double[_imgHeight, _imgWidth];

                if (bitsPerSample == 16)
                {
                    byte[] scanline = new byte[image.ScanlineSize()];
                    for (int row = 0; row < _imgHeight; row++)
                    {
                        image.ReadScanline(scanline, row);
                        for (int col = 0; col < _imgWidth; col++)
                        {
                            int offset = col * samplesPerPixel * 2; // 2 bytes per sample
                            // Fix: Standard TIFF is R, G, B
                            ushort r = BitConverter.ToUInt16(scanline, offset);
                            ushort g = (samplesPerPixel > 1) ? BitConverter.ToUInt16(scanline, offset + 2) : r;
                            ushort b = (samplesPerPixel > 2) ? BitConverter.ToUInt16(scanline, offset + 4) : r;
                            
                            _redChannel[row, col] = r;
                            _greenChannel[row, col] = g;
                            _blueChannel[row, col] = b;
                        }
                    }
                }
                else
                {
                    // 8-bit or other
                    int[] raster = new int[_imgWidth * _imgHeight];
                    if (!image.ReadRGBAImage(_imgWidth, _imgHeight, raster))
                    {
                        throw new Exception("ReadRGBAImage failed.");
                    }

                    for (int y = 0; y < _imgHeight; y++)
                    {
                        for (int x = 0; x < _imgWidth; x++)
                        {
                            // raster is bottom-up
                            int pixel = raster[(_imgHeight - 1 - y) * _imgWidth + x];
                            _redChannel[y, x] = Tiff.GetR(pixel);
                            _greenChannel[y, x] = Tiff.GetG(pixel);
                            _blueChannel[y, x] = Tiff.GetB(pixel);
                        }
                    }
                }

                // Handle Orientation (Flip/Rotate arrays if needed)
                ApplyOrientation(orientation);
                UpdateCropUI();

                // Create Display Bitmap (normalized to 8-bit with Gamma)
                UpdateDisplayFromRaw();
            }
        }

        private void ApplyOrientation(BitMiracle.LibTiff.Classic.Orientation orientation)
        {
            if (orientation == BitMiracle.LibTiff.Classic.Orientation.TOPLEFT || orientation == 0) return;

            int h = _redChannel.GetLength(0);
            int w = _redChannel.GetLength(1);

            // Simple handling for common orientations in dosimetry (TOPLEFT, BOTRIGHT, etc.)
            // For now, let's implement the logic to swap/flip if needed.
            // If it's BOTTOMLEFT (4), we flip V. If it's TOPRIGHT (2), we flip H.
            // Most scanner TIFFs are 1 (TOPLEFT).
            
            // TODO: Full implementation of all 8 orientations if needed.
            // For now, let's just log it or handle the most common ones.
        }

        private void UpdateDisplayFromRaw()
        {
            if (_redChannel == null) 
            {
                MainDisplayImage.Source = null;
                return;
            }

            int width = _redChannel.GetLength(1);
            int height = _redChannel.GetLength(0);
            
            // 1. Find Min/Max for Scaling (Auto-Contrast like Matlab)
            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var v in _redChannel) { if (v < min) min = v; if (v > max) max = v; }
            foreach (var v in _greenChannel) { if (v < min) min = v; if (v > max) max = v; }
            foreach (var v in _blueChannel) { if (v < min) min = v; if (v > max) max = v; }

            if (max <= min) max = min + 1;

            // Apply Contrast Adjustment
            double range = max - min;
            double center = (max + min) / 2.0;
            double contrastScale = Math.Pow(2.0, (128 - ContrastSlider.Value) / 64.0); // Exponential scale
            double newRange = range * contrastScale;
            min = center - newRange / 2.0;
            max = center + newRange / 2.0;

            // 2. Prepare Display Data with Gamma 2.2
            byte[] pixels = new byte[width * height * 4]; // BGRA
            double gamma = 1.0 / 2.2;

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int idx = (row * width + col) * 4;
                    
                    // Normalize to 0-1
                    double rN = (_redChannel[row, col] - min) / (max - min);
                    double gN = (_greenChannel[row, col] - min) / (max - min);
                    double bN = (_blueChannel[row, col] - min) / (max - min);

                    // Apply Gamma
                    rN = Math.Pow(Math.Max(0, rN), gamma);
                    gN = Math.Pow(Math.Max(0, gN), gamma);
                    bN = Math.Pow(Math.Max(0, bN), gamma);

                    pixels[idx] = (byte)(bN * 255.0);
                    pixels[idx + 1] = (byte)(gN * 255.0);
                    pixels[idx + 2] = (byte)(rN * 255.0);
                    pixels[idx + 3] = 255;
                }
            }

            PixelFormat format = PixelFormats.Bgra32;
            int stride = width * 4;
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
            MainDisplayImage.Source = bitmap; UpdateRulers();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Undo / Redo

        private static double[,] CloneArray(double[,] src)
        {
            if (src == null) return null;
            return (double[,])src.Clone();
        }

        private void PushUndo(string description)
        {
            _undoStack.Push(new ImageState
            {
                Red = CloneArray(_redChannel),
                Green = CloneArray(_greenChannel),
                Blue = CloneArray(_blueChannel),
                DoseMap = CloneArray(_doseMap),
                Width = _imgWidth,
                Height = _imgHeight,
                ShowingDose = _isShowingDoseMap,
                Description = description
            });
            _redoStack.Clear();
            UpdateUndoRedoUI();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count == 0) return;

            // Save current state to redo stack
            _redoStack.Push(new ImageState
            {
                Red = CloneArray(_redChannel),
                Green = CloneArray(_greenChannel),
                Blue = CloneArray(_blueChannel),
                DoseMap = CloneArray(_doseMap),
                Width = _imgWidth,
                Height = _imgHeight,
                ShowingDose = _isShowingDoseMap,
                Description = "Redo"
            });

            var state = _undoStack.Pop();
            RestoreState(state);
            StatusText.Text = $"Undo: {state.Description}";
            UpdateUndoRedoUI();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count == 0) return;

            // Save current state to undo stack
            _undoStack.Push(new ImageState
            {
                Red = CloneArray(_redChannel),
                Green = CloneArray(_greenChannel),
                Blue = CloneArray(_blueChannel),
                DoseMap = CloneArray(_doseMap),
                Width = _imgWidth,
                Height = _imgHeight,
                ShowingDose = _isShowingDoseMap,
                Description = "Undo"
            });

            var state = _redoStack.Pop();
            RestoreState(state);
            StatusText.Text = "Redo";
            UpdateUndoRedoUI();
        }

        private void RestoreState(ImageState state)
        {
            _redChannel = state.Red;
            _greenChannel = state.Green;
            _blueChannel = state.Blue;
            _doseMap = state.DoseMap;
            _imgWidth = state.Width;
            _imgHeight = state.Height;
            UpdateCropUI();
            _isShowingDoseMap = state.ShowingDose;

            if (_isShowingDoseMap && _doseMap != null)
            {
                MainDisplayImage.Source = GenerateDoseHeatmap();
                ShowDoseToggle.IsChecked = true;
                ShowDoseToggle.IsEnabled = true;
            }
            else
            {
                UpdateDisplayFromRaw();
                ShowDoseToggle.IsChecked = false;
            }
        }

        private void UpdateUndoRedoUI()
        {
            UndoMenuItem.IsEnabled = _undoStack.Count > 0;
            RedoMenuItem.IsEnabled = _redoStack.Count > 0;
        }

        #endregion

        private void MenuSelectCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabControl == null) return;
            MainTabControl.SelectedIndex = 0;
            if (NavCalibrationButton != null) NavCalibrationButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            if (NavDicomButton != null) NavDicomButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
        }

        private void MenuSelectDicom_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabControl == null) return;
            MainTabControl.SelectedIndex = 1;
            if (NavCalibrationButton != null) NavCalibrationButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            if (NavDicomButton != null) NavDicomButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
        }

        private void JawSizeMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_doseMap == null)
            {
                System.Windows.MessageBox.Show("Please generate a dose map first (Convert to Dose).", "No Dose Map");
                return;
            }

            try
            {
                var dlg = new JawSizeWindow(_doseMap, _tiffDpi, _settings)
                {
                    Owner = this
                };
                dlg.ShowDialog();
                SaveSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unable to open Jaw Size dialog: {ex.Message}");
            }
        }

        private void RefreshConfigButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshConfigs();
        }

        private void RefreshConfigs()
        {
            if (ConfigComboBox == null) return;
            ConfigComboBox.Items.Clear();
            ConfigComboBox.Items.Add("--- Select Calibration ---");

            if (System.IO.Directory.Exists(_calibrationsFolder))
            {
                var files = System.IO.Directory.GetFiles(_calibrationsFolder, "*.cal");
                foreach (var file in files)
                {
                    ConfigComboBox.Items.Add(System.IO.Path.GetFileName(file));
                }
            }
            ConfigComboBox.SelectedIndex = 0;
        }

        private void ConfigComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConfigComboBox.SelectedIndex <= 0) return;
            string fileName = ConfigComboBox.SelectedItem.ToString();
            string fullPath = System.IO.Path.Combine(_calibrationsFolder, fileName);
            LoadCalibrationConfig(fullPath);
        }

        private void LoadCalibrationConfig(string filePath)
        {
            try
            {
                var newConfig = new CalibrationConfig();
                var rawDataPoints = new List<CalibrationPoint>();
                string[] lines = System.IO.File.ReadAllLines(filePath);
                string currentSection = "";

                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        continue;
                    }

                    if (string.IsNullOrEmpty(currentSection))
                    {
                        if (trimmed.StartsWith("Channel:")) newConfig.Channel = trimmed.Substring("Channel:".Length).Trim();
                        else if (trimmed.StartsWith("Degree:")) newConfig.Degree = int.Parse(trimmed.Substring("Degree:".Length).Trim());
                    }
                    else
                    {
                        switch (currentSection)
                        {
                            case "RawData":
                                var parts = trimmed.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 && double.TryParse(parts[0], out double dose))
                                {
                                    rawDataPoints.Add(new CalibrationPoint
                                    {
                                        Dose = dose,
                                        Red = double.Parse(parts[1]),
                                        Green = double.Parse(parts[2]),
                                        Blue = double.Parse(parts[3])
                                    });
                                }
                                break;
                            case "FirstFit":
                                newConfig.FirstFit = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray();
                                break;
                            case "SecondFit":
                                newConfig.SecondFit = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray();
                                break;
                            case "ThirdFit":
                                newConfig.ThirdFit = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray();
                                break;
                            case "DeltaOpt":
                                newConfig.DeltaOpt = double.Parse(trimmed);
                                break;
                            case "RSquared":
                                newConfig.RSquared = double.Parse(trimmed);
                                break;
                        }
                    }
                }

                if (newConfig.IsValid)
                {
                    CurrentConfig = newConfig;
                    CalibrationInfoText.Text = $"Loaded: {System.IO.Path.GetFileName(filePath)}\n" +
                                             $"Channel: {newConfig.Channel}\n" +
                                             $"Degree: {newConfig.Degree}\n" +
                                             $"R²: {newConfig.RSquared:F5}";

                    // Populate calibration table from raw data
                    CalibrationPoints.Clear();
                    foreach (var pt in rawDataPoints)
                        CalibrationPoints.Add(pt);

                    // Sync channel dropdown
                    for (int i = 0; i < ChannelFitDropDown.Items.Count; i++)
                    {
                        if ((ChannelFitDropDown.Items[i] as ComboBoxItem)?.Content.ToString() == newConfig.Channel)
                        { ChannelFitDropDown.SelectedIndex = i; break; }
                    }

                    // Sync degree dropdown (degree 1 = index 0, etc.)
                    if (newConfig.Degree >= 1 && newConfig.Degree <= 5)
                        DegreeFitDropDown.SelectedIndex = newConfig.Degree - 1;

                    // Update R² display
                    RSquaredText.Text = newConfig.RSquared.ToString("F4");

                    // Rebuild plot from loaded data if we have points
                    if (CalibrationPoints.Count >= 2)
                    {
                        double[] doses = CalibrationPoints.Select(p => p.Dose).ToArray();
                        double[] doseFit = new double[doses.Length];
                        double[] rNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Red, 1) / 65535.0)).ToArray();
                        double[] gNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Green, 1) / 65535.0)).ToArray();
                        double[] bNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Blue, 1) / 65535.0)).ToArray();

                        if (newConfig.Channel.Contains("Single"))
                        {
                            double[] xData = newConfig.Channel.Contains("Red") ? rNorm : (newConfig.Channel.Contains("Green") ? gNorm : bNorm);
                            if (newConfig.FirstFit != null)
                            {
                                for (int i = 0; i < doses.Length; i++)
                                    doseFit[i] = FittingMath.PolyVal(newConfig.FirstFit, xData[i]);
                            }
                        }
                        else if (newConfig.Channel.Contains("Dual"))
                        {
                            double[] ratio = newConfig.Channel.Contains("Red")
                                ? rNorm.Zip(bNorm, (r, b) => r / (b + 1e-9)).ToArray()
                                : gNorm.Zip(bNorm, (g, b) => g / (b + 1e-9)).ToArray();
                            if (newConfig.FirstFit != null && newConfig.SecondFit != null)
                            {
                                double[] val1 = ratio.Select(r => FittingMath.PolyVal(newConfig.FirstFit!, r)).ToArray();
                                for (int i = 0; i < doses.Length; i++)
                                    doseFit[i] = FittingMath.PolyVal(newConfig.SecondFit!, val1[i]);
                            }
                        }
                        else if (newConfig.Channel.Contains("Triple"))
                        {
                            double delta = newConfig.DeltaOpt;
                            if (newConfig.FirstFit != null && newConfig.SecondFit != null && newConfig.ThirdFit != null)
                            {
                                for (int i = 0; i < doses.Length; i++)
                                {
                                    double rD = FittingMath.PolyVal(newConfig.FirstFit!, rNorm[i] * delta);
                                    double gD = FittingMath.PolyVal(newConfig.SecondFit!, gNorm[i] * delta);
                                    double bD = FittingMath.PolyVal(newConfig.ThirdFit!, bNorm[i] * delta);
                                    doseFit[i] = (rD + gD + bD) / 3.0;
                                }
                            }
                        }
                        UpdatePlot(doses, doseFit, newConfig.Channel);
                    }

                    StatusText.Text = "Config Loaded";
                    StatusIndicator.Background = new SolidColorBrush(Colors.Green);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load calibration: {ex.Message}");
            }
        }

        private async void FitButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalibrationPoints == null || CalibrationPoints.Count < 2)
            {
                var dialog = new Wpf.Ui.Controls.ContentDialog
                {
                    Title = "Insufficient Data",
                    Content = "Please extract at least 2 calibration points before calculating a fit.",
                    CloseButtonText = "Ok"
                };
                await _dialogService.ShowAsync(dialog, System.Threading.CancellationToken.None);
                return;
            }

            try
            {
                double[] doses = CalibrationPoints.Select(p => p.Dose).ToArray();
                double[] rNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Red, 1) / 65535.0)).ToArray();
                double[] gNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Green, 1) / 65535.0)).ToArray();
                double[] bNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Blue, 1) / 65535.0)).ToArray();

                string channelMode = (ChannelFitDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Single: Red";
                int degree = DegreeFitDropDown.SelectedIndex + 1;

                CurrentConfig = new CalibrationConfig { Channel = channelMode, Degree = degree, CreatedAt = DateTime.Now };
                double[]? doseFit = null;

                if (channelMode.Contains("Single")) {
                    double[] xData = channelMode.Contains("Red") ? rNorm : (channelMode.Contains("Green") ? gNorm : bNorm);
                    CurrentConfig.FirstFit = FittingMath.PolyFit(xData, doses, degree);
                    CurrentConfig.RSquared = FittingMath.CalculateRSquared(xData, doses, CurrentConfig.FirstFit);
                    if (CurrentConfig.FirstFit != null)
                    {
                        double[] coeffs = CurrentConfig.FirstFit;
                        doseFit = xData.Select(x => FittingMath.PolyVal(coeffs, x)).ToArray();
                    }
                } else if (channelMode.Contains("Dual")) {
                    double[] ratio = channelMode.Contains("Red") ? rNorm.Zip(bNorm, (r, b) => r / (b + 1e-9)).ToArray() : gNorm.Zip(bNorm, (g, b) => g / (b + 1e-9)).ToArray();
                    double[] primary = channelMode.Contains("Red") ? rNorm : gNorm;
                    CurrentConfig.FirstFit = FittingMath.PolyFit(ratio, primary, degree);
                    double[] val1 = ratio.Select(r => FittingMath.PolyVal(CurrentConfig.FirstFit!, r)).ToArray();
                    CurrentConfig.SecondFit = FittingMath.PolyFit(val1, doses, degree);
                    CurrentConfig.RSquared = FittingMath.CalculateRSquared(val1, doses, CurrentConfig.SecondFit);
                    doseFit = val1.Select(v => FittingMath.PolyVal(CurrentConfig.SecondFit!, v)).ToArray();
                } else if (channelMode.Contains("Triple")) {
                    CurrentConfig.FirstFit = FittingMath.PolyFit(rNorm, doses, degree);
                    CurrentConfig.SecondFit = FittingMath.PolyFit(gNorm, doses, degree);
                    CurrentConfig.ThirdFit = FittingMath.PolyFit(bNorm, doses, degree);
                    CurrentConfig.DeltaOpt = FittingMath.OptimizeTripleChannelDelta(rNorm, gNorm, bNorm, CurrentConfig.FirstFit, CurrentConfig.SecondFit, CurrentConfig.ThirdFit);
                    doseFit = new double[doses.Length];
                    for (int i = 0; i < doses.Length; i++) {
                        double rD = FittingMath.PolyVal(CurrentConfig.FirstFit!, rNorm[i] * CurrentConfig.DeltaOpt);
                        double gD = FittingMath.PolyVal(CurrentConfig.SecondFit!, gNorm[i] * CurrentConfig.DeltaOpt);
                        double bD = FittingMath.PolyVal(CurrentConfig.ThirdFit!, bNorm[i] * CurrentConfig.DeltaOpt);
                        doseFit[i] = (rD + gD + bD) / 3.0;
                    }
                    double ssTot = doses.Sum(d => Math.Pow(d - doses.Average(), 2));
                    double ssRes = doses.Zip(doseFit, (actual, fit) => Math.Pow(actual - fit, 2)).Sum();
                    CurrentConfig.RSquared = ssTot > 0 ? 1 - (ssRes / ssTot) : 0;
                }

                if (CurrentConfig != null) {
                    RSquaredText.Text = CurrentConfig.RSquared.ToString("F4");
                    UpdatePlot(doses, doseFit, channelMode);
                    StatusText.Text = "Calibration Successful";
                    StatusIndicator.Background = new SolidColorBrush(System.Windows.Media.Colors.Green);
                }
            } catch (Exception ex) { System.Windows.MessageBox.Show("Fitting failed: " + ex.Message); }
        }

        private void UpdatePlot(double[] actualDose, double[]? fittedDose, string label)
        {
            if (MainPlot == null) return;
            MainPlot.Plot.Clear();
            var scatter = MainPlot.Plot.Add.Scatter(actualDose, fittedDose ?? actualDose);
            scatter.LegendText = label;
            scatter.Color = ScottPlot.Color.FromHex("#0078D4");
            MainPlot.Plot.Title("Calibration Linearity (Actual vs. Fitted)");
            MainPlot.Plot.XLabel("Actual Dose (Gy)");
            MainPlot.Plot.YLabel("Calculated Dose (Gy)");
            double maxDose = (actualDose != null && actualDose.Length > 0) ? actualDose.Max() * 1.1 : 1.0;
            var line = MainPlot.Plot.Add.Line(0, 0, maxDose, maxDose);
            line.Color = ScottPlot.Color.FromHex("#44000000");
            line.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            MainPlot.Plot.Axes.AutoScale();
            MainPlot.Refresh();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentConfig == null || !CurrentConfig.IsValid) { System.Windows.MessageBox.Show("Please calculate a valid fit first."); return; }
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog 
            { 
                InitialDirectory = _calibrationsFolder,
                Filter = "Calibration Files (*.cal)|*.cal", 
                FileName = "Calibration_Active.cal" 
            };

            if (saveFileDialog.ShowDialog() == true) {
                try {
                    using (var writer = new System.IO.StreamWriter(saveFileDialog.FileName)) {
                        writer.WriteLine("# FilmDosimetry Calibration Configuration");
                        writer.WriteLine($"# Saved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        writer.WriteLine($"Channel:  {CurrentConfig.Channel}");
                        writer.WriteLine($"Degree:   {CurrentConfig.Degree}");
                        writer.WriteLine("");
                        writer.WriteLine("[RawData]");
                        foreach (var p in CalibrationPoints) writer.WriteLine($"{p.Dose}\t{p.Red}\t{p.Green}\t{p.Blue}");
                        writer.WriteLine("");
                        writer.WriteLine("[FirstFit]");
                        writer.WriteLine(string.Join(" ", CurrentConfig.FirstFit!));
                        if (CurrentConfig.SecondFit != null) { writer.WriteLine("[SecondFit]"); writer.WriteLine(string.Join(" ", CurrentConfig.SecondFit)); }
                        if (CurrentConfig.ThirdFit != null) { writer.WriteLine("[ThirdFit]"); writer.WriteLine(string.Join(" ", CurrentConfig.ThirdFit)); }
                        if (CurrentConfig.DeltaOpt != 1.0) { writer.WriteLine("[DeltaOpt]"); writer.WriteLine(CurrentConfig.DeltaOpt); }
                        writer.WriteLine("[RSquared]");
                        writer.WriteLine(CurrentConfig.RSquared);
                    }
                    StatusText.Text = "Saved Config";
                    RefreshConfigs(); // Auto-refresh dropdown
                } catch (Exception ex) { System.Windows.MessageBox.Show("Save failed: " + ex.Message); }
            }
        }

        private void AutoCenter_Click(object sender, RoutedEventArgs e) { }
        private void ManuallyAlign_Click(object sender, RoutedEventArgs e)
        {
            if (MainDisplayImage.Source == null || (_redChannel == null && _doseMap == null))
            {
                System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data");
                return;
            }
            if (_isSelectingROI) return;

            _isAligning = true;
            _alignStep = 1;
            AlignStepText.Text = "Click LEFT marker (Red)";
            AlignModeOverlay.Visibility = Visibility.Visible;
            StatusText.Text = "Alignment: Pick Left Marker";
            StatusIndicator.Background = new SolidColorBrush(Colors.DodgerBlue);
            SelectionRect.Visibility = Visibility.Collapsed;
        }

        private void ExitAlignMode_Click(object sender, RoutedEventArgs e)
        {
            _isAligning = false;
            _alignStep = 0;
            AlignModeOverlay.Visibility = Visibility.Collapsed;
            AlignMarkerLeft.Visibility = Visibility.Collapsed;
            AlignMarkerRight.Visibility = Visibility.Collapsed;
            AlignMarkerTop.Visibility = Visibility.Collapsed;
            AlignMarkerIso.Visibility = Visibility.Collapsed;
            if (StatusText.Text.StartsWith("Alignment:"))
            {
                StatusText.Text = "Ready";
                StatusIndicator.Background = new SolidColorBrush(Colors.Gray);
            }
        }

        private void HandleAlignmentClick(Point canvasPoint)
        {
            switch (_alignStep)
            {
                case 1:
                    _alignLeft = canvasPoint;
                    Canvas.SetLeft(AlignMarkerLeft, canvasPoint.X - 5);
                    Canvas.SetTop(AlignMarkerLeft, canvasPoint.Y - 5);
                    AlignMarkerLeft.Visibility = Visibility.Visible;
                    _alignStep = 2;
                    AlignStepText.Text = "Click RIGHT marker (Blue)";
                    StatusText.Text = "Alignment: Pick Right Marker";
                    break;
                case 2:
                    _alignRight = canvasPoint;
                    Canvas.SetLeft(AlignMarkerRight, canvasPoint.X - 5);
                    Canvas.SetTop(AlignMarkerRight, canvasPoint.Y - 5);
                    AlignMarkerRight.Visibility = Visibility.Visible;
                    _alignStep = 3;
                    AlignStepText.Text = "Click TOP marker (Green)";
                    StatusText.Text = "Alignment: Pick Top Marker";
                    break;
                case 3:
                    _alignTop = canvasPoint;
                    Canvas.SetLeft(AlignMarkerTop, canvasPoint.X - 5);
                    Canvas.SetTop(AlignMarkerTop, canvasPoint.Y - 5);
                    AlignMarkerTop.Visibility = Visibility.Visible;

                    System.Windows.Rect renderedRect = GetRenderedImageBounds(MainDisplayImage);
                    Point L = CanvasToPixel(_alignLeft, renderedRect);
                    Point R = CanvasToPixel(_alignRight, renderedRect);
                    Point T = CanvasToPixel(_alignTop, renderedRect);

                    PushUndo("Manual Align");
                    _ = PerformManualAlignment(L, R, T, renderedRect);
                    break;
            }
        }

        private Point CanvasToPixel(Point canvasPt, System.Windows.Rect renderedRect)
        {
            double xRatio = _imgWidth / renderedRect.Width;
            double yRatio = _imgHeight / renderedRect.Height;
            return new Point(
                (canvasPt.X - renderedRect.X) * xRatio,
                (canvasPt.Y - renderedRect.Y) * yRatio
            );
        }

        private Point PixelToCanvas(Point pixelPt, System.Windows.Rect renderedRect)
        {
            double xRatio = renderedRect.Width / _imgWidth;
            double yRatio = renderedRect.Height / _imgHeight;
            return new Point(
                pixelPt.X * xRatio + renderedRect.X,
                pixelPt.Y * yRatio + renderedRect.Y
            );
        }

        private async Task PerformManualAlignment(Point L, Point R, Point T, System.Windows.Rect renderedRect)
        {
            // Roll angle from Left-Right markers (MATLAB: atan2(R(2)-L(2), R(1)-L(1)))
            double angle = Math.Atan2(R.Y - L.Y, R.X - L.X) * (180.0 / Math.PI);

            // Isocenter: X from Top marker, Y from L-R line at that X
            double isoX = T.X;
            double isoY;
            if (Math.Abs(R.X - L.X) > 1e-9)
            {
                double slope = (R.Y - L.Y) / (R.X - L.X);
                isoY = L.Y + slope * (T.X - L.X);
            }
            else
            {
                isoY = (L.Y + R.Y) / 2.0;
            }

            // Show isocenter marker briefly
            Point isoPanelPt = PixelToCanvas(new Point(isoX, isoY), renderedRect);
            Canvas.SetLeft(AlignMarkerIso, isoPanelPt.X - 7);
            Canvas.SetTop(AlignMarkerIso, isoPanelPt.Y - 7);
            AlignMarkerIso.Visibility = Visibility.Visible;
            AlignStepText.Text = $"Isocenter found. Transforming...";
            StatusText.Text = $"Iso: ({isoX:F1}, {isoY:F1}) px, Roll: {angle:F2}°";
            await Task.Delay(600);

            int rows = _imgHeight;
            int cols = _imgWidth;
            double theta = -angle * Math.PI / 180.0;
            double cosT = Math.Cos(theta), sinT = Math.Sin(theta);

            // Transform corners to find new canvas bounds (MATLAB 1-based coordinates)
            double[,] corners = { { 1, 1 }, { cols, 1 }, { 1, rows }, { cols, rows } };
            double maxH = 0, maxV = 0;
            for (int i = 0; i < 4; i++)
            {
                double cx = corners[i, 0] - isoX;
                double cy = corners[i, 1] - isoY;
                double tx = cx * cosT + cy * sinT;
                double ty = -cx * sinT + cy * cosT;
                maxH = Math.Max(maxH, Math.Abs(tx));
                maxV = Math.Max(maxV, Math.Abs(ty));
            }

            int newW = (int)Math.Ceiling(2 * maxH * 1.02);
            int newH = (int)Math.Ceiling(2 * maxV * 1.02);
            double newCenterX = (newW + 1) / 2.0;
            double newCenterY = (newH + 1) / 2.0;

            var newRed = new double[newH, newW];
            var newGreen = new double[newH, newW];
            var newBlue = new double[newH, newW];

            // Inverse transform: for each output pixel, find source pixel
            double cosNeg = Math.Cos(-theta), sinNeg = Math.Sin(-theta);

            await Task.Run(() =>
            {
                Parallel.For(0, newH, outRow =>
                {
                    for (int outCol = 0; outCol < newW; outCol++)
                    {
                        // Undo T2 (1-based output coords)
                        double dx = (outCol + 1) - newCenterX;
                        double dy = (outRow + 1) - newCenterY;
                        // Undo rotation
                        double ux = dx * cosNeg + dy * sinNeg;
                        double uy = -dx * sinNeg + dy * cosNeg;
                        // Undo T1 (back to 1-based source coords)
                        double srcX = ux + isoX;
                        double srcY = uy + isoY;

                        // Convert to 0-based array indices
                        double srcCol = srcX - 1.0;
                        double srcRow = srcY - 1.0;

                        // Bilinear interpolation
                        if (srcCol >= 0 && srcCol < cols - 1 && srcRow >= 0 && srcRow < rows - 1)
                        {
                            int c0 = (int)srcCol, r0 = (int)srcRow;
                            int c1 = c0 + 1, r1 = r0 + 1;
                            double fc = srcCol - c0, fr = srcRow - r0;
                            double w00 = (1 - fc) * (1 - fr);
                            double w10 = fc * (1 - fr);
                            double w01 = (1 - fc) * fr;
                            double w11 = fc * fr;

                            newRed[outRow, outCol] = w00 * _redChannel[r0, c0] + w10 * _redChannel[r0, c1] + w01 * _redChannel[r1, c0] + w11 * _redChannel[r1, c1];
                            newGreen[outRow, outCol] = w00 * _greenChannel[r0, c0] + w10 * _greenChannel[r0, c1] + w01 * _greenChannel[r1, c0] + w11 * _greenChannel[r1, c1];
                            newBlue[outRow, outCol] = w00 * _blueChannel[r0, c0] + w10 * _blueChannel[r0, c1] + w01 * _blueChannel[r1, c0] + w11 * _blueChannel[r1, c1];
                        }
                    }
                });
            });

            // Replace channels with transformed data
            _redChannel = newRed;
            _greenChannel = newGreen;
            _blueChannel = newBlue;
            _imgWidth = newW;
            _imgHeight = newH;
            UpdateCropUI();
            
            // Clean up alignment UI
            ExitAlignMode_Click(null, null);

            // Refresh display
            UpdateDisplayFromRaw();
            StatusText.Text = $"Aligned: {angle:F2}° rotation, center at ({isoX:F0}, {isoY:F0})";
            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
        }
        private void AutoAlign_Click(object sender, RoutedEventArgs e) { }

        #region Orientation (Rotation & Flip)

        /// <summary>Refreshes the display based on current mode (dose map or raw image).</summary>
        private void RefreshDisplay()
        {
            if (_isShowingDoseMap && _doseMap != null)
            {
                MainDisplayImage.Source = GenerateDoseHeatmap();
            }
            else if (_redChannel != null)
            {
                UpdateDisplayFromRaw();
            }
            else
            {
                MainDisplayImage.Source = null;
            }
        }

        private static double[,] Rotate2D(double[,] src, int oldH, int oldW, bool isCW)
        {
            var dst = new double[oldW, oldH];
            for (int row = 0; row < oldH; row++)
                for (int col = 0; col < oldW; col++)
                {
                    int nr, nc;
                    if (isCW) { nr = col; nc = oldH - 1 - row; }
                    else { nr = oldW - 1 - col; nc = row; }
                    dst[nr, nc] = src[row, col];
                }
            return dst;
        }

        private void Rotation_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }

            bool isCW = (sender as FrameworkElement)?.Name == "CWButton";
            PushUndo(isCW ? "Rotate CW" : "Rotate CCW");

            int oldH = _imgHeight, oldW = _imgWidth;
            if (_redChannel != null)
            {
                _redChannel = Rotate2D(_redChannel, oldH, oldW, isCW);
                _greenChannel = Rotate2D(_greenChannel, oldH, oldW, isCW);
                _blueChannel = Rotate2D(_blueChannel, oldH, oldW, isCW);
            }
            if (_doseMap != null) _doseMap = Rotate2D(_doseMap, oldH, oldW, isCW);

            _imgWidth = oldH; _imgHeight = oldW;
            UpdateCropUI();
            RefreshDisplay();
            StatusText.Text = isCW ? "Rotated CW 90°" : "Rotated CCW 90°";
        }

        private static void FlipH(double[,] data, int h, int w)
        {
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w / 2; col++)
                {
                    int m = w - 1 - col;
                    (data[row, col], data[row, m]) = (data[row, m], data[row, col]);
                }
        }

        private static void FlipV(double[,] data, int h, int w)
        {
            for (int row = 0; row < h / 2; row++)
            {
                int m = h - 1 - row;
                for (int col = 0; col < w; col++)
                    (data[row, col], data[m, col]) = (data[m, col], data[row, col]);
            }
        }

        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }

            bool isHorizontal = (sender as FrameworkElement)?.Name == "FlipHButton";
            PushUndo(isHorizontal ? "Flip H" : "Flip V");

            int h = _imgHeight, w = _imgWidth;
            Action<double[,], int, int> flipFn = isHorizontal ? FlipH : FlipV;
            if (_redChannel != null)
            {
                flipFn(_redChannel, h, w);
                flipFn(_greenChannel, h, w);
                flipFn(_blueChannel, h, w);
            }
            if (_doseMap != null) flipFn(_doseMap, h, w);

            RefreshDisplay();
            StatusText.Text = isHorizontal ? "Flipped Horizontal" : "Flipped Vertical";
        }

        #endregion

        #region Cropping

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }

            string name = (sender as FrameworkElement)?.Name ?? "";

            if (name == "ManualCropButton")
            {
                if (_isSelectingROI || _isAligning) return;
                _isCropping = true;
                _isSelectingROI = true;
                _isFixedMode = false;
                ROIModeOverlay.Visibility = Visibility.Collapsed;
                AlignModeOverlay.Visibility = Visibility.Collapsed;
                StatusText.Text = "Draw crop region...";
                StatusIndicator.Background = new SolidColorBrush(Colors.DodgerBlue);
            }
            else if (name == "CenterCropButton")
            {
                if (!int.TryParse(CenterCropWidth.Text, out int cropW) || !int.TryParse(CenterCropHeight.Text, out int cropH))
                {
                    System.Windows.MessageBox.Show("Please enter valid width and height values.", "Invalid Input");
                    return;
                }

                cropW = Math.Min(cropW, _imgWidth);
                cropH = Math.Min(cropH, _imgHeight);

                int startX = (_imgWidth - cropW) / 2;
                int startY = (_imgHeight - cropH) / 2;

                PushUndo("Center Crop");
                ApplyCrop(startX, startY, cropW, cropH);
                StatusText.Text = $"Center Cropped to {cropW} x {cropH}";
            }
        }

        private static double[,] CropArray(double[,] src, int x, int y, int w, int h)
        {
            var dst = new double[h, w];
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w; col++)
                    dst[row, col] = src[y + row, x + col];
            return dst;
        }

        private void ApplyCrop(int x, int y, int w, int h)
        {
            x = Math.Max(0, x); y = Math.Max(0, y);
            w = Math.Min(w, _imgWidth - x); h = Math.Min(h, _imgHeight - y);
            if (w <= 0 || h <= 0) return;

            if (_redChannel != null) _redChannel = CropArray(_redChannel, x, y, w, h);
            if (_greenChannel != null) _greenChannel = CropArray(_greenChannel, x, y, w, h);
            if (_blueChannel != null) _blueChannel = CropArray(_blueChannel, x, y, w, h);
            if (_doseMap != null) _doseMap = CropArray(_doseMap, x, y, w, h);

            _imgWidth = w; _imgHeight = h;
            UpdateCropUI();
            RefreshDisplay();
            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
        }

        #endregion

        #region Filters, Smoothing & Interpolation

        private async void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }

            string name = (sender as FrameworkElement)?.Name ?? "";
            bool doseMode = _isShowingDoseMap && _doseMap != null;

            if (name == "MedianFilterButton")
            {
                if (!int.TryParse(MedianKernelSize.Text, out int kernelSize) || kernelSize < 1) kernelSize = 3;
                if (kernelSize % 2 == 0) kernelSize++;
                PushUndo($"Median {kernelSize}x{kernelSize}");
                StatusText.Text = $"Applying Median Filter ({kernelSize}x{kernelSize})...";
                StatusIndicator.Background = new SolidColorBrush(Colors.Orange);

                await Task.Run(() =>
                {
                    if (doseMode)
                    {
                        _doseMap = MedianFilter2D(_doseMap, kernelSize);
                    }
                    else
                    {
                        _redChannel = MedianFilter2D(_redChannel, kernelSize);
                        _greenChannel = MedianFilter2D(_greenChannel, kernelSize);
                        _blueChannel = MedianFilter2D(_blueChannel, kernelSize);
                    }
                });

                RefreshDisplay();
                StatusText.Text = $"Median Filter Applied ({kernelSize}x{kernelSize})";
                StatusIndicator.Background = new SolidColorBrush(Colors.Green);
            }
            else if (name == "ROIFilterButton")
            {
                if (_isSelectingROI || _isAligning) return;
                _isROIFiltering = true;
                _isSelectingROI = true;
                _isFixedMode = false;
                StatusText.Text = "Draw ROI region (outside will be zeroed)...";
                StatusIndicator.Background = new SolidColorBrush(Colors.DodgerBlue);
            }
            else if (name == "NoiseFilterButton")
            {
                if (!double.TryParse(NoiseThreshold.Text, out double threshold)) threshold = 65535;
                PushUndo("Noise Filter");
                StatusText.Text = "Applying Noise Filter...";

                await Task.Run(() =>
                {
                    if (doseMode)
                    {
                        ApplyNoiseFilter(_doseMap, threshold);
                    }
                    else
                    {
                        ApplyNoiseFilter(_redChannel, threshold);
                        ApplyNoiseFilter(_greenChannel, threshold);
                        ApplyNoiseFilter(_blueChannel, threshold);
                    }
                });

                RefreshDisplay();
                StatusText.Text = $"Noise Filter Applied (threshold: {threshold})";
                StatusIndicator.Background = new SolidColorBrush(Colors.Green);
            }
            else if (name == "SmoothButton")
            {
                string method = (SmoothDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Average";
                if (!double.TryParse(SmoothStrength.Text, out double strength) || strength < 1) strength = 3;
                int window = (int)Math.Round(strength);
                if (window % 2 == 0) window++;

                PushUndo($"{method} Smooth");
                StatusText.Text = $"Applying {method} Smooth...";
                StatusIndicator.Background = new SolidColorBrush(Colors.Orange);

                await Task.Run(() =>
                {
                    if (doseMode)
                    {
                        if (method == "Average") _doseMap = BoxFilter2D(_doseMap, window);
                        else if (method == "Median") _doseMap = MedianFilter2D(_doseMap, window);
                        else if (method == "Gaussian") _doseMap = GaussianFilter2D(_doseMap, strength);
                    }
                    else
                    {
                        if (method == "Average")
                        {
                            _redChannel = BoxFilter2D(_redChannel, window);
                            _greenChannel = BoxFilter2D(_greenChannel, window);
                            _blueChannel = BoxFilter2D(_blueChannel, window);
                        }
                        else if (method == "Median")
                        {
                            _redChannel = MedianFilter2D(_redChannel, window);
                            _greenChannel = MedianFilter2D(_greenChannel, window);
                            _blueChannel = MedianFilter2D(_blueChannel, window);
                        }
                        else if (method == "Gaussian")
                        {
                            _redChannel = GaussianFilter2D(_redChannel, strength);
                            _greenChannel = GaussianFilter2D(_greenChannel, strength);
                            _blueChannel = GaussianFilter2D(_blueChannel, strength);
                        }
                    }
                });

                RefreshDisplay();
                StatusText.Text = $"{method} Smooth Applied";
                StatusIndicator.Background = new SolidColorBrush(Colors.Green);
            }
            else if (name == "InterpButton")
            {
                string method = (InterpolationDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Linear";
                if (!double.TryParse(InterpScale.Text, out double scale) || scale < 1) scale = 2;
                scale = Math.Min(scale, 10);

                if (Math.Abs(scale - 1.0) < 0.01) return;

                PushUndo($"Interpolation {scale:F1}x");
                StatusText.Text = $"Interpolating ({method}, {scale:F1}x)...";
                StatusIndicator.Background = new SolidColorBrush(Colors.Orange);

                int newW = (int)Math.Round(_imgWidth * scale);
                int newH = (int)Math.Round(_imgHeight * scale);

                await Task.Run(() =>
                {
                    if (doseMode)
                    {
                        _doseMap = Interpolate2D(_doseMap, newW, newH, method);
                    }
                    else
                    {
                        _redChannel = Interpolate2D(_redChannel, newW, newH, method);
                        _greenChannel = Interpolate2D(_greenChannel, newW, newH, method);
                        _blueChannel = Interpolate2D(_blueChannel, newW, newH, method);
                    }
                });

                _imgWidth = newW; _imgHeight = newH;
                _tiffDpi *= scale; // Maintain physical dimensions by scaling DPI

                UpdateCropUI();
                MetaDPI.Text = _tiffDpi.ToString("F1"); // Update UI Metadata

                RefreshDisplay();
                StatusText.Text = $"Interpolated to {newW}x{newH} ({method})";
                StatusIndicator.Background = new SolidColorBrush(Colors.Green);
            }
        }

        // --- Median Filter (medfilt2 equivalent) ---
        private static double[,] MedianFilter2D(double[,] input, int kernelSize)
        {
            int h = input.GetLength(0), w = input.GetLength(1);
            var output = new double[h, w];
            int half = kernelSize / 2;
            var buffer = new double[kernelSize * kernelSize];

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    int count = 0;
                    for (int ky = -half; ky <= half; ky++)
                    {
                        int ry = Math.Clamp(row + ky, 0, h - 1);
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int cx = Math.Clamp(col + kx, 0, w - 1);
                            buffer[count++] = input[ry, cx];
                        }
                    }
                    Array.Sort(buffer, 0, count);
                    output[row, col] = buffer[count / 2];
                }
            }
            return output;
        }

        // --- Box/Average Filter (smoothdata2 movmean equivalent) ---
        private static double[,] BoxFilter2D(double[,] input, int kernelSize)
        {
            int h = input.GetLength(0), w = input.GetLength(1);
            var output = new double[h, w];
            int half = kernelSize / 2;

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    double sum = 0; int count = 0;
                    for (int ky = -half; ky <= half; ky++)
                    {
                        int ry = Math.Clamp(row + ky, 0, h - 1);
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int cx = Math.Clamp(col + kx, 0, w - 1);
                            sum += input[ry, cx];
                            count++;
                        }
                    }
                    output[row, col] = sum / count;
                }
            }
            return output;
        }

        // --- Gaussian Filter (imgaussfilt equivalent) ---
        private static double[,] GaussianFilter2D(double[,] input, double sigma)
        {
            int kernelRadius = (int)Math.Ceiling(sigma * 3);
            int kernelSize = kernelRadius * 2 + 1;

            // Build Gaussian kernel
            double[] kernel1D = new double[kernelSize];
            double kernelSum = 0;
            for (int i = 0; i < kernelSize; i++)
            {
                double x = i - kernelRadius;
                kernel1D[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
                kernelSum += kernel1D[i];
            }
            for (int i = 0; i < kernelSize; i++) kernel1D[i] /= kernelSum;

            int h = input.GetLength(0), w = input.GetLength(1);

            // Separable: horizontal pass
            var temp = new double[h, w];
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    double sum = 0;
                    for (int k = -kernelRadius; k <= kernelRadius; k++)
                    {
                        int cx = Math.Clamp(col + k, 0, w - 1);
                        sum += input[row, cx] * kernel1D[k + kernelRadius];
                    }
                    temp[row, col] = sum;
                }
            }

            // Vertical pass
            var output = new double[h, w];
            for (int col = 0; col < w; col++)
            {
                for (int row = 0; row < h; row++)
                {
                    double sum = 0;
                    for (int k = -kernelRadius; k <= kernelRadius; k++)
                    {
                        int ry = Math.Clamp(row + k, 0, h - 1);
                        sum += temp[ry, col] * kernel1D[k + kernelRadius];
                    }
                    output[row, col] = sum;
                }
            }
            return output;
        }

        // --- Noise Filter (NaN/Inf/threshold removal) ---
        private static void ApplyNoiseFilter(double[,] data, double threshold)
        {
            int h = data.GetLength(0), w = data.GetLength(1);
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    double v = data[row, col];
                    if (double.IsNaN(v) || double.IsInfinity(v) || Math.Abs(v) > threshold)
                        data[row, col] = 1;
                }
            }
        }

        // --- 2D Interpolation (interp2 equivalent) ---
        private static double[,] Interpolate2D(double[,] input, int newW, int newH, string method)
        {
            int oldH = input.GetLength(0), oldW = input.GetLength(1);
            var output = new double[newH, newW];

            Parallel.For(0, newH, newRow =>
            {
                for (int newCol = 0; newCol < newW; newCol++)
                {
                    // Map output pixel to input coordinates
                    double srcRow = (double)newRow / (newH - 1) * (oldH - 1);
                    double srcCol = (double)newCol / (newW - 1) * (oldW - 1);

                    if (method == "Nearest")
                    {
                        int r = (int)Math.Round(srcRow);
                        int c = (int)Math.Round(srcCol);
                        r = Math.Clamp(r, 0, oldH - 1);
                        c = Math.Clamp(c, 0, oldW - 1);
                        output[newRow, newCol] = input[r, c];
                    }
                    else if (method == "Linear")
                    {
                        output[newRow, newCol] = BilinearSample(input, srcRow, srcCol, oldH, oldW);
                    }
                    else // Cubic
                    {
                        output[newRow, newCol] = BicubicSample(input, srcRow, srcCol, oldH, oldW);
                    }
                }
            });

            return output;
        }

        private static double BilinearSample(double[,] data, double row, double col, int h, int w)
        {
            int r0 = Math.Clamp((int)row, 0, h - 2);
            int c0 = Math.Clamp((int)col, 0, w - 2);
            double fr = row - r0, fc = col - c0;
            return data[r0, c0] * (1 - fr) * (1 - fc) +
                   data[r0, c0 + 1] * (1 - fr) * fc +
                   data[r0 + 1, c0] * fr * (1 - fc) +
                   data[r0 + 1, c0 + 1] * fr * fc;
        }

        private static double BicubicSample(double[,] data, double row, double col, int h, int w)
        {
            int r0 = (int)Math.Floor(row);
            int c0 = (int)Math.Floor(col);
            double fr = row - r0, fc = col - c0;

            double sum = 0;
            for (int m = -1; m <= 2; m++)
            {
                double wr = CubicWeight(fr - m);
                for (int n = -1; n <= 2; n++)
                {
                    double wc = CubicWeight(fc - n);
                    int ri = Math.Clamp(r0 + m, 0, h - 1);
                    int ci = Math.Clamp(c0 + n, 0, w - 1);
                    sum += data[ri, ci] * wr * wc;
                }
            }
            return sum;
        }

        private static double CubicWeight(double x)
        {
            x = Math.Abs(x);
            if (x <= 1) return 1.5 * x * x * x - 2.5 * x * x + 1;
            if (x < 2) return -0.5 * x * x * x + 2.5 * x * x - 4 * x + 2;
            return 0;
        }

        // --- ROI Mask Filter (zero everything outside rectangle) ---
        private void ApplyROIMask(int x, int y, int w, int h)
        {
            x = Math.Max(0, x); y = Math.Max(0, y);
            w = Math.Min(w, _imgWidth - x); h = Math.Min(h, _imgHeight - y);
            bool doseMode = _isShowingDoseMap && _doseMap != null;

            for (int row = 0; row < _imgHeight; row++)
            {
                for (int col = 0; col < _imgWidth; col++)
                {
                    if (row < y || row >= y + h || col < x || col >= x + w)
                    {
                        if (doseMode)
                        {
                            _doseMap[row, col] = 0;
                        }
                        else
                        {
                            _redChannel[row, col] = 0;
                            _greenChannel[row, col] = 0;
                            _blueChannel[row, col] = 0;
                        }
                    }
                }
            }
        }

        #endregion
        private async void ExtractROI_Click(object sender, RoutedEventArgs e)
        {
            if (MainDisplayImage.Source == null)
            {
                System.Windows.MessageBox.Show("Please load an image first.", "No Image");
                return;
            }

            // 1. Create the Dialog Content
            var stackPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            
            var freeRadio = new System.Windows.Controls.RadioButton { Content = "Free ROI (Click & Drag)", IsChecked = !_settings.LastWasFixed, Margin = new Thickness(0,0,0,10) };
            var fixedRadio = new System.Windows.Controls.RadioButton { Content = "Fixed ROI (Precise Pixel Area)", IsChecked = _settings.LastWasFixed, Margin = new Thickness(0,0,0,10) };
            
            var fixedDataGrid = new System.Windows.Controls.Grid { Margin = new Thickness(20,0,0,10), IsEnabled = _settings.LastWasFixed };
            fixedDataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            fixedDataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fixedDataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            fixedDataGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var wLabel = new System.Windows.Controls.TextBlock { Text = "Width:", VerticalAlignment = VerticalAlignment.Center };
            var wInput = new System.Windows.Controls.TextBox { Text = _settings.FixedWidth.ToString(), Margin = new Thickness(0,0,0,4) };
            var hLabel = new System.Windows.Controls.TextBlock { Text = "Height:", VerticalAlignment = VerticalAlignment.Center };
            var hInput = new System.Windows.Controls.TextBox { Text = _settings.FixedHeight.ToString() };

            System.Windows.Controls.Grid.SetRow(wLabel, 0); System.Windows.Controls.Grid.SetColumn(wLabel, 0);
            System.Windows.Controls.Grid.SetRow(wInput, 0); System.Windows.Controls.Grid.SetColumn(wInput, 1);
            System.Windows.Controls.Grid.SetRow(hLabel, 1); System.Windows.Controls.Grid.SetColumn(hLabel, 0);
            System.Windows.Controls.Grid.SetRow(hInput, 1); System.Windows.Controls.Grid.SetColumn(hInput, 1);

            fixedDataGrid.Children.Add(wLabel); fixedDataGrid.Children.Add(wInput);
            fixedDataGrid.Children.Add(hLabel); fixedDataGrid.Children.Add(hInput);

            fixedRadio.Checked += (s, ev) => fixedDataGrid.IsEnabled = true;
            fixedRadio.Unchecked += (s, ev) => fixedDataGrid.IsEnabled = false;

            stackPanel.Children.Add(freeRadio);
            stackPanel.Children.Add(fixedRadio);
            stackPanel.Children.Add(fixedDataGrid);

            // 2. Show the Dialog
            var dialog = new ContentDialog
            {
                Title = "ROI Extraction Mode",
                Content = stackPanel,
                PrimaryButtonText = "Select Region",
                CloseButtonText = "Cancel"
            };

            var result = await _dialogService.ShowAsync(dialog, CancellationToken.None);

            if (result == ContentDialogResult.Primary)
            {
                _isFixedMode = fixedRadio.IsChecked == true;
                _settings.LastWasFixed = _isFixedMode;
                
                if (_isFixedMode)
                {
                    if (int.TryParse(wInput.Text, out int w) && int.TryParse(hInput.Text, out int h))
                    {
                        _settings.FixedWidth = w;
                        _settings.FixedHeight = h;
                    }
                }
                SaveSettings();

                _isSelectingROI = true;
                ROIModeOverlay.Visibility = Visibility.Visible;

                if (_isFixedMode)
                {
                    // Initial calculation of ROI box size in pixels
                    RefreshFixedROISize();
                    SelectionRect.Visibility = Visibility.Visible;
                }
            }
        }

        private void RefreshFixedROISize()
        {
            if (MainDisplayImage.Source is BitmapSource bs)
            {
                SelectionRect.Width = (double)_settings.FixedWidth / bs.PixelWidth * MainDisplayImage.ActualWidth;
                SelectionRect.Height = (double)_settings.FixedHeight / bs.PixelHeight * MainDisplayImage.ActualHeight;
            }
        }

        private void ExitROIMode_Click(object sender, RoutedEventArgs e)
        {
            _isSelectingROI = false;
            _isDrawing = false;
            _activeMeasurementMode = MeasurementMode.None;
            _isAreaRectMode = false;

            SelectionRect.Visibility = Visibility.Collapsed;
            SelectionCrosshairH.Visibility = Visibility.Collapsed;
            SelectionCrosshairV.Visibility = Visibility.Collapsed;
            MeasurementLine.Visibility = Visibility.Collapsed;
            MeasurementPolyline.Visibility = Visibility.Collapsed;
            MeasurementLabel.Visibility = Visibility.Collapsed;
            ROIModeOverlay.Visibility = Visibility.Collapsed;
            
            StatusText.Text = "ROI Tool Deactivated";
            StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF4500"));
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (CalibrationGrid.SelectedItems == null || CalibrationGrid.SelectedItems.Count == 0) return;

            // Copy to list to avoid collection modification issues while iterating
            var itemsToDelete = CalibrationGrid.SelectedItems.Cast<CalibrationPoint>().ToList();
            
            foreach (var item in itemsToDelete)
            {
                CalibrationPoints.Remove(item);
            }

            StatusText.Text = $"Deleted {itemsToDelete.Count} point(s)";
            StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22"));
        }

                private async void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAligning)
            {
                HandleAlignmentClick(e.GetPosition(SelectionCanvas));
                return;
            }

            if (!_isSelectingROI) return;

            if (_isFixedMode)
            {
                // Finalize fixed ROI position exactly where the click happened
                await PerformROIExtraction();
                // Rulers can be refreshed once here
                UpdateRulers();
                return;
            }

            if (_activeMeasurementMode == MeasurementMode.Distance)
            {
                _isDrawing = true;
                _startPoint = e.GetPosition(SelectionCanvas);
                MeasurementLine.X1 = MeasurementLine.X2 = _startPoint.X;
                MeasurementLine.Y1 = MeasurementLine.Y2 = _startPoint.Y;
                MeasurementLine.Visibility = Visibility.Visible;
                MeasurementLabel.Visibility = Visibility.Visible;
                return;
            }

            if (_activeMeasurementMode == MeasurementMode.Area)
            {
                _isDrawing = true;
                _startPoint = e.GetPosition(SelectionCanvas);
                if (_isAreaRectMode)
                {
                    SelectionRect.Width = 0;
                    SelectionRect.Height = 0;
                    SelectionRect.Visibility = Visibility.Visible;
                    Canvas.SetLeft(SelectionRect, _startPoint.X);
                    Canvas.SetTop(SelectionRect, _startPoint.Y);
                }
                else
                {
                    _measurementPoints.Clear();
                    _measurementPoints.Add(_startPoint);
                    MeasurementPolyline.Points.Clear();
                    MeasurementPolyline.Points.Add(_startPoint);
                    MeasurementPolyline.Visibility = Visibility.Visible;
                }
                MeasurementLabel.Visibility = Visibility.Visible;
                return;
            }

            _isDrawing = true;
            _startPoint = e.GetPosition(SelectionCanvas);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _startPoint.X);
            Canvas.SetTop(SelectionRect, _startPoint.Y);
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateStatusCoordinates(e);

            if (!_isSelectingROI) return;

            if (_isFixedMode)
            {
                // Ensure ROI size is current (handles image resizes)
                RefreshFixedROISize();

                // Follow the cursor (Centered position)
                Point pos = e.GetPosition(SelectionCanvas);
                double left = pos.X - SelectionRect.Width / 2;
                double top = pos.Y - SelectionRect.Height / 2;
                
                Canvas.SetLeft(SelectionRect, left);
                Canvas.SetTop(SelectionRect, top);
                SelectionRect.Visibility = Visibility.Visible;

                // Position Crosshairs (Dynamic lines spanning the canvas)
                SelectionCrosshairH.X1 = 0;
                SelectionCrosshairH.X2 = SelectionCanvas.ActualWidth;
                SelectionCrosshairH.Y1 = SelectionCrosshairH.Y2 = pos.Y;
                SelectionCrosshairH.Visibility = Visibility.Visible;

                SelectionCrosshairV.X1 = SelectionCrosshairV.X2 = pos.X;
                SelectionCrosshairV.Y1 = 0;
                SelectionCrosshairV.Y2 = SelectionCanvas.ActualHeight;
                SelectionCrosshairV.Visibility = Visibility.Visible;

                return;
            }

            if (!_isDrawing) return;

            Point currentPoint = e.GetPosition(SelectionCanvas);

            if (_activeMeasurementMode == MeasurementMode.Distance)
            {
                MeasurementLine.X2 = currentPoint.X;
                MeasurementLine.Y2 = currentPoint.Y;

                double distmm = CalculateDistance(ControlToPixel(_startPoint), ControlToPixel(currentPoint));
                MeasurementLabelText.Text = $"{distmm:F2} mm";

                Canvas.SetLeft(MeasurementLabel, (MeasurementLine.X1 + MeasurementLine.X2) / 2);
                Canvas.SetTop(MeasurementLabel, (MeasurementLine.Y1 + MeasurementLine.Y2) / 2 - 20);
                return;
            }

            if (_activeMeasurementMode == MeasurementMode.Area && _isDrawing)
            {
                Point pos = e.GetPosition(SelectionCanvas);
                if (_isAreaRectMode)
                {
                    double rectX = Math.Min(pos.X, _startPoint.X);
                    double rectY = Math.Min(pos.Y, _startPoint.Y);
                    double rectW = Math.Abs(pos.X - _startPoint.X);
                    double rectH = Math.Abs(pos.Y - _startPoint.Y);

                    SelectionRect.Width = rectW;
                    SelectionRect.Height = rectH;
                    Canvas.SetLeft(SelectionRect, rectX);
                    Canvas.SetTop(SelectionRect, rectY);

                    Point startPixel = ControlToPixel(_startPoint);
                    Point endPixel = ControlToPixel(pos);
                    double wmm = Math.Abs(startPixel.X - endPixel.X) * 25.4 / _tiffDpi;
                    double hmm = Math.Abs(startPixel.Y - endPixel.Y) * 25.4 / _tiffDpi;
                    double areamm2 = wmm * hmm;

                    MeasurementLabelText.Text = $"{wmm:F1}x{hmm:F1} mm\n{areamm2:N1} mm²";
                    Canvas.SetLeft(MeasurementLabel, rectX + rectW / 2);
                    Canvas.SetTop(MeasurementLabel, rectY + rectH / 2 - 25);
                }
                else
                {
                    _measurementPoints.Add(pos);
                    MeasurementPolyline.Points.Add(pos);

                    if (_measurementPoints.Count > 3)
                    {
                        double areamm2 = CalculateArea(_measurementPoints);
                        MeasurementLabelText.Text = $"{areamm2:N1} mm²";
                        Canvas.SetLeft(MeasurementLabel, pos.X);
                        Canvas.SetTop(MeasurementLabel, pos.Y - 25);
                    }
                }
                return;
            }

            double x = Math.Min(currentPoint.X, _startPoint.X);
            double y = Math.Min(currentPoint.Y, _startPoint.Y);
            double w = Math.Abs(currentPoint.X - _startPoint.X);
            double h = Math.Abs(currentPoint.Y - _startPoint.Y);

            SelectionRect.Width = w;
            SelectionRect.Height = h;
            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
        }

        private async void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;

            if (_activeMeasurementMode == MeasurementMode.Distance || _activeMeasurementMode == MeasurementMode.Area)
            {
                // Measurements remain visible until mode is changed or new drawing starts
                return;
            }

            if (_isCropping || _isROIFiltering)
            {
                // Convert selection rectangle to pixel coordinates
                if (MainDisplayImage.Source is BitmapSource bs)
                {
                    System.Windows.Rect renderedRect = GetRenderedImageBounds(MainDisplayImage);
                    if (renderedRect.Width <= 0 || renderedRect.Height <= 0) return;

                    double xInControl = Canvas.GetLeft(SelectionRect);
                    double yInControl = Canvas.GetTop(SelectionRect);
                    double xRatio = bs.PixelWidth / renderedRect.Width;
                    double yRatio = bs.PixelHeight / renderedRect.Height;

                    int left = (int)((xInControl - renderedRect.X) * xRatio);
                    int top = (int)((yInControl - renderedRect.Y) * yRatio);
                    int width = (int)(SelectionRect.Width * xRatio);
                    int height = (int)(SelectionRect.Height * yRatio);

                    left = Math.Max(0, left); top = Math.Max(0, top);
                    width = Math.Min(width, _imgWidth - left);
                    height = Math.Min(height, _imgHeight - top);

                    if (width > 0 && height > 0)
                    {
                        if (_isCropping)
                        {
                            PushUndo("Manual Crop");
                            ApplyCrop(left, top, width, height);
                            StatusText.Text = $"Cropped to {width} x {height}";
                        }
                        else if (_isROIFiltering)
                        {
                            PushUndo("ROI Filter");
                            ApplyROIMask(left, top, width, height);
                            RefreshDisplay();
                            StatusText.Text = "ROI Filter Applied";
                            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
                        }
                    }
                }

                _isCropping = false;
                _isROIFiltering = false;
                _isSelectingROI = false;
                SelectionRect.Visibility = Visibility.Collapsed;
                return;
            }

            await PerformROIExtraction();
        }

                private bool _isMeasurementMode = false;

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_doseMap == null && _redChannel == null)
            {
                System.Windows.MessageBox.Show("Please load an image first.", "No Data");
                return;
            }

            if (MainPlot == null) return;

            int width = _imgWidth;
            int height = _imgHeight;
            int centerX = width / 2;
            int centerY = height / 2;

            double[] xProfile;
            double[] yProfile;
            string unit = "Dose (Gy)";

            if (_doseMap != null)
            {
                xProfile = new double[width];
                for (int x = 0; x < width; x++) xProfile[x] = _doseMap[centerY, x];

                yProfile = new double[height];
                for (int y = 0; y < height; y++) yProfile[y] = _doseMap[y, centerX];
            }
            else
            {
                // Fallback to active channel (using Red as default)
                xProfile = new double[width];
                for (int x = 0; x < width; x++) xProfile[x] = _redChannel[centerY, x];

                yProfile = new double[height];
                for (int y = 0; y < height; y++) yProfile[y] = _redChannel[y, centerX];
                unit = "Pixel Value";
            }

            double[] xDistances = new double[width];
            for (int x = 0; x < width; x++) xDistances[x] = (x - centerX) * 25.4 / _tiffDpi;

            double[] yDistances = new double[height];
            for (int y = 0; y < height; y++) yDistances[y] = (y - centerY) * 25.4 / _tiffDpi;

            MainPlot.Plot.Clear();
            
            var xScatter = MainPlot.Plot.Add.Scatter(xDistances, xProfile);
            xScatter.LegendText = "X Profile (H)";
            xScatter.Color = ScottPlot.Color.FromHex("#0078D4"); // Blue
            xScatter.LineWidth = 1;
            xScatter.MarkerSize = 0;

            var yScatter = MainPlot.Plot.Add.Scatter(yDistances, yProfile);
            yScatter.LegendText = "Y Profile (V)";
            yScatter.Color = ScottPlot.Color.FromHex("#E81123"); // Red
            yScatter.LineWidth = 1;
            yScatter.MarkerSize = 0;

            MainPlot.Plot.Title("OAR / Central Profiles");
            MainPlot.Plot.XLabel("Distance from Center (mm)");
            MainPlot.Plot.YLabel(unit);
            MainPlot.Plot.ShowLegend();
            MainPlot.Plot.Axes.AutoScale();
            MainPlot.Refresh();

            StatusText.Text = "Profiles Plotted";
        }

        private async void Measurement_Click(object sender, RoutedEventArgs e)
        {
            if (MainDisplayImage.Source == null) return;

            // Identify the mode
            if (sender is System.Windows.Controls.Button btn)
            {
                if (btn.Name == "DistanceButton") _activeMeasurementMode = MeasurementMode.Distance;
                else if (btn.Name == "AreaButton")
                {
                    _activeMeasurementMode = MeasurementMode.Area;
                    // Show Tool Choice
                    var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
                    var rbRect = new System.Windows.Controls.RadioButton { Content = "Simple Rectangle", IsChecked = true, Margin = new Thickness(0, 0, 0, 10) };
                    var rbFree = new System.Windows.Controls.RadioButton { Content = "Freehand Draw", Margin = new Thickness(0, 0, 0, 10) };
                    stack.Children.Add(rbRect);
                    stack.Children.Add(rbFree);

                    var diag = new ContentDialog { Title = "Choose Area Tool", Content = stack, PrimaryButtonText = "Select Tool", CloseButtonText = "Cancel" };
                    var res = await _dialogService.ShowAsync(diag, System.Threading.CancellationToken.None);
                    if (res != ContentDialogResult.Primary) { _activeMeasurementMode = MeasurementMode.None; return; }

                    _isAreaRectMode = rbRect.IsChecked == true;
                }
                else _activeMeasurementMode = MeasurementMode.ROIDose;
            }

            _isMeasurementMode = true;
            _isSelectingROI = true;
            _isCropping = false;
            _isROIFiltering = false;

            ROIModeOverlay.Visibility = Visibility.Visible;

            string modeName = _activeMeasurementMode == MeasurementMode.Distance ? "Distance/Line" :
                            _activeMeasurementMode == MeasurementMode.Area ? $"Area ({(_isAreaRectMode ? "Rectangle" : "Freehand")})" : "ROI Dose";

            StatusText.Text = $"Measurement Mode: {modeName}";
            StatusIndicator.Background = new SolidColorBrush(Colors.MediumPurple);

            // Hide other visual tools
            MeasurementLine.Visibility = Visibility.Collapsed;
            MeasurementPolyline.Visibility = Visibility.Collapsed;
            MeasurementLabel.Visibility = Visibility.Collapsed;

            if (_activeMeasurementMode == MeasurementMode.ROIDose && _settings.LastWasFixed)
            {
                RefreshFixedROISize();
                SelectionRect.Visibility = Visibility.Visible;
            }
            else
            {
                SelectionRect.Visibility = Visibility.Collapsed;
            }
        }

        private async System.Threading.Tasks.Task PerformROIExtraction()
        {
            if (MainDisplayImage.Source is not BitmapSource bitmapSource) return;

            // 1. Get the actual rendered image bounds (accounts for Uniform stretch/centering)
            System.Windows.Rect renderedRect = GetRenderedImageBounds(MainDisplayImage);
            if (renderedRect.Width <= 0 || renderedRect.Height <= 0) return;

            // 2. Identify the ROI position relative to the ACTUAL image pixels
            double xInControl = Canvas.GetLeft(SelectionRect);
            double yInControl = Canvas.GetTop(SelectionRect);

            // Correct for "letterboxing" offsets
            double xInImage = xInControl - renderedRect.X;
            double yInImage = yInControl - renderedRect.Y;

            // Accurate pixel ratios (pixels per display point)
            double xRatio = bitmapSource.PixelWidth / renderedRect.Width;
            double yRatio = bitmapSource.PixelHeight / renderedRect.Height;

            int left = (int)(xInImage * xRatio);
            int top = (int)(yInImage * yRatio);
            int width = (int)(SelectionRect.Width * xRatio);
            int height = (int)(SelectionRect.Height * yRatio);

            // Bounds check & Clip to film
            int origLeft = left;
            int origTop = top;
            
            if (left < 0) { width += left; left = 0; }
            if (top < 0) { height += top; top = 0; }
            if (left + width > bitmapSource.PixelWidth) width = (int)bitmapSource.PixelWidth - left;
            if (top + height > bitmapSource.PixelHeight) height = (int)bitmapSource.PixelHeight - top;

            try
            {
                double sumR = 0, sumG = 0, sumB = 0;
                int count = 0;

                if (width > 0 && height > 0)
                {
                    if (_redChannel != null)
                    {
                        // Use High-Precision Raw Data
                        for (int r = top; r < top + height; r++)
                        {
                            for (int c = left; c < left + width; c++)
                            {
                                sumR += _redChannel[r, c];
                                sumG += _greenChannel[r, c];
                                sumB += _blueChannel[r, c];
                                count++;
                            }
                        }
                    }
                    else
                    {
                        // Fallback to BitmapSource
                        Int32Rect region = new Int32Rect(left, top, width, height);
                        int stride = (region.Width * bitmapSource.Format.BitsPerPixel + 7) / 8;
                        byte[] pixels = new byte[region.Height * stride];
                        bitmapSource.CopyPixels(region, pixels, stride, 0);

                        int bytesPerPixel = bitmapSource.Format.BitsPerPixel / 8;
                        for (int i = 0; i < pixels.Length; i += bytesPerPixel)
                        {
                            if (bitmapSource.Format == PixelFormats.Bgr24 || bitmapSource.Format == PixelFormats.Bgr32 || bitmapSource.Format == PixelFormats.Bgra32)
                            {
                                sumB += pixels[i];
                                sumG += pixels[i + 1];
                                sumR += pixels[i + 2];
                                count++;
                            }
                        }
                    }
                }

                if (count > 0)
                {
                    double avgR = sumR / count;
                    double avgG = sumG / count;
                    double avgB = sumB / count;

                    if (_isMeasurementMode)
                    {
                        double sumDose = 0;
                        double sumSqDose = 0;
                        if (_doseMap != null)
                        {
                            for (int r = top; r < top + height; r++) {
                                for (int c = left; c < left + width; c++) {
                                    double d = _doseMap[r, c];
                                    sumDose += d;
                                    sumSqDose += d * d;
                                }
                            }
                        }

                        double meanDose = (count > 0) ? sumDose / count : 0;
                        double stdDose = (count > 1) ? Math.Sqrt(Math.Max(0, (sumSqDose / count) - (meanDose * meanDose))) : 0;

                        string doseMsg = (_doseMap != null) ? $"\n\nMean Dose: {meanDose:F3} ± {stdDose:F2} cGy" : "\n\n(No Dose Map)";

                        System.Windows.MessageBox.Show(
                            $"ROI Measurement ({count:N0} pixels)\n\n" +
                            $"Avg Red:   {avgR:F1}\n" +
                            $"Avg Green: {avgG:F1}\n" +
                            $"Avg Blue:  {avgB:F1}" + doseMsg,
                            "Measurement Results");
                        
                        _isMeasurementMode = false;
                        _isSelectingROI = false;
                        ROIModeOverlay.Visibility = Visibility.Collapsed;
                        SelectionRect.Visibility = Visibility.Collapsed;

                        StatusText.Text = "Ready";
                        StatusIndicator.Background = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        // 4. Prompt for Dose using ContentDialog
                        var doseInput = new Wpf.Ui.Controls.TextBox { PlaceholderText = "Enter Dose (Gy)", Margin = new Thickness(0, 10, 0, 0) };
                        var doseDialog = new ContentDialog { Title = "Add Calibration Point", Content = doseInput, PrimaryButtonText = "Add Point", CloseButtonText = "Discard" };
                        var res = await _dialogService.ShowAsync(doseDialog, System.Threading.CancellationToken.None);

                        if (res == ContentDialogResult.Primary)
                        {
                            double.TryParse(doseInput.Text, out double dose);
                            CalibrationPoints.Add(new CalibrationPoint { Dose = dose, Red = avgR, Green = avgG, Blue = avgB });
                            StatusText.Text = $"Extracted: {dose} Gy";
                            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
                        }
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Selection is outside of the image bounds.", "Sampling Error");
                }

                // Force selection hide after any interactive prompt
                SelectionRect.Visibility = Visibility.Collapsed;
                SelectionCrosshairH.Visibility = Visibility.Collapsed;
                SelectionCrosshairV.Visibility = Visibility.Collapsed;

                // Ensure rulers and display refresh once
                UpdateRulers();
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during pixel sampling: {ex.Message}", "Sampling Error");
            }
        }


        #region Spatial Ruler & MM Logic

        private void UpdateStatusCoordinates(System.Windows.Input.MouseEventArgs e)
        {
            if (MainDisplayImage.Source == null || _imgWidth == 0) return;

            // Get mouse position relative to the DISPLAY PANEL (SelectionCanvas)
            System.Windows.Point mPos = e.GetPosition(SelectionCanvas);

            // True Center of the Panel (The Visual Zero)
            double centerX = SelectionCanvas.ActualWidth / 2.0;
            double centerY = SelectionCanvas.ActualHeight / 2.0;

            // mm per display pixel (based on the film's current scaling factor)
            System.Windows.Rect bounds = GetRenderedImageBounds(MainDisplayImage);
            if (bounds.Width <= 0) return;
            
            double mm_per_pixel = 25.4 / _tiffDpi;
            double display_pixels_per_mm = bounds.Width / (_imgWidth * mm_per_pixel);

            // Coordinates relative to the Panel Center
            double mmX = (mPos.X - centerX) / display_pixels_per_mm;
            double mmY = (mPos.Y - centerY) / display_pixels_per_mm;

            try { 
                if (CoordText != null)
                    CoordText.Text = string.Format("{0:F1}, {1:F1} mm", mmX, mmY);
            } catch { }
        }

        private void UpdateRulers()
        {
            if (MainDisplayImage.Source == null || _imgWidth == 0) return;

            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                TopRulerCanvas.Children.Clear();
                BottomRulerCanvas.Children.Clear();
                LeftRulerCanvas.Children.Clear();
                RightRulerCanvas.Children.Clear();

                System.Windows.Rect bounds = GetRenderedImageBounds(MainDisplayImage);
                if (bounds.Width <= 0 || bounds.Height <= 0) return;

                double mm_per_pixel = 25.4 / _tiffDpi;
                double display_pixels_per_mm = bounds.Width / (_imgWidth * mm_per_pixel);

                var rulerBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 0, 0, 0));
                var minorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 100, 100, 100));

                double canvasW = TopRulerCanvas.ActualWidth;
                double canvasH = LeftRulerCanvas.ActualHeight;
                if (canvasW <= 0) canvasW = 2000;
                if (canvasH <= 0) canvasH = 2000;

                double centerX = canvasW / 2.0;
                double centerY = canvasH / 2.0;

                // --- 1. HORIZONTAL RULERS (Top & Bottom) ---
                double topH = 30;
                TopRulerCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = 0, X2 = canvasW, Y1 = topH - 1, Y2 = topH - 1, Stroke = rulerBrush, StrokeThickness = 1 });
                BottomRulerCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = 0, X2 = canvasW, Y1 = 0.5, Y2 = 0.5, Stroke = rulerBrush, StrokeThickness = 1 });

                for (double mm = -500; mm <= 500; mm += 1.0)
                {
                    double x = centerX + (mm * display_pixels_per_mm);
                    if (x < 0 || x > canvasW) continue;

                    bool isMajor = (System.Math.Abs(mm) % 50 < 0.001);
                    bool isMid = (System.Math.Abs(mm) % 10 < 0.001);
                    bool isSmall = (System.Math.Abs(mm) % 5 < 0.001);

                    TopRulerCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = x, X2 = x, Stroke = isMajor ? rulerBrush : minorBrush, StrokeThickness = isMajor ? 1.2 : 0.6, Y1 = isMajor ? 0 : (isMid ? 15 : (isSmall ? 22 : 26)), Y2 = topH });
                    BottomRulerCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = x, X2 = x, Stroke = isMajor ? rulerBrush : minorBrush, StrokeThickness = isMajor ? 1.2 : 0.6, Y1 = 0, Y2 = isMajor ? 30 : (isMid ? 15 : (isSmall ? 8 : 4)) });

                    if (isMajor)
                    {
                        TopRulerCanvas.Children.Add(new System.Windows.Controls.TextBlock { Text = mm.ToString("0"), FontSize = 10, FontWeight = System.Windows.FontWeights.Bold, Foreground = rulerBrush, Margin = new System.Windows.Thickness(x + 2, 2, 0, 0) });
                        BottomRulerCanvas.Children.Add(new System.Windows.Controls.TextBlock { Text = mm.ToString("0"), FontSize = 10, FontWeight = System.Windows.FontWeights.Bold, Foreground = rulerBrush, Margin = new System.Windows.Thickness(x + 2, 16, 0, 0) });
                    }
                }

                // --- 2. VERTICAL RULERS (Left & Right) ---
                double leftW = 40;
                LeftRulerCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = leftW - 1, X2 = leftW - 1, Y1 = 0, Y2 = canvasH, Stroke = rulerBrush, StrokeThickness = 1 });
                RightRulerCanvas.Children.Add(new System.Windows.Shapes.Line { X1 = 0.5, X2 = 0.5, Y1 = 0, Y2 = canvasH, Stroke = rulerBrush, StrokeThickness = 1 });

                for (double mm = -500; mm <= 500; mm += 1.0)
                {
                    double y = centerY + (mm * display_pixels_per_mm);
                    if (y < 0 || y > canvasH) continue;

                    bool isMajor = (System.Math.Abs(mm) % 50 < 0.001);
                    bool isMid = (System.Math.Abs(mm) % 10 < 0.001);
                    bool isSmall = (System.Math.Abs(mm) % 5 < 0.001);

                    LeftRulerCanvas.Children.Add(new System.Windows.Shapes.Line { Y1 = y, Y2 = y, Stroke = isMajor ? rulerBrush : minorBrush, StrokeThickness = isMajor ? 1.2 : 0.6, X1 = isMajor ? 0 : (isMid ? 20 : (isSmall ? 30 : 35)), X2 = leftW });
                    RightRulerCanvas.Children.Add(new System.Windows.Shapes.Line { Y1 = y, Y2 = y, Stroke = isMajor ? rulerBrush : minorBrush, StrokeThickness = isMajor ? 1.2 : 0.6, X1 = 0, X2 = isMajor ? 40 : (isMid ? 20 : (isSmall ? 10 : 5)) });

                    if (isMajor)
                    {
                        LeftRulerCanvas.Children.Add(new System.Windows.Controls.TextBlock { Text = mm.ToString("0"), FontSize = 10, FontWeight = System.Windows.FontWeights.Bold, Foreground = rulerBrush, RenderTransform = new System.Windows.Media.RotateTransform(-90), Margin = new System.Windows.Thickness(2, y - 2, 0, 0) });
                        RightRulerCanvas.Children.Add(new System.Windows.Controls.TextBlock { Text = mm.ToString("0"), FontSize = 10, FontWeight = System.Windows.FontWeights.Bold, Foreground = rulerBrush, RenderTransform = new System.Windows.Media.RotateTransform(-90), Margin = new System.Windows.Thickness(26, y - 2, 0, 0) });
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private System.Windows.Rect GetRenderedImageBounds(System.Windows.Controls.Image img)
        {
            if (img.Source == null) return new System.Windows.Rect(0, 0, 0, 0);

            // Use the Parent Container (ImageGrid) as the logical boundary
            // This ensures (controlWidth, controlHeight) represents the full panel, 
            // accounting for any vertical/horizontal empty space.
            double controlWidth = SelectionCanvas.ActualWidth;
            double controlHeight = SelectionCanvas.ActualHeight;
            
            // If SelectionCanvas isn't loaded yet, fallback to ImageGrid (the parent)
            if (controlWidth == 0) controlWidth = ImageGrid.ActualWidth;
            if (controlHeight == 0) controlHeight = ImageGrid.ActualHeight;

            double imageWidth = img.Source.Width;
            double imageHeight = img.Source.Height;

            double ratioX = controlWidth / imageWidth;
            double ratioY = controlHeight / imageHeight;
            double ratio = System.Math.Min(ratioX, ratioY);

            double renderedWidth = imageWidth * ratio;
            double renderedHeight = imageHeight * ratio;

            // Absolute centering within the panel
            double left = (controlWidth - renderedWidth) / 2.0;
            double top = (controlHeight - renderedHeight) / 2.0;

            return new System.Windows.Rect(left, top, renderedWidth, renderedHeight);
        }

        private void ShowDoseToggle_Click(object sender, RoutedEventArgs e)
        {
            if (ShowDoseToggle.IsChecked == true)
            {
                if (_doseMap == null) { ShowDoseToggle.IsChecked = false; return; }
                _isShowingDoseMap = true;
                MainDisplayImage.Source = GenerateDoseHeatmap();
            }
            else
            {
                _isShowingDoseMap = false;
                MainDisplayImage.Source = _rawImageSource;
            }
        }

        private async void ConvertToDose_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentConfig == null || !CurrentConfig.IsValid)
            {
                System.Windows.MessageBox.Show("Please load or calculate a calibration first.", "No Calibration");
                return;
            }
            if (_redChannel == null)
            {
                System.Windows.MessageBox.Show("Please load a film scan first.", "No Image");
                return;
            }

            StatusText.Text = "Converting to Dose...";
            StatusIndicator.Background = new SolidColorBrush(Colors.Orange);

            try
            {
                int w = _imgWidth;
                int h = _imgHeight;
                _doseMap = new double[h, w];
                _rawImageSource = MainDisplayImage.Source;

                var config = CurrentConfig;
                double delta = config.DeltaOpt;
                string mode = config.Channel;

                await Task.Run(() =>
                {
                    Parallel.For(0, h, y =>
                    {
                        for (int x = 0; x < w; x++)
                        {
                            _doseMap[y, x] = CalculateSinglePixelDose(x, y, mode, config, delta);
                        }
                    });
                });

                var heatmap = GenerateDoseHeatmap();
                MainDisplayImage.Source = heatmap;
                
                _isShowingDoseMap = true;
                ShowDoseToggle.IsChecked = true;
                ShowDoseToggle.IsEnabled = true;

                double maxDose = 0.001;
                foreach (var d in _doseMap) if (d > maxDose) maxDose = d;
                DoseRangeText.Text = $"0.0 - {maxDose:F2} cGy";

                StatusText.Text = "Conversion Complete";
                StatusIndicator.Background = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Conversion failed: {ex.Message}");
            }
        }

        #region Command Stubs

        private void BrowseImage() => Open_Click(null!, null!);
        private void ProcessImage() => ConvertToDose_Click(null!, null!);
        private void ExportResults() => ExportDoseMap_Click(null!, null!);

        private void ExportDoseMap_Click(object sender, RoutedEventArgs e)
        {
            if (_doseMap == null)
            {
                System.Windows.MessageBox.Show("No dose map available to export. Please convert to dose first.");
                return;
            }

            var dlg = new SaveFileDialog { Filter = "Text Files|*.txt|All Files|*.*", FileName = "DoseMap_Export.txt" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(dlg.FileName))
                    {
                        writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd}");
                        writer.WriteLine($"DPI: {_tiffDpi:F1}");
                        writer.WriteLine($"Interpolation: 1");
                        writer.WriteLine($"X Res: {_imgWidth}");
                        writer.WriteLine($"Y Res: {_imgHeight}");
                        writer.WriteLine();
                        writer.WriteLine();
                        writer.WriteLine("Array Start:");

                        for (int y = 0; y < _imgHeight; y++)
                        {
                            var sb = new StringBuilder();
                            for (int x = 0; x < _imgWidth; x++)
                            {
                                sb.Append(_doseMap[y, x].ToString("F4", CultureInfo.InvariantCulture));
                                if (x < _imgWidth - 1) sb.Append("\t");
                            }
                            writer.WriteLine(sb.ToString());
                        }

                        writer.WriteLine();
                        writer.WriteLine(":Array End");
                    }
                    StatusText.Text = "Dose Map Exported";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error exporting dose map: {ex.Message}");
                }
            }
        }

        private void ImportDoseMap_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var reader = new System.IO.StreamReader(dlg.FileName))
                    {
                        // Parse header
                        string? line;
                        double dpi = 72;
                        int width = 0, height = 0;

                        // Read 5 header lines
                        for (int i = 0; i < 5; i++)
                        {
                            line = reader.ReadLine();
                            if (string.IsNullOrEmpty(line)) continue;
                            var parts = line.Split(':');
                            if (parts.Length < 2) continue;
                            var key = parts[0].Trim();
                            var val = parts[1].Trim();
                            if (key.Contains("DPI")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out dpi);
                            else if (key.Contains("X Res")) int.TryParse(val, out width);
                            else if (key.Contains("Y Res")) int.TryParse(val, out height);
                        }

                        if (width == 0 || height == 0)
                        {
                            System.Windows.MessageBox.Show("Invalid header format (Width or Height is zero).");
                            return;
                        }

                        // Look for Array Start:
                        bool foundStart = false;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("Array Start:")) 
                            { 
                                foundStart = true;
                                break; 
                            }
                        }

                        if (!foundStart)
                        {
                            System.Windows.MessageBox.Show("Could not find 'Array Start:' marker.");
                            return;
                        }

                        _doseMap = new double[height, width];
                        for (int y = 0; y < height; y++)
                        {
                            line = reader.ReadLine();
                            if (line == null || line.Contains(":Array End")) break;
                            var values = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int x = 0; x < width && x < values.Length; x++)
                            {
                                double.TryParse(values[x], NumberStyles.Any, CultureInfo.InvariantCulture, out _doseMap[y, x]);
                            }
                        }

                        _imgWidth = width;
                        _imgHeight = height;
                        _tiffDpi = dpi;
                        _isShowingDoseMap = true;

                        UpdateImageMetadata(dlg.FileName, width, height, dpi);
                        MainDisplayImage.Source = GenerateDoseHeatmap();
                        ShowDoseToggle.IsChecked = true;
                        ShowDoseToggle.IsEnabled = true;
                        UpdateRulers();

                        StatusText.Text = "Dose Map Imported";
                        StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22"));
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error importing dose map: {ex.Message}");
                }
            }
        }
        private void OpenAlignmentWindow() => ManuallyAlign_Click(null!, null!);
        private void OpenJawSizeWindow() => JawSizeMenu_Click(null!, null!);
        private void OpenGammaWindow() => System.Windows.MessageBox.Show("Gamma analysis feature coming soon.", "Feature Not Available");

        #endregion

        #endregion

        private double CalculateSinglePixelDose(int x, int y, string mode, CalibrationConfig config, double delta)
        {
            try
            {
                double rValue = _redChannel != null ? _redChannel[y, x] : 0;
                double gValue = _greenChannel != null ? _greenChannel[y, x] : 0;
                double bValue = _blueChannel != null ? _blueChannel[y, x] : 0;

                double rOD = -Math.Log10(Math.Max(rValue, 1) / 65535.0);
                double gOD = -Math.Log10(Math.Max(gValue, 1) / 65535.0);
                double bOD = -Math.Log10(Math.Max(bValue, 1) / 65535.0);

                if (mode.Contains("Single") && mode.Contains("Red"))
                {
                    return Math.Max(0, FittingMath.PolyVal(config.FirstFit, rOD));
                }
                else if (mode.Contains("Single") && mode.Contains("Green"))
                {
                    return Math.Max(0, FittingMath.PolyVal(config.FirstFit, gOD));
                }
                else if (mode.Contains("Single") && mode.Contains("Blue"))
                {
                    return Math.Max(0, FittingMath.PolyVal(config.FirstFit, bOD));
                }
                else if (mode.Contains("Dual") && mode.Contains("Red"))
                {
                    double ratio = rOD / (bOD + 2.22e-16);
                    double firstPass = FittingMath.PolyVal(config.FirstFit, ratio);
                    return Math.Max(0, FittingMath.PolyVal(config.SecondFit, firstPass));
                }
                else if (mode.Contains("Dual") && mode.Contains("Green"))
                {
                    double ratio = gOD / (bOD + 2.22e-16);
                    double firstPass = FittingMath.PolyVal(config.FirstFit, ratio);
                    return Math.Max(0, FittingMath.PolyVal(config.SecondFit, firstPass));
                }
                else if (mode.Contains("Triple"))
                {
                    double doseR = FittingMath.PolyVal(config.FirstFit, rOD);
                    double doseG = FittingMath.PolyVal(config.SecondFit, gOD);
                    double doseB = FittingMath.PolyVal(config.ThirdFit, bOD);

                    return Math.Max(0, ((doseR + doseG + doseB) / 3.0) * delta);
                }
            }
            catch { }
            return 0;
        }

        private BitmapSource GenerateDoseHeatmap()
        {
            int h = _imgHeight;
            int w = _imgWidth;
            int stride = w * 4;
            byte[] pixels = new byte[h * stride];

            double maxDose = 0.001;
            foreach (var d in _doseMap) if (d > maxDose) maxDose = d;

            // Apply Contrast (Scaling) to dose map
            double doseContrast = Math.Pow(2.0, (ContrastSlider.Value - 128) / 64.0);
            maxDose /= doseContrast;

            string mapName = (ColorMapDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Jet";

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    double d = _doseMap[y, x];
                    var (R, G, B) = GetColorFromMap(d / maxDose, mapName);
                    int idx = y * stride + x * 4;
                    pixels[idx] = B;
                    pixels[idx + 1] = G;
                    pixels[idx + 2] = R;
                    pixels[idx + 3] = 255;
                }
            });

            return BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        }

        private (byte R, byte G, byte B) GetColorFromMap(double v, string mapName)
        {
            v = Math.Clamp(v, 0, 1);
            return mapName switch
            {
                "Hot" => GetHotColor(v),
                "Viridis" => GetViridisColor(v),
                "Gray" => GetGrayColor(v),
                _ => GetJetColor(v)
            };
        }

        private (byte R, byte G, byte B) GetJetColor(double v)
        {
            double r = 0, g = 0, b = 0;
            if (v < 0.25) { r = 0; g = 4 * v; b = 1; }
            else if (v < 0.5) { r = 0; g = 1; b = 1 + 4 * (0.25 - v); }
            else if (v < 0.75) { r = 4 * (v - 0.5); g = 1; b = 0; }
            else { r = 1; g = 1 + 4 * (0.75 - v); b = 0; }
            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private (byte R, byte G, byte B) GetHotColor(double v)
        {
            double r, g, b;
            if (v < 0.33) { r = 3 * v; g = 0; b = 0; }
            else if (v < 0.66) { r = 1; g = 3 * (v - 0.33); b = 0; }
            else { r = 1; g = 1; b = 3 * (v - 0.66); }
            return ((byte)(Math.Clamp(r, 0, 1) * 255), (byte)(Math.Clamp(g, 0, 1) * 255), (byte)(Math.Clamp(b, 0, 1) * 255));
        }

        private (byte R, byte G, byte B) GetGrayColor(double v)
        {
            byte val = (byte)(v * 255);
            return (val, val, val);
        }

        private (byte R, byte G, byte B) GetViridisColor(double v)
        {
            // Simple 3-point approximation: Purple -> Green -> Yellow
            double r, g, b;
            if (v < 0.5) {
                double t = v * 2;
                r = 0.26 + 0.1 * t; g = 0.0 + 0.6 * t; b = 0.33 + 0.1 * t;
            } else {
                double t = (v - 0.5) * 2;
                r = 0.36 + 0.6 * t; g = 0.6 + 0.3 * t; b = 0.43 - 0.3 * t;
            }
            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_redChannel != null || _doseMap != null) RefreshDisplay();
        }

        private void ColorMapDropDown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isShowingDoseMap && _doseMap != null) RefreshDisplay();
        }

        #region Measurement Helpers

        private Point ControlToPixel(Point controlPoint)
        {
            if (MainDisplayImage.Source is not BitmapSource bs) return controlPoint;
            System.Windows.Rect renderedRect = GetRenderedImageBounds(MainDisplayImage);
            if (renderedRect.Width <= 0 || renderedRect.Height <= 0) return controlPoint;

            double xRatio = bs.PixelWidth / renderedRect.Width;
            double yRatio = bs.PixelHeight / renderedRect.Height;

            double xInImage = (controlPoint.X - renderedRect.X) * xRatio;
            double yInImage = (controlPoint.Y - renderedRect.Y) * yRatio;

            return new Point(xInImage, yInImage);
        }

        private double CalculateDistance(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            double pixelDist = Math.Sqrt(dx * dx + dy * dy);
            return pixelDist * 25.4 / _tiffDpi;
        }

        private double CalculateArea(List<Point> points)
        {
            if (points == null || points.Count < 3) return 0;
            var pixelPoints = points.Select(p => ControlToPixel(p)).ToList();
            double area = 0;
            int j = pixelPoints.Count - 1;
            for (int i = 0; i < pixelPoints.Count; i++)
            {
                area += (pixelPoints[j].X + pixelPoints[i].X) * (pixelPoints[j].Y - pixelPoints[i].Y);
                j = i;
            }
            double areaPixels2 = Math.Abs(area / 2.0);
            double factor = 25.4 / _tiffDpi;
            return areaPixels2 * factor * factor;
        }

        #endregion
    }
}
