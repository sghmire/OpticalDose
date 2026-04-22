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
using System.Printing;

namespace OpticalDose
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public ICommand BrowseCommand => new FilmRelayCommand(BrowseImage);
        public ICommand ProcessCommand => new FilmRelayCommand(ProcessImage);
        public ICommand ExportCommand => new FilmRelayCommand(ExportResults);
        public ICommand AlignmentCommand => new FilmRelayCommand(OpenAlignmentWindow);
        public ICommand FieldSizeCommand => new FilmRelayCommand(OpenFieldSizeWindow);
        public ICommand GammaCommand => new FilmRelayCommand(OpenGammaWindow);
        public ObservableCollection<CalibrationPoint> CalibrationPoints { get; set; } = new();
        public CalibrationConfig? CurrentConfig { get; set; }
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
        public AppSettings _settings = new AppSettings();
        private FieldSizeWindow? _fieldSizeWindow;

        // Measurement State
        private enum MeasurementMode { None, ROIDose, Distance, Area, Crosshairs }
        private MeasurementMode _activeMeasurementMode = MeasurementMode.None;
        private bool _isAreaRectMode = false;
        private List<Point> _measurementPoints = new();
        private bool _isPickingCenter = false;
        private Point? _referenceCenterPixel;

        // Raw High-Precision Image Data
        private double[,]? _redChannel;
        private double[,]? _greenChannel;
        private double[,]? _blueChannel;
        private int _imgWidth, _imgHeight;
        private double _dpiX = 72, _dpiY = 72; // Independent X and Y DPI
        private string _calibrationsFolder = "";

        // Dosimetry States
        private double[,]? _doseMap;
        private double[,]? _filmDoseMap; // Dedicated film dose storage
        private double _filmDpiX, _filmDpiY;
        private ImageSource? _rawImageSource;
        private bool _isShowingDoseMap = false;
        
        // Imported Plan Dose for direct Analysis loading
        private double[,]? _importedPlanDose;
        private double _importedPlanDpiX, _importedPlanDpiY;
        private double _importedPlanOriginX, _importedPlanOriginY;
        private double _importedPlanRefX, _importedPlanRefY, _importedPlanRefZ;
        private double _importedPlanSpacingYSign = 1.0;
        private string _importedPlanOrientation = "Z";
        private int _importedPlanFractions = 1;
        private string _activeFilmFileName = "None";
        private string _activeDicomFileName = "None";

        // Undo/Redo History
        private readonly Stack<ImageState> _undoStack = new();
        private readonly Stack<ImageState> _redoStack = new();

        public MainWindow()
        {
            InitializeComponent();
            _dialogService.SetContentPresenter(RootContentDialogPresenter);

            // Consolidate Sync logic in code-behind for reliability
            AnalysisComp.AnalysisRequested += AnalysisComp_SyncRequested;
            AnalysisComp.PlanRequested += AnalysisComp_SyncRequested;
            
            // Global Progress Reporting
            AnalysisComp.ProgressUpdate += (v) => GlobalProgressBar.Value = v;
            AnalysisComp.ProgressActive += (active) => GlobalProgressBar.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            
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
            InputBindings.Add(new KeyBinding(new RelayCommand(() => Undo_Click(null!, null!)), Key.Z, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new RelayCommand(() => Redo_Click(null!, null!)), Key.Y, ModifierKeys.Control));

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

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var snapshot = AnalysisComp.GetReportSnapshot();
                
                // Build FlowDocument similar to fn_PDFPrinter layout
                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40),
                    ColumnWidth = double.PositiveInfinity,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                };

                doc.Blocks.Add(new Paragraph(new Run("Optical Dose Analysis Report"))
                {
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(173, 216, 230)),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                var infoTable = new Table();
                infoTable.Columns.Add(new TableColumn { Width = new GridLength(150) });
                infoTable.Columns.Add(new TableColumn());
                infoTable.RowGroups.Add(new TableRowGroup());
                void AddInfo(string label, string value)
                {
                    var row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                    infoTable.RowGroups[0].Rows.Add(row);
                }
                AddInfo("Date", DateTime.Now.ToString("yyyy-MM-dd"));
                AddInfo("Film", snapshot.FilmFileName);
                AddInfo("Plan", snapshot.PlanFileName);
                AddInfo("Pass Rate", $"{snapshot.PassRate:F1} %");
                AddInfo("Gamma", $"{snapshot.DdPercent:F1}% / {snapshot.DtaMm:F1} mm ({snapshot.Mode}, Thresh {snapshot.ThresholdPercent:F1}%, Scale {snapshot.DoseScale:F3})");
                AddInfo("Shifts (mm)", $"X {snapshot.ShiftX:F2}, Y {snapshot.ShiftY:F2}");
                doc.Blocks.Add(infoTable);

                // Helper to add image block
                void AddImage(string title, BitmapSource src, double maxWidth)
                {
                    var p = new Paragraph(new Run(title)) { FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 6) };
                    doc.Blocks.Add(p);
                    var img = new System.Windows.Controls.Image
                    {
                        Source = src,
                        Stretch = System.Windows.Media.Stretch.Uniform,
                        Width = maxWidth
                    };
                    doc.Blocks.Add(new BlockUIContainer(img) { Margin = new Thickness(0, 0, 0, 10) });
                }

                AddImage("2D Plan vs Film", CombineSideBySide(snapshot.PlanImage, snapshot.FilmImage), 700);
                AddImage("Center Profiles", snapshot.ProfileImage, 700);
                AddImage("Gamma Map", snapshot.GammaImage, 700);

                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    doc.PageHeight = pd.PrintableAreaHeight;
                    doc.PageWidth = pd.PrintableAreaWidth;
                    pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Optical Dose Analysis Report");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unable to print report: {ex.Message}", "Print Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private static BitmapSource CombineSideBySide(BitmapSource left, BitmapSource right)
        {
            int height = Math.Max(left.PixelHeight, right.PixelHeight);

            int width = left.PixelWidth + right.PixelWidth;
            var drawing = new DrawingVisual();
            using (var dc = drawing.RenderOpen())
            {
                dc.DrawImage(left, new Rect(0, 0, left.PixelWidth, height));
                dc.DrawImage(right, new Rect(left.PixelWidth, 0, right.PixelWidth, height));
            }
            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawing);
            rtb.Freeze();
            return rtb;
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
            var tabControl = new System.Windows.Controls.TabControl { 
                Background = Brushes.Transparent, 
                BorderThickness = new Thickness(0),
                Margin = new Thickness(-10) // Negative margin to fill ContentDialog space
            };

            // --- Tab 1: General ---
            var generalStack = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
            generalStack.Children.Add(new System.Windows.Controls.TextBlock { 
                Text = "Calibration Data Path", 
                Style = (Style)this.FindResource("DosimetrySectionHeader"),
                Margin = new Thickness(0,0,0,10) 
            });

            var pathGrid = new System.Windows.Controls.Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pathInput = new System.Windows.Controls.TextBox { 
                Text = _calibrationsFolder, 
                IsReadOnly = true,
                Margin = new Thickness(0,0,10,0)
            };
            
            var browseBtn = new Wpf.Ui.Controls.Button { Content = "Browse..." };
            browseBtn.Click += (s, ev) => {
                var dlg = new Microsoft.Win32.OpenFolderDialog();
                dlg.InitialDirectory = _calibrationsFolder;
                if (dlg.ShowDialog() == true) { pathInput.Text = dlg.FolderName; }
            };

            System.Windows.Controls.Grid.SetColumn(pathInput, 0);
            System.Windows.Controls.Grid.SetColumn(browseBtn, 1);
            pathGrid.Children.Add(pathInput);
            pathGrid.Children.Add(browseBtn);
            generalStack.Children.Add(pathGrid);

            var generalTab = new System.Windows.Controls.TabItem { Header = "General", Content = generalStack };
            tabControl.Items.Add(generalTab);

            // --- Tab 2: Gamma Engine ---
            var gammaStack = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
            
            // Uncertainty
            gammaStack.Children.Add(new System.Windows.Controls.TextBlock { 
                Text = "Dose Uncertainty Factor (%)", 
                Style = (Style)this.FindResource("DosimetrySectionHeader"),
                Margin = new Thickness(0,0,0,5) 
            });
            var uncInput = new Wpf.Ui.Controls.NumberBox { Value = _settings.GammaUncertainty, Minimum = 0, Maximum = 10, SmallChange = 0.1, Margin = new Thickness(0,0,0,15) };
            gammaStack.Children.Add(uncInput);

            // Search Step
            gammaStack.Children.Add(new System.Windows.Controls.TextBlock { 
                Text = "Sub-pixel Search Resolution (px)", 
                Style = (Style)this.FindResource("DosimetrySectionHeader"),
                Margin = new Thickness(0,0,0,5) 
            });
            var stepInput = new Wpf.Ui.Controls.NumberBox { Value = _settings.GammaSearchStep, Minimum = 0.01, Maximum = 0.5, SmallChange = 0.05, Margin = new Thickness(0,0,0,15) };
            gammaStack.Children.Add(stepInput);

            // Smoothing
            gammaStack.Children.Add(new System.Windows.Controls.TextBlock { 
                Text = "Pre-Analysis Film Smoothing (mm)", 
                Style = (Style)this.FindResource("DosimetrySectionHeader"),
                Margin = new Thickness(0,0,0,5) 
            });
            var smoothInput = new Wpf.Ui.Controls.NumberBox { Value = _settings.GammaSmoothingSigma, Minimum = 0, Maximum = 2.0, SmallChange = 0.1, Margin = new Thickness(0,0,0,15) };
            gammaStack.Children.Add(smoothInput);

            // Interpolation Mode
            var bicubicCheck = new System.Windows.Controls.CheckBox { Content = "Use Bicubic Interpolation (Recommended)", IsChecked = _settings.GammaUseBicubic, Margin = new Thickness(0,0,0,5) };
            gammaStack.Children.Add(bicubicCheck);

            var gammaTab = new System.Windows.Controls.TabItem { Header = "Gamma Engine", Content = gammaStack };
            tabControl.Items.Add(gammaTab);

            var dialog = new ContentDialog(_dialogService.GetContentPresenter())
            {
                Title = "Application Settings",
                Content = tabControl,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _settings.CalibrationsPath = pathInput.Text;
                _calibrationsFolder = _settings.CalibrationsPath;
                
                // Save Gamma Settings
                _settings.GammaUncertainty = uncInput.Value ?? 2.0;
                _settings.GammaSearchStep = stepInput.Value ?? 0.1;
                _settings.GammaSmoothingSigma = smoothInput.Value ?? 0.0;
                _settings.GammaUseBicubic = bicubicCheck.IsChecked ?? true;

                if (!System.IO.Directory.Exists(_calibrationsFolder))
                {
                    try { System.IO.Directory.CreateDirectory(_calibrationsFolder); } catch { }
                }
                SaveSettings();
                RefreshConfigs();
                StatusText.Text = "Settings Updated";
            }
        }

        private async void About_Click(object sender, RoutedEventArgs e)
        {
            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };

            try
            {
                var img = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Icon.png")),
                    Width = 64,
                    Height = 64,
                    Margin = new Thickness(0, 0, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stack.Children.Add(img);
            }
            catch { /* Skip icon if not found */ }

            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Optical Dose",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Version 1.0.0",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Film dosimetry and analysis.\nDeveloped for clinical precision and efficiency.",
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = (Brush)FindResource("TextFillColorSecondaryBrush")
            });


            var dialog = new ContentDialog(_dialogService.GetContentPresenter())
            {
                Title = "About Optical Dose",
                Content = stack,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        private async void TutorialJawSize_Click(object sender, RoutedEventArgs e)
        {
            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            
            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "How to perform a FWHM Field Size test",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "• Irradiate Full and Half dose on two films\n" +
                       "• Extract ROI for 0, half dose and full dose\n" +
                       "• Poly fit with Degree: 2 and single channel: Green\n" +
                       ". Apply median filtering; kernel size: 3\n" +
                       ". Convert to dose map\n" +
                       ". Open Field Size tool\n" +
                       "• Align the film well with the ROI\n" +
                       "• Analyze",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 22,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var dialog = new Wpf.Ui.Controls.ContentDialog(_dialogService.GetContentPresenter())
            {
                Title = "Tutorial: FWHM Field Size",
                Content = stack,
                CloseButtonText = "Close",
                DefaultButton = Wpf.Ui.Controls.ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        private async void References_Click(object sender, RoutedEventArgs e)
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Height = 500 };
            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(10), MaxWidth = 520 };

            stack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Mathematical References",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            void AddSection(string title, string body)
            {
                stack.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = title,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 4)
                });
                stack.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = body,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            AddSection("Optical Density",
                "OD = -log10(pixel / 65535). Pixel floor at 1 to avoid log(0). Used per channel to linearize film response.");

            AddSection("Single-Channel Calibration",
                "Fit: dose = P(OD), where P is an n-degree polynomial (Vandermonde least squares) with ridge λ=1e-8 for stability. R² computed against reference doses.");

            AddSection("Dual-Channel Calibration",
                "Compute ratio = OD_primary / OD_blue. First polynomial maps ratio → surrogate OD. Second polynomial maps surrogate → dose. Dose = P2(P1(ratio)).");

            AddSection("Triple-Channel Calibration",
                "Channel fits: dose_r = Pr(OD_r), dose_g = Pg(OD_g), dose_b = Pb(OD_b). Delta is optimized to minimize Σ(avg(dose_r(δ·OD_r), dose_g(δ·OD_g), dose_b(δ·OD_b)) − dose_ref)². Final dose = mean of channel doses (with delta applied to OD inputs).");

            AddSection("Gamma Analysis",
                "Implements classic Low gamma: γ² = (ΔD / DD%)² + (Δr / DTA)². Options: global vs local normalization; uncertainty margin subtracts from |ΔD|; sub-pixel search step; bicubic interpolation. Thresholding skips pixels below (threshold% of max plan dose). ROI optional.");

            AddSection("Interpolation",
                "Nearest, bilinear, bicubic with clamped borders. Degenerate 1-pixel dimensions handled to avoid division by zero.");

            AddSection("Filtering",
                "Median, box, and Gaussian (σ → radius 3σ). Noise filter replaces NaN/Inf/outliers above threshold with 1.");

            AddSection("FWHM Field Size",
                "Profiles extracted across plateau window. Peak by method (Max/Mean/Median). Edges at 50% of peak via linear interpolation; FWHM = right − left. Reported mean ± SD across sampled rows/columns; coverage = plateauX × plateauY.");

            AddSection("Delta Optimization (Triple)",
                "Golden-section search in [0.8, 1.2], 50 iterations. Objective uses reference dose if provided; otherwise minimizes inter-channel disagreement.");

            scroll.Content = stack;

            var dialog = new ContentDialog(_dialogService.GetContentPresenter())
            {
                Title = "References",
                Content = scroll,
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
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
                    
                    _activeFilmFileName = dlg.FileName;
                    
                    // Update Metadata (DPI and Size are updated inside ReadTiffData or here)
                    long fileSize = new System.IO.FileInfo(dlg.FileName).Length;
                    MetaFileName.Text = System.IO.Path.GetFileName(dlg.FileName);
                    MetaImageSize.Text = $"{_imgWidth} x {_imgHeight}";
                    MetaDPI.Text = _dpiX == _dpiY ? _dpiX.ToString("F1") : $"{_dpiX:F1}x{_dpiY:F1}";
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
                        _dpiX = _dpiY = xRes * 2.54;
                    else
                        _dpiX = _dpiY = xRes;
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

        private const int MaxUndoStates = 10;

        private void PushUndo(string description)
        {
            _undoStack.Push(new ImageState
            {
                Red = ImageTransforms.CloneArray(_redChannel),
                Green = ImageTransforms.CloneArray(_greenChannel),
                Blue = ImageTransforms.CloneArray(_blueChannel),
                DoseMap = ImageTransforms.CloneArray(_doseMap),
                Width = _imgWidth,
                Height = _imgHeight,
                DpiX = _dpiX,
                DpiY = _dpiY,
                ShowingDose = _isShowingDoseMap,
                Description = description
            });
            _redoStack.Clear();

            // Cap undo stack to prevent unbounded memory growth
            while (_undoStack.Count > MaxUndoStates)
            {
                // Remove oldest items by rebuilding with only the newest MaxUndoStates
                var temp = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = Math.Min(temp.Length, MaxUndoStates) - 1; i >= 0; i--)
                    _undoStack.Push(temp[i]);
                break;
            }

            UpdateUndoRedoUI();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count == 0) return;

            // Save current state to redo stack
            _redoStack.Push(new ImageState
            {
                Red = ImageTransforms.CloneArray(_redChannel),
                Green = ImageTransforms.CloneArray(_greenChannel),
                Blue = ImageTransforms.CloneArray(_blueChannel),
                DoseMap = ImageTransforms.CloneArray(_doseMap),
                Width = _imgWidth,
                Height = _imgHeight,
                DpiX = _dpiX,
                DpiY = _dpiY,
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
                Red = ImageTransforms.CloneArray(_redChannel),
                Green = ImageTransforms.CloneArray(_greenChannel),
                Blue = ImageTransforms.CloneArray(_blueChannel),
                DoseMap = ImageTransforms.CloneArray(_doseMap),
                Width = _imgWidth,
                Height = _imgHeight,
                DpiX = _dpiX,
                DpiY = _dpiY,
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
            _dpiX = state.DpiX;
            _dpiY = state.DpiY;
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
            UpdateRulers();
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
            UpdateNavButtons(0);
        }

        private void MenuSelectDicom_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabControl == null) return;
            MainTabControl.SelectedIndex = 1;
            UpdateNavButtons(1);
        }

        private void MenuSelectAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabControl == null) return;
            MainTabControl.SelectedIndex = 2;
            UpdateNavButtons(2);
        }

        private void UpdateNavButtons(int index)
        {
            if (NavCalibrationButton != null) NavCalibrationButton.Tag = index == 0 ? "Active" : "";
            if (NavDicomButton != null) NavDicomButton.Tag = index == 1 ? "Active" : "";
            if (NavAnalysisButton != null) NavAnalysisButton.Tag = index == 2 ? "Active" : "";
        }

        private void AnalysisComp_SyncRequested(object? sender, EventArgs e)
        {
            SyncAllDataToAnalysis();
        }

        private void SyncAllDataToAnalysis()
        {
            bool hasFilm = _filmDoseMap != null;
            bool hasPlan = _importedPlanDose != null;

            if (hasFilm)
            {
                AnalysisComp.SetFilmDose(_filmDoseMap, _filmDpiX, _filmDpiY, _activeFilmFileName, _referenceCenterPixel);
            }

            if (hasPlan)
            {
                AnalysisComp.SetPlanDose(_importedPlanDose,
                                       _importedPlanDpiX, _importedPlanDpiY,
                                       _importedPlanRefX, _importedPlanRefY, _importedPlanRefZ,
                                       _importedPlanOriginX, _importedPlanOriginY,
                                       _importedPlanSpacingYSign, _importedPlanOrientation, 
                                       _activeDicomFileName, _importedPlanFractions);
            }

            if (hasFilm || hasPlan)
            {
                StatusText.Text = "Dose Maps Synced to Analysis Tab";
                StatusIndicator.Background = new SolidColorBrush(Colors.MediumSeaGreen);
            }
            else
            {
                System.Windows.MessageBox.Show("No dose map data available to sync. Please load or process images first.", "Sync Status");
            }
        }

        private void DicomViewer_DosePlaneExtracted(object sender, DoseExtractedEventArgs e)
        {
            if (e.DoseMap == null) return;
            
            _activeDicomFileName = e.FileName ?? "DICOM Set";

            // Update internal state to match extracted plane
            _importedPlanDose = e.DoseMap;
            _importedPlanDpiX = 25.4 / e.SpacingX;
            _importedPlanDpiY = 25.4 / e.SpacingY;
            _importedPlanRefX = e.RefX;
            _importedPlanRefY = e.RefY;
            _importedPlanRefZ = e.RefZ;
            _importedPlanOriginX = e.OriginX;
            _importedPlanOriginY = e.OriginY;
            _importedPlanSpacingYSign = e.SpacingYSign;
            _importedPlanOrientation = e.PlaneOrientation;
            _importedPlanFractions = e.NumberOfFractions;

            // Send to Analysis Component with full coordinate mapping
            AnalysisComp.SetPlanDose(_importedPlanDose, 
                                   _importedPlanDpiX, 
                                   _importedPlanDpiY,
                                   _importedPlanRefX, _importedPlanRefY, _importedPlanRefZ,
                                   _importedPlanOriginX, _importedPlanOriginY,
                                   _importedPlanSpacingYSign,
                                   _importedPlanOrientation,
                                   _activeDicomFileName,
                                   _importedPlanFractions);

            // Switching to Analysis Tab now handled by UI
            MenuSelectAnalysis_Click(null!, null!);

            StatusText.Text = "Plan Dose Extracted to Analysis!";
            StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22"));
        }

        private void FieldSizeMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_fieldSizeWindow != null)
                {
                    if (_fieldSizeWindow.WindowState == WindowState.Minimized)
                        _fieldSizeWindow.WindowState = WindowState.Normal;

                    _fieldSizeWindow.Activate();
                    return;
                }

                var dlg = new FieldSizeWindow(
                    _doseMap ?? _redChannel,
                    _dpiX,
                    _settings,
                    MainDisplayImage.Source as BitmapSource,
                    () => (
                        _doseMap ?? _redChannel,
                        _dpiX,
                        MainDisplayImage.Source as BitmapSource,
                        _activeFilmFileName != "None"
                            ? System.IO.Path.GetFileName(_activeFilmFileName)
                            : "Main window image"))
                {
                    Owner = this
                };
                _fieldSizeWindow = dlg;
                dlg.Closed += (_, _) =>
                {
                    _fieldSizeWindow = null;
                    SaveSettings();
                };
                dlg.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unable to open Field size dialog: {ex.Message}");
            }
        }

        private void StarShotMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null)
            {
                System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data");
                return;
            }

            if (_referenceCenterPixel == null)
            {
                System.Windows.MessageBox.Show("Please use the 'Pick Center' tool first to select the approximate center of the Star Shot.", "No Center Picked");
                return;
            }

            try
            {
                // Prefer Dose Map if available, else use Red channel (typical for star shot film)
                double[,] data = _doseMap ?? _redChannel!;
                
                var dlg = new StarShotWindow(data, _dpiX, _referenceCenterPixel.Value)
                {
                    Owner = this
                };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unable to open Star Shot dialog: {ex.Message}");
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
            string? fileName = ConfigComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(fileName)) return;
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
                        else if (trimmed.StartsWith("Degree:")) newConfig.Degree = int.Parse(trimmed.Substring("Degree:".Length).Trim(), CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        switch (currentSection)
                        {
                            case "RawData":
                                var parts = trimmed.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 4 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double dose))
                                {
                                    rawDataPoints.Add(new CalibrationPoint
                                    {
                                        Dose = dose,
                                        Red = double.Parse(parts[1], CultureInfo.InvariantCulture),
                                        Green = double.Parse(parts[2], CultureInfo.InvariantCulture),
                                        Blue = double.Parse(parts[3], CultureInfo.InvariantCulture)
                                    });
                                }
                                break;
                            case "FirstFit":
                                newConfig.FirstFit = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                                break;
                            case "SecondFit":
                                newConfig.SecondFit = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                                break;
                            case "ThirdFit":
                                newConfig.ThirdFit = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                                break;
                            case "DeltaOpt":
                                newConfig.DeltaOpt = double.Parse(trimmed, CultureInfo.InvariantCulture);
                                break;
                            case "RSquared":
                                newConfig.RSquared = double.Parse(trimmed, CultureInfo.InvariantCulture);
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
                int degree = DegreeFitDropDown.SelectedIndex + 1;

                // 1. Group by dose to pre-average points for the fit calculation
                var groupedPoints = CalibrationPoints
                    .GroupBy(p => p.Dose)
                    .Select(g => new
                    {
                        Dose = g.Key,
                        Red = g.Average(p => p.Red),
                        Green = g.Average(p => p.Green),
                        Blue = g.Average(p => p.Blue)
                    })
                    .OrderBy(p => p.Dose)
                    .ToList();

                if (groupedPoints.Count <= degree)
                {
                    System.Windows.MessageBox.Show($"Need at least {degree + 1} distinct dose levels to calculate a polynomial fit of degree {degree}.", "Insufficient Distinct Doses");
                    return;
                }

                // Arrays for fitting (Averaged)
                double[] fitDoses = groupedPoints.Select(p => p.Dose).ToArray();
                double[] fitRNorm = groupedPoints.Select(p => -Math.Log10(Math.Max(p.Red, 1) / 65535.0)).ToArray();
                double[] fitGNorm = groupedPoints.Select(p => -Math.Log10(Math.Max(p.Green, 1) / 65535.0)).ToArray();
                double[] fitBNorm = groupedPoints.Select(p => -Math.Log10(Math.Max(p.Blue, 1) / 65535.0)).ToArray();

                // Arrays for overall evaluation/plot (All Points)
                double[] allDoses = CalibrationPoints.Select(p => p.Dose).ToArray();
                double[] allRNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Red, 1) / 65535.0)).ToArray();
                double[] allGNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Green, 1) / 65535.0)).ToArray();
                double[] allBNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Blue, 1) / 65535.0)).ToArray();

                string channelMode = (ChannelFitDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Single: Red";
                
                CurrentConfig = new CalibrationConfig { Channel = channelMode, Degree = degree, CreatedAt = DateTime.Now };
                double[]? allDosesFit = null;

                if (channelMode.Contains("Single")) {
                    double[] fitXData = channelMode.Contains("Red") ? fitRNorm : (channelMode.Contains("Green") ? fitGNorm : fitBNorm);
                    double[] allXData = channelMode.Contains("Red") ? allRNorm : (channelMode.Contains("Green") ? allGNorm : allBNorm);
                    
                    CurrentConfig.FirstFit = FittingMath.PolyFit(fitXData, fitDoses, degree);
                    CurrentConfig.RSquared = FittingMath.CalculateRSquared(fitXData, fitDoses, CurrentConfig.FirstFit);
                    
                    // Evaluate on all points for the plot
                    if (CurrentConfig.FirstFit != null)
                    {
                        double[] coeffs = CurrentConfig.FirstFit;
                        allDosesFit = allXData.Select(x => FittingMath.PolyVal(coeffs, x)).ToArray();
                    }
                } else if (channelMode.Contains("Dual")) {
                    double[] fitRatio = channelMode.Contains("Red") 
                        ? fitRNorm.Zip(fitBNorm, (r, b) => r / (b + 1e-9)).ToArray() 
                        : fitGNorm.Zip(fitBNorm, (g, b) => g / (b + 1e-9)).ToArray();
                    double[] fitPrimary = channelMode.Contains("Red") ? fitRNorm : fitGNorm;
                    
                    double[] allRatio = channelMode.Contains("Red") 
                        ? allRNorm.Zip(allBNorm, (r, b) => r / (b + 1e-9)).ToArray() 
                        : allGNorm.Zip(allBNorm, (g, b) => g / (b + 1e-9)).ToArray();
                    
                    CurrentConfig.FirstFit = FittingMath.PolyFit(fitRatio, fitPrimary, degree);
                    
                    double[] fitVal1 = fitRatio.Select(r => FittingMath.PolyVal(CurrentConfig.FirstFit!, r)).ToArray();
                    CurrentConfig.SecondFit = FittingMath.PolyFit(fitVal1, fitDoses, degree);
                    CurrentConfig.RSquared = FittingMath.CalculateRSquared(fitVal1, fitDoses, CurrentConfig.SecondFit);
                    
                    // Evaluate on all points for the plot
                    double[] allVal1 = allRatio.Select(r => FittingMath.PolyVal(CurrentConfig.FirstFit!, r)).ToArray();
                    allDosesFit = allVal1.Select(v => FittingMath.PolyVal(CurrentConfig.SecondFit!, v)).ToArray();
                } else if (channelMode.Contains("Triple")) {
                    CurrentConfig.FirstFit = FittingMath.PolyFit(fitRNorm, fitDoses, degree);
                    CurrentConfig.SecondFit = FittingMath.PolyFit(fitGNorm, fitDoses, degree);
                    CurrentConfig.ThirdFit = FittingMath.PolyFit(fitBNorm, fitDoses, degree);
                    CurrentConfig.DeltaOpt = FittingMath.OptimizeTripleChannelDelta(fitRNorm, fitGNorm, fitBNorm, CurrentConfig.FirstFit, CurrentConfig.SecondFit, CurrentConfig.ThirdFit, fitDoses);
                    
                    // R-squared on FIT points for triple channel
                    double[] fitDosesCalc = new double[fitDoses.Length];
                    for (int i = 0; i < fitDoses.Length; i++) {
                        double rD = FittingMath.PolyVal(CurrentConfig.FirstFit!, fitRNorm[i] * CurrentConfig.DeltaOpt);
                        double gD = FittingMath.PolyVal(CurrentConfig.SecondFit!, fitGNorm[i] * CurrentConfig.DeltaOpt);
                        double bD = FittingMath.PolyVal(CurrentConfig.ThirdFit!, fitBNorm[i] * CurrentConfig.DeltaOpt);
                        fitDosesCalc[i] = (rD + gD + bD) / 3.0;
                    }
                    double ssTot = fitDoses.Sum(d => Math.Pow(d - fitDoses.Average(), 2));
                    double ssRes = fitDoses.Zip(fitDosesCalc, (actual, fit) => Math.Pow(actual - fit, 2)).Sum();
                    CurrentConfig.RSquared = ssTot > 0 ? 1 - (ssRes / ssTot) : 0;
                    
                    // Evaluate on all points for the plot
                    allDosesFit = new double[allDoses.Length];
                    for (int i = 0; i < allDoses.Length; i++) {
                        double rD = FittingMath.PolyVal(CurrentConfig.FirstFit!, allRNorm[i] * CurrentConfig.DeltaOpt);
                        double gD = FittingMath.PolyVal(CurrentConfig.SecondFit!, allGNorm[i] * CurrentConfig.DeltaOpt);
                        double bD = FittingMath.PolyVal(CurrentConfig.ThirdFit!, allBNorm[i] * CurrentConfig.DeltaOpt);
                        allDosesFit[i] = (rD + gD + bD) / 3.0;
                    }
                }

                if (CurrentConfig != null) {
                    RSquaredText.Text = CurrentConfig.RSquared.ToString("F4");
                    UpdatePlot(allDoses, allDosesFit, channelMode);
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

        private void PickCenter_Click(object sender, RoutedEventArgs e)
        {
            if (MainDisplayImage.Source == null || (_redChannel == null && _doseMap == null))
            {
                System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data");
                return;
            }

            ResetToolState();
            _isPickingCenter = true;
            ShowToolOverlay("Mode: Pick Reference Center");
            StatusText.Text = "Mode: Click on image to set center point";
            StatusIndicator.Background = new SolidColorBrush(Colors.DodgerBlue);
        }

        private void HandleCenterPick(Point canvasPoint)
        {
            _referenceCenterPixel = ControlToPixel(canvasPoint);

            // Update UI Marker
            UpdateAlignmentCross(AlignCrossIsoH, AlignCrossIsoV, canvasPoint.X, canvasPoint.Y, true);
            AlignCrossIsoH.Visibility = Visibility.Visible;
            AlignCrossIsoV.Visibility = Visibility.Visible;

            _isPickingCenter = false;
            SelectionCrosshairH.Visibility = Visibility.Collapsed;
            SelectionCrosshairV.Visibility = Visibility.Collapsed;

            HideToolOverlay();
            StatusText.Text = $"Center Picked: {_referenceCenterPixel.Value.X:F1}, {_referenceCenterPixel.Value.Y:F1}";
            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
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
            ShowToolOverlay("Step: Click LEFT marker (Red)");
            StatusText.Text = "Alignment: Pick Left Marker";
            StatusIndicator.Background = new SolidColorBrush(Colors.DodgerBlue);
            SelectionRect.Visibility = Visibility.Collapsed;
        }

        private void ExitAlignMode_Click(object sender, RoutedEventArgs e)
        {
            _isAligning = false;
            _alignStep = 0;
            HideToolOverlay();
            AlignCross1H.Visibility = Visibility.Collapsed;
            AlignCross1V.Visibility = Visibility.Collapsed;
            AlignCross2H.Visibility = Visibility.Collapsed;
            AlignCross2V.Visibility = Visibility.Collapsed;
            AlignCross3H.Visibility = Visibility.Collapsed;
            AlignCross3V.Visibility = Visibility.Collapsed;
            AlignCrossIsoH.Visibility = Visibility.Collapsed;
            AlignCrossIsoV.Visibility = Visibility.Collapsed;
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
                    UpdateAlignmentCross(AlignCross1H, AlignCross1V, canvasPoint.X, canvasPoint.Y);
                    AlignCross1H.Visibility = Visibility.Visible;
                    AlignCross1V.Visibility = Visibility.Visible;
                    _alignStep = 2;
                    ShowToolOverlay("Step: Click RIGHT marker (Blue)");
                    StatusText.Text = "Alignment: Pick Right Marker";
                    break;
                case 2:
                    _alignRight = canvasPoint;
                    UpdateAlignmentCross(AlignCross2H, AlignCross2V, canvasPoint.X, canvasPoint.Y);
                    AlignCross2H.Visibility = Visibility.Visible;
                    AlignCross2V.Visibility = Visibility.Visible;
                    _alignStep = 3;
                    ShowToolOverlay("Step: Click TOP marker (Green)");
                    StatusText.Text = "Alignment: Pick Top Marker";
                    break;
                case 3:
                    _alignTop = canvasPoint;
                    UpdateAlignmentCross(AlignCross3H, AlignCross3V, canvasPoint.X, canvasPoint.Y);
                    AlignCross3H.Visibility = Visibility.Visible;
                    AlignCross3V.Visibility = Visibility.Visible;

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
            double dxLR = R.X - L.X;
            double dyLR = R.Y - L.Y;
            double angle = Math.Atan2(dyLR, dxLR) * (180.0 / Math.PI);

            // Isocenter: projection of T onto the line L-R
            double lenSq = dxLR * dxLR + dyLR * dyLR;
            double isoX, isoY;
            if (lenSq > 1e-9)
            {
                double dot = ((T.X - L.X) * dxLR + (T.Y - L.Y) * dyLR) / lenSq;
                isoX = L.X + dot * dxLR;
                isoY = L.Y + dot * dyLR;
            }
            else
            {
                isoX = T.X;
                isoY = (L.Y + R.Y) / 2.0;
            }

            // Show isocenter marker briefly
            Point isoPanelPt = PixelToCanvas(new Point(isoX, isoY), renderedRect);
            UpdateAlignmentCross(AlignCrossIsoH, AlignCrossIsoV, isoPanelPt.X, isoPanelPt.Y, true);
            AlignCrossIsoH.Visibility = Visibility.Visible;
            AlignCrossIsoV.Visibility = Visibility.Visible;
            ShowToolOverlay("Isocenter found. Transforming...");
            StatusText.Text = $"Iso: ({isoX:F1}, {isoY:F1}) px, Roll: {angle:F2}°";
            await Task.Delay(600);

            int rows = _imgHeight;
            int cols = _imgWidth;
            double gamma = -angle * Math.PI / 180.0;
            double cosG = Math.Cos(gamma), sinG = Math.Sin(gamma);

            // Transform corners to find new canvas bounds (MATLAB 1-based coordinates)
            double[,] corners = { { 1, 1 }, { cols, 1 }, { 1, rows }, { cols, rows } };
            double maxH = 0, maxV = 0;
            for (int i = 0; i < 4; i++)
            {
                double cx = corners[i, 0] - isoX;
                double cy = corners[i, 1] - isoY;
                double tx = cx * cosG - cy * sinG;
                double ty = cx * sinG + cy * cosG;
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
            double[,]? sourceDoseMap = _doseMap;
            double[,]? newDoseMap = sourceDoseMap != null ? new double[newH, newW] : null;

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
                        double ux = dx * cosG + dy * sinG;
                        double uy = -dx * sinG + dy * cosG;
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
                            if (sourceDoseMap != null && newDoseMap != null)
                                newDoseMap[outRow, outCol] = w00 * sourceDoseMap[r0, c0] + w10 * sourceDoseMap[r0, c1] + w01 * sourceDoseMap[r1, c0] + w11 * sourceDoseMap[r1, c1];
                        }
                    }
                });
            });

            // Replace channels with transformed data
            _redChannel = newRed;
            _greenChannel = newGreen;
            _blueChannel = newBlue;
            if (newDoseMap != null)
            {
                _doseMap = newDoseMap;
                _filmDoseMap = newDoseMap;
            }
            _imgWidth = newW;
            _imgHeight = newH;
            
            // The three-point alignment always constructs the output image such that the isocenter 
            // is at the exact geometric center. We update the reference center to match.
            _referenceCenterPixel = new Point(newW / 2.0, newH / 2.0);
            
            UpdateCropUI();
            
            // Clean up alignment UI
            ExitAlignMode_Click(null!, null!);

            // Refresh display
            UpdateDisplayFromRaw();
            StatusText.Text = $"Aligned: {angle:F2}° rotation, center at ({isoX:F0}, {isoY:F0})";
            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
        }

        private void UpdateAlignmentCross(Line h, Line v, double x, double y, bool isIso = false)
        {
            if (h == null || v == null) return;
            double canvasW = SelectionCanvas.ActualWidth;
            double canvasH = SelectionCanvas.ActualHeight;

            if (isIso)
            {
                // Iso crosshair is a bit larger than regular points
                h.X1 = 0; h.X2 = canvasW;
                h.Y1 = y; h.Y2 = y;
                v.X1 = x; v.X2 = x;
                v.Y1 = 0; v.Y2 = canvasH;
            }
            else
            {
                // Regular alignment crosshairs
                h.X1 = 0; h.X2 = canvasW;
                h.Y1 = y; h.Y2 = y;
                v.X1 = x; v.X2 = x;
                v.Y1 = 0; v.Y2 = canvasH;
            }
        }
        private void AutoAlign_Click(object sender, RoutedEventArgs e) { }

        #region Orientation (Rotation & Flip)

        /// <summary>Refreshes the display based on current mode (dose map or raw image).</summary>
        private void RefreshDisplay()
        {
            if (_isShowingDoseMap && _doseMap != null)
            {
                _filmDoseMap = _doseMap; // Sync the background analysis dose map to include any applied filters/transforms
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

        private void Rotation_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }

            bool isCW = (sender as FrameworkElement)?.Name == "CWButton";
            PushUndo(isCW ? "Rotate CW" : "Rotate CCW");

            int oldH = _imgHeight, oldW = _imgWidth;
            if (_redChannel != null)
            {
                _redChannel = ImageTransforms.Rotate2D(_redChannel, oldH, oldW, isCW);
                _greenChannel = ImageTransforms.Rotate2D(_greenChannel, oldH, oldW, isCW);
                _blueChannel = ImageTransforms.Rotate2D(_blueChannel, oldH, oldW, isCW);
            }
            if (_doseMap != null) _doseMap = ImageTransforms.Rotate2D(_doseMap, oldH, oldW, isCW);

            _imgWidth = oldH; _imgHeight = oldW;
            
            if (_referenceCenterPixel.HasValue)
            {
                double px = _referenceCenterPixel.Value.X;
                double py = _referenceCenterPixel.Value.Y;
                _referenceCenterPixel = isCW ? new Point(_imgWidth - py, px) : new Point(py, _imgHeight - px);
            }

            UpdateCropUI();
            RefreshDisplay();
            StatusText.Text = isCW ? "Rotated CW 90°" : "Rotated CCW 90°";
        }

        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }

            bool isHorizontal = (sender as FrameworkElement)?.Name == "FlipHButton";
            PushUndo(isHorizontal ? "Flip H" : "Flip V");

            int h = _imgHeight, w = _imgWidth;
            Action<double[,], int, int> flipFn = isHorizontal ? ImageTransforms.FlipH : ImageTransforms.FlipV;
            if (_redChannel != null)
            {
                flipFn(_redChannel, h, w);
                flipFn(_greenChannel, h, w);
                flipFn(_blueChannel, h, w);
            }
            if (_doseMap != null) flipFn(_doseMap, h, w);

            if (_referenceCenterPixel.HasValue)
            {
                double px = _referenceCenterPixel.Value.X;
                double py = _referenceCenterPixel.Value.Y;
                _referenceCenterPixel = isHorizontal ? new Point(_imgWidth - px, py) : new Point(px, _imgHeight - py);
            }

            RefreshDisplay();
            StatusText.Text = isHorizontal ? "Flipped Horizontal" : "Flipped Vertical";
        }

        #endregion

        #region Cropping

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            if (_redChannel == null && _doseMap == null) { System.Windows.MessageBox.Show("Please load an image or dose map first.", "No Data"); return; }
            ResetToolState();
            string name = (sender as FrameworkElement)?.Name ?? "";

            if (name == "ManualCropButton")
            {
                if (_isSelectingROI || _isAligning) return;
                _isCropping = true;
                _isSelectingROI = true;
                _isFixedMode = false;
                HideToolOverlay();
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

        private void ApplyCrop(int x, int y, int w, int h)
        {
            x = Math.Max(0, x); y = Math.Max(0, y);
            w = Math.Min(w, _imgWidth - x); h = Math.Min(h, _imgHeight - y);
            if (w <= 0 || h <= 0) return;

            if (_redChannel != null) _redChannel = ImageTransforms.CropArray(_redChannel, x, y, w, h);
            if (_greenChannel != null) _greenChannel = ImageTransforms.CropArray(_greenChannel, x, y, w, h);
            if (_blueChannel != null) _blueChannel = ImageTransforms.CropArray(_blueChannel, x, y, w, h);
            if (_doseMap != null) _doseMap = ImageTransforms.CropArray(_doseMap, x, y, w, h);

            if (_referenceCenterPixel.HasValue)
            {
                _referenceCenterPixel = new Point(_referenceCenterPixel.Value.X - x, _referenceCenterPixel.Value.Y - y);
            }

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
                        _doseMap = ImageFilters.MedianFilter2D(_doseMap, kernelSize);
                    }
                    else
                    {
                        _redChannel = ImageFilters.MedianFilter2D(_redChannel, kernelSize);
                        _greenChannel = ImageFilters.MedianFilter2D(_greenChannel, kernelSize);
                        _blueChannel = ImageFilters.MedianFilter2D(_blueChannel, kernelSize);
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
                        ImageFilters.ApplyNoiseFilter(_doseMap, threshold);
                    }
                    else
                    {
                        ImageFilters.ApplyNoiseFilter(_redChannel, threshold);
                        ImageFilters.ApplyNoiseFilter(_greenChannel, threshold);
                        ImageFilters.ApplyNoiseFilter(_blueChannel, threshold);
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
                        if (method == "Average") _doseMap = ImageFilters.BoxFilter2D(_doseMap, window);
                        else if (method == "Median") _doseMap = ImageFilters.MedianFilter2D(_doseMap, window);
                        else if (method == "Gaussian") _doseMap = ImageFilters.GaussianFilter2D(_doseMap, strength);
                    }
                    else
                    {
                        if (method == "Average")
                        {
                            _redChannel = ImageFilters.BoxFilter2D(_redChannel, window);
                            _greenChannel = ImageFilters.BoxFilter2D(_greenChannel, window);
                            _blueChannel = ImageFilters.BoxFilter2D(_blueChannel, window);
                        }
                        else if (method == "Median")
                        {
                            _redChannel = ImageFilters.MedianFilter2D(_redChannel, window);
                            _greenChannel = ImageFilters.MedianFilter2D(_greenChannel, window);
                            _blueChannel = ImageFilters.MedianFilter2D(_blueChannel, window);
                        }
                        else if (method == "Gaussian")
                        {
                            _redChannel = ImageFilters.GaussianFilter2D(_redChannel, strength);
                            _greenChannel = ImageFilters.GaussianFilter2D(_greenChannel, strength);
                            _blueChannel = ImageFilters.GaussianFilter2D(_blueChannel, strength);
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
                        _doseMap = ImageFilters.Interpolate2D(_doseMap, newW, newH, method);
                    }
                    else
                    {
                        _redChannel = ImageFilters.Interpolate2D(_redChannel, newW, newH, method);
                        _greenChannel = ImageFilters.Interpolate2D(_greenChannel, newW, newH, method);
                        _blueChannel = ImageFilters.Interpolate2D(_blueChannel, newW, newH, method);
                    }
                });

                double actualScaleX = newW / (double)_imgWidth;
                double actualScaleY = newH / (double)_imgHeight;

                _imgWidth = newW; _imgHeight = newH;
                _dpiX *= actualScaleX; 
                _dpiY *= actualScaleY; // Maintain physical dimensions by using actual achieved scale

                if (doseMode)
                {
                    _filmDpiX = _dpiX;
                    _filmDpiY = _dpiY;
                }

                UpdateCropUI();
                MetaDPI.Text = _dpiX == _dpiY ? _dpiX.ToString("F1") : $"{_dpiX:F1}x{_dpiY:F1}"; // Update UI Metadata

                RefreshDisplay();
                StatusText.Text = $"Interpolated to {newW}x{newH} ({method})";
                StatusIndicator.Background = new SolidColorBrush(Colors.Green);
            }
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

            ResetToolState();

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
                ShowToolOverlay("ROI Extraction Active");

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
            ResetToolState();
            
            StatusText.Text = "ROI Tool Deactivated";
            StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF4500"));
        }

        private void ResetToolState()
        {
            _isSelectingROI = false;
            _isDrawing = false;
            _isFixedMode = false;
            _isCropping = false;
            _isROIFiltering = false;
            _activeMeasurementMode = MeasurementMode.None;
            _isAreaRectMode = false;
            _isAligning = false;
            _isMeasurementMode = false;
            _isPickingCenter = false;

            SelectionRect.Visibility = Visibility.Collapsed;
            SelectionCrosshairH.Visibility = Visibility.Collapsed;
            SelectionCrosshairV.Visibility = Visibility.Collapsed;
            MeasurementLine.Visibility = Visibility.Collapsed;
            MeasurementPolyline.Visibility = Visibility.Collapsed;
            MeasurementLabel.Visibility = Visibility.Collapsed;
            
            HideToolOverlay();
        }

        private void ShowToolOverlay(string text)
        {
            if (ToolModeOverlay == null || ToolModeText == null) return;
            ToolModeText.Text = text;
            ToolModeOverlay.Visibility = Visibility.Visible;
        }

        private void HideToolOverlay()
        {
            if (ToolModeOverlay != null) ToolModeOverlay.Visibility = Visibility.Collapsed;
        }

        private void ExitActiveTool_Click(object sender, RoutedEventArgs e)
        {
            if (_isAligning) ExitAlignMode_Click(sender, e);
            else if (_isSelectingROI) ExitROIMode_Click(sender, e);
            else HideToolOverlay();
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

            if (_isPickingCenter)
            {
                HandleCenterPick(e.GetPosition(SelectionCanvas));
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

            if (_activeMeasurementMode == MeasurementMode.Crosshairs)
            {
                Point pos = e.GetPosition(SelectionCanvas);
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

            if (_isPickingCenter)
            {
                Point pos = e.GetPosition(SelectionCanvas);
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

            if (_isAligning)
            {
                Point pos = e.GetPosition(SelectionCanvas);
                switch (_alignStep)
                {
                    case 1:
                        UpdateAlignmentCross(AlignCross1H, AlignCross1V, pos.X, pos.Y);
                        AlignCross1H.Visibility = Visibility.Visible;
                        AlignCross1V.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        UpdateAlignmentCross(AlignCross2H, AlignCross2V, pos.X, pos.Y);
                        AlignCross2H.Visibility = Visibility.Visible;
                        AlignCross2V.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        UpdateAlignmentCross(AlignCross3H, AlignCross3V, pos.X, pos.Y);
                        AlignCross3H.Visibility = Visibility.Visible;
                        AlignCross3V.Visibility = Visibility.Visible;
                        break;
                }
                return;
            }

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
                    double wmm = Math.Abs(startPixel.X - endPixel.X) * 25.4 / _dpiX;
                    double hmm = Math.Abs(startPixel.Y - endPixel.Y) * 25.4 / _dpiY;
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
            for (int x = 0; x < width; x++) xDistances[x] = (x - centerX) * 25.4 / _dpiX;

            double[] yDistances = new double[height];
            for (int y = 0; y < height; y++) yDistances[y] = (y - centerY) * 25.4 / _dpiY;

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
            if (MainDisplayImage.Source == null)
            {
                StatusText.Text = "Please load an image or dose map first.";
                StatusIndicator.Background = new SolidColorBrush(Colors.Orange);
                return;
            }

            ResetToolState();

            // Identify the mode using direct object comparison (more robust than string Name)
            if (sender == DistanceButton)
            {
                _activeMeasurementMode = MeasurementMode.Distance;
            }
            else if (sender == AreaButton)
            {
                _activeMeasurementMode = MeasurementMode.Area;
                
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
            else
            {
                _activeMeasurementMode = MeasurementMode.ROIDose;
            }

            _isMeasurementMode = true;
            _isSelectingROI = true;
            _isCropping = false;
            _isROIFiltering = false;

            string modeName = _activeMeasurementMode == MeasurementMode.Distance ? "Distance/Line" :
                            _activeMeasurementMode == MeasurementMode.Area ? $"Area ({(_isAreaRectMode ? "Rectangle" : "Freehand")})" : "ROI Dose";

            ShowToolOverlay($"Tool: {modeName}");

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

        private void CrosshairsButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainDisplayImage.Source == null) return;

            ResetToolState();

            _activeMeasurementMode = MeasurementMode.Crosshairs;
            _isMeasurementMode = true;
            _isSelectingROI = true; // Keeps the SelectionCanvas active

            ShowToolOverlay("Tool: Crosshairs");
            StatusText.Text = "Measurement Mode: Crosshairs";
            StatusIndicator.Background = new SolidColorBrush(Colors.MediumPurple);

            SelectionRect.Visibility = Visibility.Collapsed;
            MeasurementLine.Visibility = Visibility.Collapsed;
            MeasurementPolyline.Visibility = Visibility.Collapsed;
            MeasurementLabel.Visibility = Visibility.Collapsed;
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
                        
                        if (_activeMeasurementMode != MeasurementMode.ROIDose)
                        {
                            _isMeasurementMode = false;
                            _isSelectingROI = false;
                            HideToolOverlay();
                            StatusText.Text = "Ready";
                            StatusIndicator.Background = new SolidColorBrush(Colors.Green);
                        }
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

            double mm_per_pixel_X = 25.4 / _dpiX;
            double mm_per_pixel_Y = 25.4 / _dpiY;
            double display_pixels_per_mm_X = bounds.Width / (_imgWidth * mm_per_pixel_X);
            double display_pixels_per_mm_Y = bounds.Height / (_imgHeight * mm_per_pixel_Y);

            // Coordinates relative to the Panel Center
            double mmX = (mPos.X - centerX) / display_pixels_per_mm_X;
            double mmY = (mPos.Y - centerY) / display_pixels_per_mm_Y;

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
                System.Windows.Rect bounds = GetRenderedImageBounds(MainDisplayImage);
                if (bounds.Width <= 0 || bounds.Height <= 0) return;

                double mm_per_pixel_X = 25.4 / _dpiX;
                double mm_per_pixel_Y = 25.4 / _dpiY;
                double display_pixels_per_mm_X = bounds.Width / (_imgWidth * mm_per_pixel_X);
                double display_pixels_per_mm_Y = bounds.Height / (_imgHeight * mm_per_pixel_Y);

                var majorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
                var minorBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160));
                var originBrush = System.Windows.Media.Brushes.DarkOrange;
                var majorPen = new System.Windows.Media.Pen(majorBrush, 1.0);
                var minorPen = new System.Windows.Media.Pen(minorBrush, 0.6);
                var originPen = new System.Windows.Media.Pen(originBrush, 2.0);
                majorPen.Freeze(); minorPen.Freeze(); originPen.Freeze(); majorBrush.Freeze(); minorBrush.Freeze();

                double canvasW = TopRulerCanvas.ActualWidth;
                double canvasH = LeftRulerCanvas.ActualHeight;
                if (canvasW <= 0) canvasW = 2000;
                if (canvasH <= 0) canvasH = 2000;

                double centerX = canvasW / 2.0;
                double centerY = canvasH / 2.0;

                double majorStep = GetNiceStep(display_pixels_per_mm_X, 50);
                double minorStep = GetNiceStep(display_pixels_per_mm_X, 8);
                if (minorStep >= majorStep) minorStep = majorStep / 5.0;

                DrawRuler(TopRulerCanvas, canvasW, 30, dc =>
                {
                    dc.DrawLine(new System.Windows.Media.Pen(majorBrush, 1), new System.Windows.Point(0, 29), new System.Windows.Point(canvasW, 29));
                    for (double mm = -1000; mm <= 1000; mm += minorStep)
                    {
                        double x = centerX + (mm * display_pixels_per_mm_X);
                        if (x < -10 || x > canvasW + 10) continue;

                        bool isZero = (System.Math.Abs(mm) < 0.001);
                        bool isMajor = (System.Math.Abs(mm % majorStep) < 0.001) || (System.Math.Abs(mm % majorStep - majorStep) < 0.001);
                        bool isMid = (majorStep > 5) && (System.Math.Abs(mm % (majorStep / 2)) < 0.001);
                        
                        double y1 = isMajor ? 0 : (isMid ? 15 : 22);

                        System.Windows.Media.Pen p = isZero ? originPen : (isMajor ? majorPen : minorPen);
                        dc.DrawLine(p, new System.Windows.Point(x, y1), new System.Windows.Point(x, 30));
                        
                        if (isMajor)
                        {
                            var ft = new System.Windows.Media.FormattedText(mm.ToString("0"), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new System.Windows.Media.Typeface("Segoe UI"), 10, isZero ? originBrush : majorBrush, 1.25);
                            dc.DrawText(ft, new System.Windows.Point(x + 2, 2));
                        }
                    }
                });

                DrawRuler(BottomRulerCanvas, canvasW, 30, dc =>
                {
                    dc.DrawLine(new System.Windows.Media.Pen(majorBrush, 1), new System.Windows.Point(0, 0.5), new System.Windows.Point(canvasW, 0.5));
                    for (double mm = -1000; mm <= 1000; mm += minorStep)
                    {
                        double x = centerX + (mm * display_pixels_per_mm_X);
                        if (x < -10 || x > canvasW + 10) continue;

                        bool isZero = (System.Math.Abs(mm) < 0.001);
                        bool isMajor = (System.Math.Abs(mm % majorStep) < 0.001) || (System.Math.Abs(mm % majorStep - majorStep) < 0.001);
                        bool isMid = (majorStep > 5) && (System.Math.Abs(mm % (majorStep / 2)) < 0.001);

                        double y2 = isMajor ? 30 : (isMid ? 15 : 8);

                        System.Windows.Media.Pen p = isZero ? originPen : (isMajor ? majorPen : minorPen);
                        dc.DrawLine(p, new System.Windows.Point(x, 0), new System.Windows.Point(x, y2));

                        if (isMajor)
                        {
                            var ft = new System.Windows.Media.FormattedText(mm.ToString("0"), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new System.Windows.Media.Typeface("Segoe UI"), 10, isZero ? originBrush : majorBrush, 1.25);
                            dc.DrawText(ft, new System.Windows.Point(x + 2, 16));
                        }
                    }
                });

                DrawRuler(LeftRulerCanvas, 40, canvasH, dc =>
                {
                    dc.DrawLine(new System.Windows.Media.Pen(majorBrush, 1), new System.Windows.Point(39, 0), new System.Windows.Point(39, canvasH));
                    for (double mm = -1000; mm <= 1000; mm += minorStep)
                    {
                        double y = centerY + (mm * display_pixels_per_mm_Y);
                        if (y < -10 || y > canvasH + 10) continue;

                        bool isZero = (System.Math.Abs(mm) < 0.001);
                        bool isMajor = (System.Math.Abs(mm % majorStep) < 0.001) || (System.Math.Abs(mm % majorStep - majorStep) < 0.001);
                        bool isMid = (majorStep > 5) && (System.Math.Abs(mm % (majorStep / 2)) < 0.001);

                        double x1 = isMajor ? 0 : (isMid ? 20 : 30);

                        System.Windows.Media.Pen p = isZero ? originPen : (isMajor ? majorPen : minorPen);
                        dc.DrawLine(p, new System.Windows.Point(x1, y), new System.Windows.Point(40, y));

                        if (isMajor)
                        {
                            var ft = new System.Windows.Media.FormattedText(mm.ToString("0"), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new System.Windows.Media.Typeface("Segoe UI"), 10, isZero ? originBrush : majorBrush, 1.25);
                            dc.PushTransform(new System.Windows.Media.RotateTransform(-90, 10, y));
                            dc.DrawText(ft, new System.Windows.Point(2, y - 2));
                            dc.Pop();
                        }
                    }
                });

                DrawRuler(RightRulerCanvas, 40, canvasH, dc =>
                {
                    dc.DrawLine(new System.Windows.Media.Pen(majorBrush, 1), new System.Windows.Point(0.5, 0), new System.Windows.Point(0.5, canvasH));
                    for (double mm = -1000; mm <= 1000; mm += minorStep)
                    {
                        double y = centerY + (mm * display_pixels_per_mm_Y);
                        if (y < -10 || y > canvasH + 10) continue;

                        bool isZero = (System.Math.Abs(mm) < 0.001);
                        bool isMajor = (System.Math.Abs(mm % majorStep) < 0.001) || (System.Math.Abs(mm % majorStep - majorStep) < 0.001);
                        bool isMid = (majorStep > 5) && (System.Math.Abs(mm % (majorStep / 2)) < 0.001);

                        double x2 = isMajor ? 40 : (isMid ? 20 : 10);

                        System.Windows.Media.Pen p = isZero ? originPen : (isMajor ? majorPen : minorPen);
                        dc.DrawLine(p, new System.Windows.Point(0, y), new System.Windows.Point(x2, y));

                        if (isMajor)
                        {
                            var ft = new System.Windows.Media.FormattedText(mm.ToString("0"), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new System.Windows.Media.Typeface("Segoe UI"), 10, isZero ? originBrush : majorBrush, 1.25);
                            dc.PushTransform(new System.Windows.Media.RotateTransform(-90, 30, y));
                            dc.DrawText(ft, new System.Windows.Point(26, y - 2));
                            dc.Pop();
                        }
                    }
                });
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private double GetNiceStep(double pixelsPerMm, double minPixels)
        {
            double targetMm = minPixels / pixelsPerMm;
            double[] niceSteps = { 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
            foreach (var step in niceSteps)
            {
                if (step >= targetMm) return step;
            }
            return 2000;
        }

        private void DrawRuler(System.Windows.Controls.Canvas canvas, double width, double height, Action<System.Windows.Media.DrawingContext> drawAction)
        {
            canvas.Children.Clear();
            int w = (int)Math.Max(1, Math.Ceiling(width));
            int h = (int)Math.Max(1, Math.Ceiling(height));
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                drawAction(dc);
            }
            var bmp = new RenderTargetBitmap(w, h, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bmp.Render(dv);
            bmp.Freeze();
            var img = new System.Windows.Controls.Image { Source = bmp, Width = width, Height = height };
            canvas.Children.Add(img);
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
                var localDoseMap = new double[h, w];
                _rawImageSource = MainDisplayImage.Source;

                var config = CurrentConfig;
                double delta = config.DeltaOpt;
                string mode = config.Channel;

                // Capture channels locally for thread safety
                var red = _redChannel;
                var green = _greenChannel;
                var blue = _blueChannel;

                await Task.Run(() =>
                {
                    Parallel.For(0, h, y =>
                    {
                        for (int x = 0; x < w; x++)
                        {
                            localDoseMap[y, x] = DoseCalculator.CalculateSinglePixelDose(red, green, blue, x, y, mode, config, delta);
                        }
                    });
                });

                _doseMap = localDoseMap;
                var heatmap = GenerateDoseHeatmap();
                MainDisplayImage.Source = heatmap;
                
                _isShowingDoseMap = true;
                ShowDoseToggle.IsChecked = true;
                ShowDoseToggle.IsEnabled = true;

                // Sync to analysis-ready film backup
                _filmDoseMap = _doseMap;
                _filmDpiX = _dpiX;
                _filmDpiY = _dpiY;

                double maxDose = 0.001;
                foreach (var d in _doseMap) if (d > maxDose) maxDose = d;
                DoseRangeText.Text = $"0.0 - {maxDose:F2} cGy";

                UpdateRulers(); // Update rulers after dose conversion
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

        private async void ExportDoseMap_Click(object sender, RoutedEventArgs e)
        {
            if (_doseMap == null)
            {
                System.Windows.MessageBox.Show("No dose map available to export. Please convert to dose first.");
                return;
            }
        
            string defaultName = _activeFilmFileName != "None" 
                ? "Film_" + System.IO.Path.GetFileNameWithoutExtension(_activeFilmFileName) + "_DoseMap.txt" 
                : "Film_DoseMap_Export.txt";
            var dlg = new SaveFileDialog { Filter = "Text Files|*.txt|All Files|*.*", FileName = defaultName };
            if (dlg.ShowDialog() == true)
            {
                StatusText.Text = "Exporting Dose Map...";
                StatusIndicator.Background = new SolidColorBrush(Colors.Orange);
                GlobalProgressBar.Value = 0;
                GlobalProgressBar.Visibility = Visibility.Visible;
                
                string fileName = dlg.FileName;
                double dpiX = _dpiX;
                double dpiY = _dpiY;
                int imgW = _imgWidth;
                int imgH = _imgHeight;
                double[,] dose = _doseMap;
        
                try
                {
                    await Task.Run(() =>
                    {
                        using (var writer = new System.IO.StreamWriter(fileName))
                        {
                            writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd}");
                            writer.WriteLine($"DPI_X: {dpiX:F1}");
                            writer.WriteLine($"DPI_Y: {dpiY:F1}");
                            writer.WriteLine($"Interpolation: 1");
                            writer.WriteLine($"X Res: {imgW}");
                            writer.WriteLine($"Y Res: {imgH}");
                            writer.WriteLine();
                            writer.WriteLine();
                            writer.WriteLine("Array Start:");
        
                            for (int y = 0; y < imgH; y++)
                            {
                                var sb = new StringBuilder();
                                for (int x = 0; x < imgW; x++)
                                {
                                    sb.Append(dose[y, x].ToString("F4", CultureInfo.InvariantCulture));
                                    if (x < imgW - 1) sb.Append("\t");
                                }
                                writer.WriteLine(sb.ToString());
                                
                                if (y % 20 == 0)
                                {
                                    double prog = (double)y / imgH * 100.0;
                                    Dispatcher.Invoke(() => GlobalProgressBar.Value = prog);
                                }
                            }
        
                            writer.WriteLine();
                            writer.WriteLine(":Array End");
                        }
                    });
                    
                    StatusText.Text = "Dose Map Exported";
                    StatusIndicator.Background = new SolidColorBrush(Colors.Green);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error exporting dose map: {ex.Message}");
                }
                finally
                {
                    GlobalProgressBar.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void ImportDoseMap_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    // 1. Choice Dialog: Film vs DICOM
                    var choicePanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
                    choicePanel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Which role should this Dose Map take?", Margin = new Thickness(0,0,0,10) });
                    
                    var choiceDialog = new ContentDialog { 
                        Title = "Dose Map Role", 
                        Content = choicePanel,
                        PrimaryButtonText = "Film (Measured)",
                        SecondaryButtonText = "DICOM (Planned)",
                        CloseButtonText = "Cancel"
                    };

                    var result = await _dialogService.ShowAsync(choiceDialog, CancellationToken.None);
                    if (result == ContentDialogResult.None) return;

                    bool isFilm = result == ContentDialogResult.Primary;

                    using (var reader = new System.IO.StreamReader(dlg.FileName))
                    {
                        // Parse header
                        string? line;
                        double dpi = 72;
                        int width = 0, height = 0;
                        double ox = 0, oy = 0, rx = 0, ry = 0, rz = 0;
                        double sySign = 1.0;
                        string orientation = "Z";
                        int fractions = 1;

                        // Read header lines until Array Start
                        bool foundStart = false;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("Array Start:")) 
                            { 
                                foundStart = true;
                                break; 
                            }
                            
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split(':');
                            if (parts.Length < 2) continue;
                            var key = parts[0].Trim();
                            var val = parts[1].Trim();
                            
                            if (key.Contains("DPI")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out dpi);
                            else if (key.Contains("X Res")) int.TryParse(val, out width);
                            else if (key.Contains("Y Res")) int.TryParse(val, out height);
                            else if (key.Contains("Origin_X")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out ox);
                            else if (key.Contains("Origin_Y")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out oy);
                            else if (key.Contains("Ref_X")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out rx);
                            else if (key.Contains("Ref_Y")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out ry);
                            else if (key.Contains("Ref_Z")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out rz);
                            else if (key.Contains("Plane_Orientation")) orientation = val;
                            else if (key.Contains("Spacing_Y_Sign")) double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out sySign);
                            else if (key.Contains("Fractions")) int.TryParse(val, out fractions);
                        }

                        if (!foundStart || width <= 0 || height <= 0)
                        {
                            System.Windows.MessageBox.Show("Invalid header format or could not find Array Start.");
                            return;
                        }

                        StatusText.Text = "Importing Dose Map...";
                        StatusIndicator.Background = new SolidColorBrush(Colors.Orange);
                        GlobalProgressBar.Value = 0;
                        GlobalProgressBar.Visibility = Visibility.Visible;

                        double[,] importedDose = new double[height, width];

                        await Task.Run(() =>
                        {
                            for (int y = 0; y < height; y++)
                            {
                                line = reader.ReadLine();
                                if (line == null || line.Contains(":Array End")) break;
                                var values = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                for (int x = 0; x < width && x < values.Length; x++)
                                {
                                    double.TryParse(values[x], NumberStyles.Any, CultureInfo.InvariantCulture, out importedDose[y, x]);
                                }

                                if (y % 20 == 0)
                                {
                                    double prog = (double)y / height * 100.0;
                                    Dispatcher.Invoke(() => GlobalProgressBar.Value = prog);
                                }
                            }
                        });

                        if (isFilm)
                        {
                            _doseMap = importedDose;
                            _filmDoseMap = importedDose;
                            _imgWidth = width;
                            _imgHeight = height;
                            _dpiX = _dpiY = dpi;
                            _filmDpiX = _filmDpiY = dpi;
                            _isShowingDoseMap = true;
                            _activeFilmFileName = dlg.FileName;

                            UpdateImageMetadata(dlg.FileName, width, height, dpi);
                            MainDisplayImage.Source = GenerateDoseHeatmap();
                            ShowDoseToggle.IsChecked = true;
                            ShowDoseToggle.IsEnabled = true;
                            UpdateRulers();
                            StatusText.Text = "Film Dose Map Imported";
                            SyncAllDataToAnalysis();
                        }
                        else
                        {
                            _importedPlanDose = importedDose;
                            _importedPlanDpiX = _importedPlanDpiY = dpi;
                            _importedPlanOriginX = ox;
                            _importedPlanOriginY = oy;
                            _importedPlanRefX = rx;
                            _importedPlanRefY = ry;
                            _importedPlanRefZ = rz;
                            _importedPlanSpacingYSign = sySign;
                            _importedPlanOrientation = orientation;
                            _importedPlanFractions = fractions;
                            _activeDicomFileName = dlg.FileName;
                            StatusText.Text = "DICOM Dose Map Imported (Ready for Analysis)";
                            SyncAllDataToAnalysis();
                        }

                        StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22"));
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error importing dose map: {ex.Message}");
                }
                finally
                {
                    GlobalProgressBar.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void OpenAlignmentWindow() => ManuallyAlign_Click(null!, null!);
        private void OpenFieldSizeWindow() => FieldSizeMenu_Click(null!, null!);
        private void OpenGammaWindow() => System.Windows.MessageBox.Show("Gamma analysis feature coming soon.", "Feature Not Available");

        #endregion

        #endregion

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
                    var (R, G, B) = ColorMaps.GetColorFromMap(d / maxDose, mapName);
                    int idx = y * stride + x * 4;
                    pixels[idx] = B;
                    pixels[idx + 1] = G;
                    pixels[idx + 2] = R;
                    pixels[idx + 3] = 255;
                }
            });

            return BitmapSource.Create(w, h, _dpiX, _dpiY, PixelFormats.Bgra32, null, pixels, stride);
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
            return pixelDist * 25.4 / _dpiX;
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
            double factor = 25.4 / _dpiX;
            return areaPixels2 * factor * factor;
        }

        #endregion
    }
}
