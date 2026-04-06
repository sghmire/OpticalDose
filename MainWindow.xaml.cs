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
using Wpf.Ui.Controls;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;
using BitMiracle.LibTiff.Classic;
using System.Runtime.InteropServices;

namespace FilmAnalysis
{
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

        private bool _isSelectingROI = false;
        private bool _isDrawing = false;
        private Point _startPoint;

        // Raw High-Precision Image Data
        private double[,] _redChannel;
        private double[,] _greenChannel;
        private double[,] _blueChannel;
        private int _imgWidth, _imgHeight;
        private double _tiffDpi = 72; // Default DPI

        public MainWindow()
        {
            InitializeComponent();
            InitializeCalibrationData();
        }

        private void InitializeCalibrationData()
        {
            CalibrationPoints = new ObservableCollection<CalibrationPoint>
            {
                new CalibrationPoint { Dose = 0, Red = 55000, Green = 55000, Blue = 55000 },
                new CalibrationPoint { Dose = 100, Red = 42000, Green = 45000, Blue = 48000 },
                new CalibrationPoint { Dose = 200, Red = 31000, Green = 35000, Blue = 41000 },
                new CalibrationPoint { Dose = 500, Red = 15000, Green = 20000, Blue = 28000 }
            };
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
                    MainDisplayImage.Source = bitmap;
                    
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

                _redChannel = new double[_imgHeight, _imgWidth];
                _greenChannel = new double[_imgHeight, _imgWidth];
                _blueChannel = new double[_imgHeight, _imgWidth];

                int[] raster = new int[_imgWidth * _imgHeight];
                if (!image.ReadRGBAImage(_imgWidth, _imgHeight, raster))
                {
                    throw new Exception("ReadRGBAImage failed.");
                }

                // LibTiff ReadRGBAImage returns pixels in a bottom-up raster
                // with ABGR format in a 32-bit int.
                // However, for high-precision dosimetry we often want the raw 16-bit values if available.
                // ReadRGBAImage always converts to 8-bit per channel.
                
                // If the user wants 16-bit (no information loss), we should read scanlines instead.
                
                if (bitsPerSample == 16)
                {
                    byte[] scanline = new byte[image.ScanlineSize()];
                    for (int row = 0; row < _imgHeight; row++)
                    {
                        image.ReadScanline(scanline, row);
                        for (int col = 0; col < _imgWidth; col++)
                        {
                            int offset = col * samplesPerPixel * 2; // 2 bytes per sample
                            ushort b = BitConverter.ToUInt16(scanline, offset);
                            ushort g = (samplesPerPixel > 1) ? BitConverter.ToUInt16(scanline, offset + 2) : b;
                            ushort r = (samplesPerPixel > 2) ? BitConverter.ToUInt16(scanline, offset + 4) : b;
                            
                            _redChannel[row, col] = r;
                            _greenChannel[row, col] = g;
                            _blueChannel[row, col] = b;
                        }
                    }
                }
                else
                {
                    // 8-bit or other
                    for (int y = 0; y < _imgHeight; y++)
                    {
                        for (int x = 0; x < _imgWidth; x++)
                        {
                            int pixel = raster[(_imgHeight - 1 - y) * _imgWidth + x];
                            _redChannel[y, x] = Tiff.GetR(pixel);
                            _greenChannel[y, x] = Tiff.GetG(pixel);
                            _blueChannel[y, x] = Tiff.GetB(pixel);
                        }
                    }
                }

                // Create Display Bitmap (normalized to 8-bit)
                UpdateDisplayFromRaw();
            }
        }

        private void UpdateDisplayFromRaw()
        {
            if (_redChannel == null) return;

            int width = _redChannel.GetLength(1);
            int height = _redChannel.GetLength(0);
            
            // Find max for normalization if needed, but usually we just scale 16-bit to 8-bit
            // Most dosimetry TIFFs are 16-bit.
            double maxVal = 0;
            foreach (var v in _redChannel) if (v > maxVal) maxVal = v;
            foreach (var v in _greenChannel) if (v > maxVal) maxVal = v;
            foreach (var v in _blueChannel) if (v > maxVal) maxVal = v;

            if (maxVal <= 255) maxVal = 255; // 8-bit
            else maxVal = 65535; // 16-bit

            byte[] pixels = new byte[width * height * 4]; // BGRA
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int idx = (row * width + col) * 4;
                    pixels[idx] = (byte)(_blueChannel[row, col] * 255.0 / maxVal);
                    pixels[idx + 1] = (byte)(_greenChannel[row, col] * 255.0 / maxVal);
                    pixels[idx + 2] = (byte)(_redChannel[row, col] * 255.0 / maxVal);
                    pixels[idx + 3] = 255;
                }
            }

            PixelFormat format = PixelFormats.Bgra32;
            int stride = width * 4;
            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
            MainDisplayImage.Source = bitmap;
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
            ConfigComboBox.Items.Clear();
            ConfigComboBox.Items.Add("--- Select ---");
            ConfigComboBox.SelectedIndex = 0;
        }

        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            if (CalibrationPoints == null || CalibrationPoints.Count < 2)
            {
                System.Windows.MessageBox.Show("Please enter at least 2 data points for fitting.", "Insufficient Data");
                return;
            }

            try
            {
                // 1. Extract and Normalize Data
                double[] doses = CalibrationPoints.Select(p => p.Dose).ToArray();
                double[] rNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Red, 1) / 65535.0)).ToArray();
                double[] gNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Green, 1) / 65535.0)).ToArray();
                double[] bNorm = CalibrationPoints.Select(p => -Math.Log10(Math.Max(p.Blue, 1) / 65535.0)).ToArray();

                // 2. Get UI Parameters
                int degree = int.Parse((DegreeFitDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "3");
                string channelLogic = (ChannelFitDropDown.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "R";

                var config = new CalibrationConfig
                {
                    PolyDegree = degree,
                    ChannelType = channelLogic.Contains("Triple") ? "Triple" : (channelLogic.Contains("Dual") ? "Dual" : "Single"),
                    TargetChannel = channelLogic
                };

                double r2 = 0;

                // 3. Mathematical Fitting
                if (channelLogic.StartsWith("Single"))
                {
                    double[] chData = channelLogic.Contains("Red") ? rNorm : (channelLogic.Contains("Green") ? gNorm : bNorm);
                    config.RedCoefficients = FittingMath.PolyFit(chData, doses, degree);
                    r2 = FittingMath.CalculateRSquared(chData, doses, config.RedCoefficients);
                }
                else if (channelLogic.StartsWith("Triple"))
                {
                    config.RedCoefficients = FittingMath.PolyFit(rNorm, doses, degree);
                    config.GreenCoefficients = FittingMath.PolyFit(gNorm, doses, degree);
                    config.BlueCoefficients = FittingMath.PolyFit(bNorm, doses, degree);

                    config.DeltaOpt = FittingMath.OptimizeTripleChannelDelta(rNorm, gNorm, bNorm, 
                        config.RedCoefficients, config.GreenCoefficients, config.BlueCoefficients);

                    // For Triple Channel, R2 is calculated on the averaged dose
                    double[] avgDoseFit = new double[doses.Length];
                    for (int i = 0; i < doses.Length; i++)
                    {
                        double rD = FittingMath.PolyVal(config.RedCoefficients, rNorm[i]);
                        double gD = FittingMath.PolyVal(config.GreenCoefficients, gNorm[i]);
                        double bD = FittingMath.PolyVal(config.BlueCoefficients, bNorm[i]);
                        avgDoseFit[i] = (rD + gD + bD) / 3.0 * config.DeltaOpt;
                    }
                    
                    // Simple R2 on the mean dose
                    r2 = FittingMath.CalculateRSquared(new double[doses.Length], doses, new double[0]); // We'll just calculate it manually
                    double ssTot = doses.Sum(d => Math.Pow(d - doses.Average(), 2));
                    double ssRes = 0;
                    for(int i=0; i<doses.Length; i++) ssRes += Math.Pow(doses[i] - avgDoseFit[i], 2);
                    r2 = 1 - (ssRes / ssTot);
                }

                // 4. Update UI Results
                CurrentConfig = config;
                RSquaredText.Text = r2.ToString("F4");

                // Color Coding
                var parent = RSquaredText.Parent as StackPanel;
                var border = parent?.Parent as Border;
                if (border != null)
                {
                    if (r2 >= 0.9995) border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22")); // ForestGreen
                    else if (r2 >= 0.9990) border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDAA520")); // GoldenRod
                    else border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB22222")); // Firebrick
                }

                StatusText.Text = "Fit Ready!";
                StatusIndicator.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF228B22"));
                CalibrationInfoText.Text = $"Active Fit: {channelLogic}\nDegree: {degree}\nR²: {r2:F5}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error calculating fit: {ex.Message}", "Math Error");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement Save logic from Matlab
        }

        private void AutoCenter_Click(object sender, RoutedEventArgs e) { }
        private void ManuallyAlign_Click(object sender, RoutedEventArgs e) { }
        private void AutoAlign_Click(object sender, RoutedEventArgs e) { }
        private void Rotation_Click(object sender, RoutedEventArgs e) { }
        private void Flip_Click(object sender, RoutedEventArgs e) { }
        private void Crop_Click(object sender, RoutedEventArgs e) { }
        private void Filter_Click(object sender, RoutedEventArgs e) { }
        private void ConvertToDose_Click(object sender, RoutedEventArgs e) { }
        private void ExtractROI_Click(object sender, RoutedEventArgs e)
        {
            _isSelectingROI = true;
            System.Windows.MessageBox.Show("Click and Drag on the image to select the Region of Interest.", "ROI Selection Mode");
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelectingROI) return;

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

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;
            _isDrawing = false;
            _isSelectingROI = false;
            SelectionRect.Visibility = Visibility.Collapsed;

            // Logic to calculate pixel averages goes here
            PerformROIExtraction();
        }


        private void PerformROIExtraction()
        {
            if (MainDisplayImage.Source is not BitmapSource bitmapSource) return;

            // 1. Get the actual Pixel coordinates
            double xRatio = bitmapSource.PixelWidth / MainDisplayImage.ActualWidth;
            double yRatio = bitmapSource.PixelHeight / MainDisplayImage.ActualHeight;

            double x = Canvas.GetLeft(SelectionRect) * xRatio;
            double y = Canvas.GetTop(SelectionRect) * yRatio;
            double w = SelectionRect.Width * xRatio;
            double h = SelectionRect.Height * yRatio;

            int left = (int)x;
            int top = (int)y;
            int width = (int)Math.Max(1, w);
            int height = (int)Math.Max(1, h);

            // Bounds check
            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (left + width > bitmapSource.PixelWidth) width = bitmapSource.PixelWidth - left;
            if (top + height > bitmapSource.PixelHeight) height = bitmapSource.PixelHeight - top;

            try
            {
                double sumR = 0, sumG = 0, sumB = 0;
                int count = 0;

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
                    // Fallback to BitmapSource (already handled in previous version, but let's re-implement for consistency)
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

                if (count > 0)
                {
                    double avgR = sumR / count;
                    double avgG = sumG / count;
                    double avgB = sumB / count;

                    // 4. Update the Calibration List
                    // We'll prompt for the dose value or use 0 as default
                    var newPoint = new CalibrationPoint 
                    { 
                        Dose = 0, 
                        Red = (int)avgR, 
                        Green = (int)avgG, 
                        Blue = (int)avgB 
                    };
                    CalibrationPoints.Add(newPoint);
                    
                    System.Windows.MessageBox.Show($"Extracted Means:\nR: {avgR:F1}\nG: {avgG:F1}\nB: {avgB:F1}", "ROI Extraction Result");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error during pixel sampling: {ex.Message}", "Sampling Error");
            }
        }
        private void Measurement_Click(object sender, RoutedEventArgs e) { }
    }
}