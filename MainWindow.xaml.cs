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

namespace FilmAnalysis
{
    public class AppSettings
    {
        public int FixedWidth { get; set; } = 100;
        public int FixedHeight { get; set; } = 100;
        public bool LastWasFixed { get; set; } = false;
        public string CalibrationsPath { get; set; } = string.Empty;
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
        public ObservableCollection<CalibrationPoint> CalibrationPoints { get; set; }
        public CalibrationConfig CurrentConfig { get; set; }
        private readonly IContentDialogService _dialogService = new ContentDialogService();

        // Application State
        private bool _isSelectingROI = false;
        private bool _isDrawing = false;
        private bool _isFixedMode = false;
        private Point _startPoint;
        private AppSettings _settings = new AppSettings();

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
                    pathInput.Text = dlg.FolderName;
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

                    // Clear raw high-precision data if standard image is loaded
                    _redChannel = null;
                    _greenChannel = null;
                    _blueChannel = null;
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
        }

        private void ReadTiffData(string filePath)
        {
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
            if (_redChannel == null) return;

            int width = _redChannel.GetLength(1);
            int height = _redChannel.GetLength(0);
            
            // 1. Find Min/Max for Scaling (Auto-Contrast like Matlab)
            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var v in _redChannel) { if (v < min) min = v; if (v > max) max = v; }
            foreach (var v in _greenChannel) { if (v < min) min = v; if (v > max) max = v; }
            foreach (var v in _blueChannel) { if (v < min) min = v; if (v > max) max = v; }

