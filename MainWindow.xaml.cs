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
                    MainDisplayImage.Source = new BitmapImage(new Uri(dlg.FileName));
                }
                catch
                {
                    System.Windows.MessageBox.Show("Unable to load the selected image.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
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
            // Map UI coordinates (SelectionCanvas) to Pixel coordinates (BitmapSource)
            double xRatio = bitmapSource.PixelWidth / MainDisplayImage.ActualWidth;
            double yRatio = bitmapSource.PixelHeight / MainDisplayImage.ActualHeight;

            double x = Canvas.GetLeft(SelectionRect) * xRatio;
            double y = Canvas.GetTop(SelectionRect) * yRatio;
            double w = SelectionRect.Width * xRatio;
            double h = SelectionRect.Height * yRatio;

            Int32Rect region = new Int32Rect((int)x, (int)y, (int)Math.Max(1, w), (int)Math.Max(1, h));

            try
            {
                // 2. Extract Pixel Data
                int stride = (region.Width * bitmapSource.Format.BitsPerPixel + 7) / 8;
                byte[] pixels = new byte[region.Height * stride];
                bitmapSource.CopyPixels(region, pixels, stride, 0);

                // 3. Simple Mean Analysis (assuming 8-bit or 16nd-bit interleaved)
                double sumR = 0, sumG = 0, sumB = 0;
                int count = 0;

                // Support Bgr24/Bgr32 formats which are common in WPF
                int bytesPerPixel = bitmapSource.Format.BitsPerPixel / 8;
                for (int i = 0; i < pixels.Length; i += bytesPerPixel)
                {
                    if (bitmapSource.Format == PixelFormats.Bgr24 || bitmapSource.Format == PixelFormats.Bgr32)
                    {
                        sumB += pixels[i];
                        sumG += pixels[i + 1];
                        sumR += pixels[i + 2];
                        count++;
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