            if (max <= min) max = min + 1;

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
                        if (trimmed.StartsWith("Channel:")) newConfig.Channel = trimmed.Split(':')[1].Trim();
                        else if (trimmed.StartsWith("Degree:")) newConfig.Degree = int.Parse(trimmed.Split(':')[1].Trim());
                    }
                    else
                    {
                        switch (currentSection)
                        {
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
                    
                    // Update plot if possible (would need raw data saved in file too)
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
                    doseFit = xData.Select(x => FittingMath.PolyVal(CurrentConfig.FirstFit, x)).ToArray();
                } else if (channelMode.Contains("Dual")) {
                    double[] ratio = channelMode.Contains("Red") ? rNorm.Zip(bNorm, (r, b) => r / (b + 1e-9)).ToArray() : gNorm.Zip(bNorm, (g, b) => g / (b + 1e-9)).ToArray();
                    double[] primary = channelMode.Contains("Red") ? rNorm : gNorm;
                    CurrentConfig.FirstFit = FittingMath.PolyFit(ratio, primary, degree);
                    double[] val1 = ratio.Select(r => FittingMath.PolyVal(CurrentConfig.FirstFit, r)).ToArray();
                    CurrentConfig.SecondFit = FittingMath.PolyFit(val1, doses, degree);
                    CurrentConfig.RSquared = FittingMath.CalculateRSquared(val1, doses, CurrentConfig.SecondFit);
                    doseFit = val1.Select(v => FittingMath.PolyVal(CurrentConfig.SecondFit, v)).ToArray();
                } else if (channelMode.Contains("Triple")) {
                    CurrentConfig.FirstFit = FittingMath.PolyFit(rNorm, doses, degree);
                    CurrentConfig.SecondFit = FittingMath.PolyFit(gNorm, doses, degree);
                    CurrentConfig.ThirdFit = FittingMath.PolyFit(bNorm, doses, degree);
                    CurrentConfig.DeltaOpt = FittingMath.OptimizeTripleChannelDelta(rNorm, gNorm, bNorm, CurrentConfig.FirstFit, CurrentConfig.SecondFit, CurrentConfig.ThirdFit);
                    doseFit = new double[doses.Length];
                    for (int i = 0; i < doses.Length; i++) {
                        double rD = FittingMath.PolyVal(CurrentConfig.FirstFit, rNorm[i] * CurrentConfig.DeltaOpt);
                        double gD = FittingMath.PolyVal(CurrentConfig.SecondFit, gNorm[i] * CurrentConfig.DeltaOpt);
                        double bD = FittingMath.PolyVal(CurrentConfig.ThirdFit, bNorm[i] * CurrentConfig.DeltaOpt);
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
                    }
                    StatusText.Text = "Saved Config";
                    RefreshConfigs(); // Auto-refresh dropdown
                } catch (Exception ex) { System.Windows.MessageBox.Show("Save failed: " + ex.Message); }
            }
        }

        private void AutoCenter_Click(object sender, RoutedEventArgs e) { }
        private void ManuallyAlign_Click(object sender, RoutedEventArgs e) { }
        private void AutoAlign_Click(object sender, RoutedEventArgs e) { }
        private void Rotation_Click(object sender, RoutedEventArgs e) { }
        private void Flip_Click(object sender, RoutedEventArgs e) { }
        private void Crop_Click(object sender, RoutedEventArgs e) { }
        private void Filter_Click(object sender, RoutedEventArgs e) { }
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
            SelectionRect.Visibility = Visibility.Collapsed;
            SelectionCrosshairH.Visibility = Visibility.Collapsed;
            SelectionCrosshairV.Visibility = Visibility.Collapsed;
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
            if (!_isSelectingROI) return;

            if (_isFixedMode)
            {
                // Finalize fixed ROI position exactly where the click happened
                await PerformROIExtraction();
                // Rulers can be refreshed once here
                UpdateRulers();
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
            await PerformROIExtraction();
        }

                private bool _isMeasurementMode = false;

        private async void Measurement_Click(object sender, RoutedEventArgs e)
        {
            if (MainDisplayImage.Source == null) return;
            
            _isMeasurementMode = true;
            _isSelectingROI = true;
            ROIModeOverlay.Visibility = Visibility.Visible;
            StatusText.Text = "Measurement Mode (Select ROI)";
            StatusIndicator.Background = new SolidColorBrush(Colors.MediumPurple);
            
            if (_settings.LastWasFixed)
            {
                RefreshFixedROISize();
                SelectionRect.Visibility = Visibility.Visible;
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

        private double CalculateSinglePixelDose(int x, int y, string mode, CalibrationConfig config, double delta)
        {
            const double eps = 2.22e-16;
            try
            {
                double rValue = _redChannel != null ? _redChannel[y, x] : 0;
                double gValue = _greenChannel != null ? _greenChannel[y, x] : 0;
                double bValue = _blueChannel != null ? _blueChannel[y, x] : 0;

                double rOD = -Math.Log10(rValue / 65535.0 + eps);
                double gOD = -Math.Log10(gValue / 65535.0 + eps);
                double bOD = -Math.Log10(bValue / 65535.0 + eps);

                if (mode == "Red")
                {
                    return Math.Max(0, FittingMath.PolyVal(config.FirstFit, rOD));
                }
                else if (mode == "Green")
                {
                    return Math.Max(0, FittingMath.PolyVal(config.FirstFit, gOD));
                }
                else if (mode == "Blue")
                {
                    return Math.Max(0, FittingMath.PolyVal(config.FirstFit, bOD));
                }
                else if (mode == "Red/Blue")
                {
                    double ratio = rOD / bOD;
                    double firstPass = FittingMath.PolyVal(config.FirstFit, ratio);
                    return Math.Max(0, FittingMath.PolyVal(config.SecondFit, firstPass));
                }
                else if (mode == "Green/Blue")
                {
                    double ratio = gOD / bOD;
                    double firstPass = FittingMath.PolyVal(config.FirstFit, ratio);
                    return Math.Max(0, FittingMath.PolyVal(config.SecondFit, firstPass));
                }
                else if (mode.Contains("|") || mode.StartsWith("Triple"))
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

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    double d = _doseMap[y, x];
                    var (R, G, B) = GetJetColor(d / maxDose);
                    int idx = y * stride + x * 4;
                    pixels[idx] = B;
                    pixels[idx + 1] = G;
                    pixels[idx + 2] = R;
                    pixels[idx + 3] = 255;
                }
            });

            return BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        }

        private (byte R, byte G, byte B) GetJetColor(double v)
        {
            v = Math.Clamp(v, 0, 1);
            double r = 0, g = 0, b = 0;
            if (v < 0.25) { r = 0; g = 4 * v; b = 1; }
            else if (v < 0.5) { r = 0; g = 1; b = 1 + 4 * (0.25 - v); }
            else if (v < 0.75) { r = 4 * (v - 0.5); g = 1; b = 0; }
            else { r = 1; g = 1 + 4 * (0.75 - v); b = 0; }
            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        #endregion
    }
}
