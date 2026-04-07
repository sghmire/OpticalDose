using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScottPlot;

namespace FilmAnalysis
{
    public partial class AnalysisControl : UserControl
    {
        private double[,] _filmDose;
        private double[,] _planDose;
        private double _filmDpiX, _filmDpiY;
        private double _planDpiX, _planDpiY;
        private double[,] _gammaMap;
        
        // Physical Reference (DICOM center/Isocenter)
        private double _planRefX, _planRefY, _planRefZ;
        private double _planOriginX, _planOriginY;
        private double _planSpacingYSign = 1.0;
        private string _planeOrientation = "Z";

        // ROI Selection
        private bool _roiSelectMode = false;
        private bool _roiDragging = false;
        private Point _roiStart;
        private Canvas? _roiActiveCanvas;
        // ROI in physical DICOM LPS coords (mm)
        private double _roiLpsMinX, _roiLpsMaxX, _roiLpsMinY, _roiLpsMaxY;
        private bool _hasRoi = false;
        
        // Film physical geometry
        private double _filmOriginX = 0, _filmOriginY = 0;

        public event EventHandler AnalysisRequested;
        public event EventHandler PlanRequested;

        public AnalysisControl()
        {
            InitializeComponent();
            SetupPlots();
        }

        private void SetupPlots()
        {
            ProfilePlot.Plot.Title("Dose Profile Comparison");
            ProfilePlot.Plot.XLabel("Position (mm)");
            ProfilePlot.Plot.YLabel("Dose (cGy)");
            ProfilePlot.Plot.Grid.IsVisible = true;
            ProfilePlot.Refresh();
        }

        public void SetFilmDose(double[,] dose, double dpiX, double dpiY)
        {
            _filmDose = dose;
            _filmDpiX = dpiX;
            _filmDpiY = dpiY;
            _filmOriginX = -(dose.GetLength(1) / 2.0) * (25.4 / dpiX);
            _filmOriginY = -(dose.GetLength(0) / 2.0) * (25.4 / dpiY);

            FilmStatusText.Text = $"Loaded ({dose.GetLength(1)}x{dose.GetLength(0)})";
            MeasuredEmptyMsg.Visibility = Visibility.Collapsed;
            RefreshImages();
            UpdateProfiles();
            CheckReady();
        }

        public void SetPlanDose(double[,] dose, double dpiX, double dpiY, double refX, double refY, double refZ, double origX, double origY, double spacingYSign, string orientation)
        {
            _planDose = dose;
            _planDpiX = dpiX;
            _planDpiY = dpiY;
            _planRefX = refX;
            _planRefY = refY;
            _planRefZ = refZ;
            _planOriginX = origX;
            _planOriginY = origY;
            _planSpacingYSign = spacingYSign;
            _planeOrientation = orientation;

            PlanStatusText.Text = $"Loaded ({dose.GetLength(1)}x{dose.GetLength(0)}) at {refZ:F1}mm";
            PlannedEmptyMsg.Visibility = Visibility.Collapsed;
            RefreshImages();
            UpdateProfiles();
            CheckReady();
        }

        private void CheckReady()
        {
            bool hasFilm = _filmDose != null;
            bool hasPlan = _planDose != null;
            
            RunAnalysisButton.IsEnabled = (hasFilm && hasPlan);
            SelectRoiButton.IsEnabled = (hasFilm || hasPlan);

            // Update Sync Button Appearances to indicate if data is already present
            SyncFilmButton.Appearance = hasFilm ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
            SyncDicomButton.Appearance = hasPlan ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
        }

        private void RefreshImages()
        {
            if (_filmDose != null)
                MeasuredImage.Source = GenerateHeatmap(_filmDose, _filmDpiX, _filmDpiY);
            if (_planDose != null)
                PlannedImage.Source = GenerateHeatmap(_planDose, _planDpiX, _planDpiY);
        }

        private BitmapSource GenerateHeatmap(double[,] dose, double dpiX, double dpiY)
        {
            int h = dose.GetLength(0);
            int w = dose.GetLength(1);
            int stride = w * 4;
            byte[] pixels = new byte[h * stride];

            double max = 0.001;
            foreach (var d in dose) if (d > max) max = d;

            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++)
                {
                    double v = Math.Clamp(dose[y, x] / max, 0, 1);
                    var (R, G, B) = GetJetColor(v);
                    int idx = y * stride + x * 4;
                    pixels[idx] = B;
                    pixels[idx + 1] = G;
                    pixels[idx + 2] = R;
                    pixels[idx + 3] = 255;
                }
            });

            return BitmapSource.Create(w, h, dpiX, dpiY, PixelFormats.Bgra32, null, pixels, stride);
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

        private void UpdateProfiles()
        {
            if (_filmDose == null && _planDose == null) return;
            ProfilePlot.Plot.Clear();

            bool isX = ProfileAxisCombo.SelectedIndex == 0;
            
            double fractions = FractionsInput.Value ?? 1.0;

            if (_planDose != null)
            {
                var planProfile = GetCenterProfile(_planDose, isX);
                double spX = 25.4 / _planDpiX;
                double spY = 25.4 / _planDpiY;
                
                // Map Plan indices to relative physical coordinates (mm from Reference/Isocenter)
                double[] x;
                if (isX) x = Enumerable.Range(0, planProfile.Length).Select(i => (_planOriginX + i * spX) - _planRefX).ToArray();
                else x = Enumerable.Range(0, planProfile.Length).Select(i => (_planOriginY + i * spY * _planSpacingYSign) - _planRefY).ToArray();

                // Scale dose by fractions
                double[] scaledProfile = planProfile.Select(p => p / fractions).ToArray();

                var sig = ProfilePlot.Plot.Add.Scatter(x, scaledProfile);
                sig.LegendText = "Planned (DICOM)";
                sig.Color = ScottPlot.Color.FromHex("#1E90FF"); // DodgerBlue
                sig.LineWidth = 2.0f;
            }

            if (_filmDose != null)
            {
                var filmProfile = GetCenterProfile(_filmDose, isX);
                double spacing = 25.4 / (isX ? _filmDpiX : _filmDpiY);
                double shift = isX ? (_filmDose != null ? (XShiftInput.Value ?? 0.0) : 0.0) : (_filmDose != null ? (YShiftInput.Value ?? 0.0) : 0.0);
                double fOrg = isX ? _filmOriginX : _filmOriginY;

                double[] x = Enumerable.Range(0, filmProfile.Length).Select(i => i * spacing + fOrg + shift).ToArray();
                var sig = ProfilePlot.Plot.Add.Scatter(x, filmProfile);
                sig.LegendText = "Measured (Film)";
                sig.Color = ScottPlot.Color.FromHex("#00FF00"); // Lime
                sig.LineWidth = 1.5f;
            }

            ProfilePlot.Plot.Legend.IsVisible = true;
            ProfilePlot.Plot.Axes.AutoScale();
            ProfilePlot.Refresh();
        }

        private double[] GetCenterProfile(double[,] dose, bool horizontal)
        {
            int h = dose.GetLength(0);
            int w = dose.GetLength(1);
            if (horizontal)
            {
                int midY = h / 2;
                double[] p = new double[w];
                for (int x = 0; x < w; x++) p[x] = dose[midY, x];
                return p;
            }
            else
            {
                int midX = w / 2;
                double[] p = new double[h];
                for (int y = 0; y < h; y++) p[y] = dose[y, midX];
                return p;
            }
        }

        private async void RunAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            if (_filmDose == null || _planDose == null) return;

            RunAnalysisButton.IsEnabled = false;
            StatusIndicatorActive(true);

            double dd = GammaDdInput.Value ?? 3.0;
            double dta = GammaDtaInput.Value ?? 3.0;
            double threshold = GammaThresholdInput.Value ?? 10.0;
            bool isGlobal = GammaModeCombo.SelectedIndex == 0;
            double shiftX = XShiftInput.Value ?? 0.0;
            double shiftY = YShiftInput.Value ?? 0.0;
            double fractions = FractionsInput.Value ?? 1.0;

            // No array cropping needed, Gamma loop handles the physical ROI bounds directly
            await Task.Run(() => PerformGammaAnalysis(_filmDose, _planDose, dd, dta, threshold, isGlobal, shiftX, shiftY, fractions));

            DisplayGammaResults();
            StatusIndicatorActive(false);
            RunAnalysisButton.IsEnabled = true;
        }

        private void PerformGammaAnalysis(double[,] filmDose, double[,] planDose, double dd_percent, double dta_mm, double threshold_percent, bool isGlobal, double shiftX, double shiftY, double fractions)
        {
            int fh = filmDose.GetLength(0);
            int fw = filmDose.GetLength(1);
            int ph = planDose.GetLength(0);
            int pw = planDose.GetLength(1);

            _gammaMap = new double[fh, fw];

            double filmSpacingX = 25.4 / _filmDpiX;
            double filmSpacingY = 25.4 / _filmDpiY;
            double planSpacingX = 25.4 / _planDpiX;
            double planSpacingY = 25.4 / _planDpiY;

            double maxPlan = 0.001;
            foreach (var d in _planDose) if (d > maxPlan) maxPlan = d;
            
            // Adjust max plan by fractions for thresholding
            maxPlan /= fractions;

            double threshVal = maxPlan * (threshold_percent / 100.0);

            // Search window in Plan units
            int winX = (int)Math.Ceiling(dta_mm / planSpacingX);
            int winY = (int)Math.Ceiling(dta_mm / planSpacingY);

            Parallel.For(0, fh, fy =>
            {
                for (int fx = 0; fx < fw; fx++)
                {
                    double filmVal = _filmDose[fy, fx];
                    if (filmVal < threshVal) { _gammaMap[fy, fx] = double.NaN; continue; }

                    // Film physical position
                    double fmmX = fx * filmSpacingX + _filmOriginX + shiftX;
                    double fmmY = fy * filmSpacingY + _filmOriginY + shiftY;

                    // Physical DICOM coordinates (LPS mm)
                    double lpsX = fmmX + _planRefX;
                    double lpsY = fmmY + _planRefY;

                    // Skip if outside ROI
                    if (_hasRoi)
                    {
                        if (lpsX < _roiLpsMinX || lpsX > _roiLpsMaxX || lpsY < _roiLpsMinY || lpsY > _roiLpsMaxY)
                        {
                            _gammaMap[fy, fx] = double.NaN;
                            continue;
                        }
                    }

                    // Nearest Plan index — use origin-based mapping (Bug #6 fix)
                    int npx = (int)Math.Round((lpsX - _planOriginX) / planSpacingX);
                    int npy = (int)Math.Round((lpsY - _planOriginY) / (planSpacingY * _planSpacingYSign));

                    double minGammaSq = double.MaxValue;

                    double step = 1.0 / 3.0; // Sub-pixel tracking (evaluates 9 points per plan pixel area)
                    double startPy = Math.Max(0, npy - winY);
                    double endPy = Math.Min(ph - 1, npy + winY);
                    double startPx = Math.Max(0, npx - winX);
                    double endPx = Math.Min(pw - 1, npx + winX);

                    for (double py = startPy; py <= endPy; py += step)
                    {
                        for (double px = startPx; px <= endPx; px += step)
                        {
                            // Distance Component: Calculate physical LPS positions relative to Ref Point
                            double pmmX = (_planOriginX + px * planSpacingX) - _planRefX;
                            double pmmY = (_planOriginY + py * planSpacingY * _planSpacingYSign) - _planRefY;

                            double distSq = Math.Pow(fmmX - pmmX, 2) + Math.Pow(fmmY - pmmY, 2);
                            double gammaDistSq = distSq / Math.Pow(dta_mm, 2);

                            // Early Exit Math: If physical distance alone exceeds minGammaSq mathematically, skip dose lookup!
                            if (gammaDistSq >= minGammaSq) continue;

                            double planVal = GetInterpolatedPlanDose(px, py);
                            
                            // Scale Plan dose for comparison
                            double scaledPlanVal = planVal / fractions;

                            // Dose Difference Component
                            double dDiffPercent;
                            if (isGlobal)
                            {
                                dDiffPercent = (filmVal - scaledPlanVal) / maxPlan * 100.0;
                            }
                            else
                            {
                                // Local Dose Normalization
                                double localNorm = scaledPlanVal > 0.001 ? scaledPlanVal : 0.001; 
                                dDiffPercent = (filmVal - scaledPlanVal) / localNorm * 100.0;
                            }
                            
                            double gammaSq = Math.Pow(dDiffPercent / dd_percent, 2) + gammaDistSq;
                            if (gammaSq < minGammaSq) minGammaSq = gammaSq;
                        }
                    }
                    _gammaMap[fy, fx] = Math.Sqrt(minGammaSq);
                }
            });
        }

        private double GetInterpolatedPlanDose(double px, double py)
        {
            int ph = _planDose.GetLength(0);
            int pw = _planDose.GetLength(1);

            int x0 = (int)Math.Floor(px);
            int x1 = x0 + 1;
            int y0 = (int)Math.Floor(py);
            int y1 = y0 + 1;

            x0 = Math.Max(0, Math.Min(pw - 1, x0));
            x1 = Math.Max(0, Math.Min(pw - 1, x1));
            y0 = Math.Max(0, Math.Min(ph - 1, y0));
            y1 = Math.Max(0, Math.Min(ph - 1, y1));

            double dx = px - x0;
            double dy = py - y0;

            double c00 = _planDose[y0, x0];
            double c10 = _planDose[y0, x1];
            double c01 = _planDose[y1, x0];
            double c11 = _planDose[y1, x1];

            return c00 * (1 - dx) * (1 - dy) +
                   c10 * dx * (1 - dy) +
                   c01 * (1 - dx) * dy +
                   c11 * dx * dy;
        }

        private void DisplayGammaResults()
        {
            if (_gammaMap == null) return;

            int fh = _gammaMap.GetLength(0);
            int fw = _gammaMap.GetLength(1);
            int totalPoints = 0;
            int passPoints = 0;

            int stride = fw * 4;
            byte[] pixels = new byte[fh * stride];

            for (int y = 0; y < fh; y++)
            {
                for (int x = 0; x < fw; x++)
                {
                    double g = _gammaMap[y, x];
                    int idx = y * stride + x * 4;
                    if (double.IsNaN(g))
                    {
                        // Background (Transparent or Gray)
                        pixels[idx] = 20; pixels[idx+1] = 20; pixels[idx+2] = 20; pixels[idx+3] = 255;
                        continue;
                    }

                    totalPoints++;
                    if (g <= 1.0) passPoints++;

                    // Color: Green if <= 1, Red if > 1
                    if (g <= 1.0) 
                    {
                        // Gradient from Green to Black? Or just Green?
                        byte green = (byte)(255 * (1.0 - g * 0.5));
                        pixels[idx] = 0; pixels[idx+1] = green; pixels[idx+2] = 0; pixels[idx+3] = 255;
                    }
                    else
                    {
                        // Red for fail
                        byte red = (byte)Math.Min(255, 150 + (g-1)*20);
                        pixels[idx] = 0; pixels[idx+1] = 0; pixels[idx+2] = red; pixels[idx+3] = 255;
                    }
                }
            }

            GammaImage.Source = BitmapSource.Create(fw, fh, _filmDpiX, _filmDpiY, PixelFormats.Bgra32, null, pixels, stride);
            GammaEmptyMsg.Visibility = Visibility.Collapsed;

            double passRate = (double)passPoints / totalPoints * 100.0;
            PassRateText.Text = $"{passRate:F1} %";
            PassRateText.Foreground = passRate >= 95 ? Brushes.Lime : Brushes.Red;
            PassRateStatus.Text = passRate >= 95 ? "PASS (Criteria: 95%)" : "FAIL (Criteria: 95%)";
            ResultsPanel.Visibility = Visibility.Visible;
        }

        private void StatusIndicatorActive(bool active)
        {
            // Implementation of spinner if added
        }

        private void SyncFilmButton_Click(object sender, RoutedEventArgs e)
        {
             AnalysisRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SyncDicomButton_Click(object sender, RoutedEventArgs e)
        {
             PlanRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Alignment_Changed(object sender, RoutedEventArgs e) 
        {
            if (XShiftInput == null || YShiftInput == null || MeasuredImageTranslation == null) return;
            
            // Update visual translation (mm to pixels)
            double pixX = (XShiftInput.Value ?? 0.0) * (_filmDpiX / 25.4);
            double pixY = (YShiftInput.Value ?? 0.0) * (_filmDpiY / 25.4);
            
            MeasuredImageTranslation.X = pixX;
            MeasuredImageTranslation.Y = pixY;

            UpdateProfiles();
        }
        private void GammaParams_Changed(object sender, RoutedEventArgs e) { /* Auto-run optional */ }
        private void ProfileAxisCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateProfiles();

        // ===== ROI Selection =====

        private void SelectRoi_Click(object sender, RoutedEventArgs e)
        {
            _roiSelectMode = true;
            SelectRoiButton.Content = "Draw rectangle on a dose map...";
            SelectRoiButton.IsEnabled = false;
            Cursor = Cursors.Cross;
        }

        private void ClearRoi_Click(object sender, RoutedEventArgs e)
        {
            _hasRoi = false;
            _roiSelectMode = false;
            MeasuredRoiRect.Visibility = Visibility.Collapsed;
            PlannedRoiRect.Visibility = Visibility.Collapsed;
            ClearRoiButton.IsEnabled = false;
            CropButton.IsEnabled = false;
            SelectRoiButton.Content = "Select ROI";
            SelectRoiButton.IsEnabled = (_filmDose != null || _planDose != null);
            RoiStatusText.Text = "No ROI selected";
            Cursor = Cursors.Arrow;
        }

        private void RoiCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_roiSelectMode) return;
            _roiActiveCanvas = sender as Canvas;
            if (_roiActiveCanvas == null) return;

            _roiStart = e.GetPosition(_roiActiveCanvas);
            _roiDragging = true;
            _roiActiveCanvas.CaptureMouse();

            // Show the rectangle on the active canvas
            var rect = _roiActiveCanvas == MeasuredCanvas ? MeasuredRoiRect : PlannedRoiRect;
            Canvas.SetLeft(rect, _roiStart.X);
            Canvas.SetTop(rect, _roiStart.Y);
            rect.Width = 0;
            rect.Height = 0;
            rect.Visibility = Visibility.Visible;
        }

        private void RoiCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_roiDragging || _roiActiveCanvas == null) return;
            var pos = e.GetPosition(_roiActiveCanvas);

            var rect = _roiActiveCanvas == MeasuredCanvas ? MeasuredRoiRect : PlannedRoiRect;
            double x = Math.Min(_roiStart.X, pos.X);
            double y = Math.Min(_roiStart.Y, pos.Y);
            double w = Math.Abs(pos.X - _roiStart.X);
            double h = Math.Abs(pos.Y - _roiStart.Y);

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = w;
            rect.Height = h;
        }

        private void RoiCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_roiDragging || _roiActiveCanvas == null) return;
            _roiDragging = false;
            _roiActiveCanvas.ReleaseMouseCapture();

            var pos = e.GetPosition(_roiActiveCanvas);
            double cw = _roiActiveCanvas.ActualWidth;
            double ch = _roiActiveCanvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            // Determine which dose map this canvas represents
            bool isFilm = _roiActiveCanvas == MeasuredCanvas;
            var dose = isFilm ? _filmDose : _planDose;
            if (dose == null) return;

            int imgCols = dose.GetLength(1), imgRows = dose.GetLength(0);
            double imgAspect = (double)imgCols / imgRows;
            double canAspect = cw / ch;

            // Calculate rendered image bounds (Stretch=Uniform)
            double rw, rh, ox, oy;
            if (imgAspect > canAspect) { rw = cw; rh = cw / imgAspect; ox = 0; oy = (ch - rh) / 2; }
            else { rh = ch; rw = ch * imgAspect; ox = (cw - rw) / 2; oy = 0; }

            // Convert canvas pixels to fractional image coords
            double x1 = Math.Min(_roiStart.X, pos.X);
            double y1 = Math.Min(_roiStart.Y, pos.Y);
            double x2 = Math.Max(_roiStart.X, pos.X);
            double y2 = Math.Max(_roiStart.Y, pos.Y);

            double fracX1 = Math.Max(0, (x1 - ox) / rw);
            double fracY1 = Math.Max(0, (y1 - oy) / rh);
            double fracX2 = Math.Min(1, (x2 - ox) / rw);
            double fracY2 = Math.Min(1, (y2 - oy) / rh);

            if (fracX2 - fracX1 < 0.01 || fracY2 - fracY1 < 0.01)
            {
                // Too small, cancel
                ClearRoi_Click(sender, e);
                return;
            }

            // Convert fractional canvas ROI to physical DICOM LPS bounds
            Point p1 = isFilm ? FilmFracToLps(fracX1, fracY1) : PlanFracToLps(fracX1, fracY1);
            Point p2 = isFilm ? FilmFracToLps(fracX2, fracY2) : PlanFracToLps(fracX2, fracY2);

            _roiLpsMinX = Math.Min(p1.X, p2.X);
            _roiLpsMaxX = Math.Max(p1.X, p2.X);
            _roiLpsMinY = Math.Min(p1.Y, p2.Y);
            _roiLpsMaxY = Math.Max(p1.Y, p2.Y);

            _hasRoi = true;
            _roiSelectMode = false;
            Cursor = Cursors.Arrow;
            ClearRoiButton.IsEnabled = true;
            CropButton.IsEnabled = true;
            SelectRoiButton.Content = "Select ROI";
            SelectRoiButton.IsEnabled = true;

            // Show ROI on both canvases
            SyncRoiRectangles();

            double widthMm = _roiLpsMaxX - _roiLpsMinX;
            double heightMm = _roiLpsMaxY - _roiLpsMinY;
            RoiStatusText.Text = $"ROI: {widthMm:F1} x {heightMm:F1} mm (Physical Area)";
        }

        private Point FilmFracToLps(double fracX, double fracY)
        {
            double filmSpacingX = 25.4 / _filmDpiX;
            double filmSpacingY = 25.4 / _filmDpiY;
            double shiftX = XShiftInput?.Value ?? 0.0;
            double shiftY = YShiftInput?.Value ?? 0.0;
            int fw = _filmDose.GetLength(1);
            int fh = _filmDose.GetLength(0);

            double px = fracX * fw;
            double py = fracY * fh;

            double fmmX = px * filmSpacingX + _filmOriginX + shiftX;
            double fmmY = py * filmSpacingY + _filmOriginY + shiftY;

            return new Point(fmmX + _planRefX, fmmY + _planRefY);
        }

        private Point LpsToFilmFrac(double lpsX, double lpsY)
        {
            double filmSpacingX = 25.4 / _filmDpiX;
            double filmSpacingY = 25.4 / _filmDpiY;
            double shiftX = XShiftInput?.Value ?? 0.0;
            double shiftY = YShiftInput?.Value ?? 0.0;
            int fw = _filmDose.GetLength(1);
            int fh = _filmDose.GetLength(0);

            double fmmX = lpsX - _planRefX;
            double fmmY = lpsY - _planRefY;

            double px = (fmmX - shiftX - _filmOriginX) / filmSpacingX;
            double py = (fmmY - shiftY - _filmOriginY) / filmSpacingY;

            return new Point(px / fw, py / fh);
        }

        private Point PlanFracToLps(double fracX, double fracY)
        {
            double planSpacingX = 25.4 / _planDpiX;
            double planSpacingY = 25.4 / _planDpiY;
            int pw = _planDose.GetLength(1);
            int ph = _planDose.GetLength(0);

            double px = fracX * pw;
            double py = fracY * ph;

            double lpsX = px * planSpacingX + _planOriginX;
            double lpsY = py * planSpacingY * _planSpacingYSign + _planOriginY;

            return new Point(lpsX, lpsY);
        }

        private Point LpsToPlanFrac(double lpsX, double lpsY)
        {
            double planSpacingX = 25.4 / _planDpiX;
            double planSpacingY = 25.4 / _planDpiY;
            int pw = _planDose.GetLength(1);
            int ph = _planDose.GetLength(0);

            double px = (lpsX - _planOriginX) / planSpacingX;
            double py = (lpsY - _planOriginY) / (planSpacingY * _planSpacingYSign);

            return new Point(px / pw, py / ph);
        }

        private void SyncRoiRectangles()
        {
            // Draw ROI rectangle on Measured canvas
            if (_filmDose != null)
            {
                Point p1 = LpsToFilmFrac(_roiLpsMinX, _roiLpsMinY);
                Point p2 = LpsToFilmFrac(_roiLpsMaxX, _roiLpsMaxY);
                double fracX1 = Math.Min(p1.X, p2.X);
                double fracY1 = Math.Min(p1.Y, p2.Y);
                double fracX2 = Math.Max(p1.X, p2.X);
                double fracY2 = Math.Max(p1.Y, p2.Y);
                DrawRoiOnCanvas(MeasuredCanvas, MeasuredRoiRect, _filmDose.GetLength(1), _filmDose.GetLength(0), fracX1, fracY1, fracX2, fracY2);
            }
            // Draw ROI rectangle on Planned canvas
            if (_planDose != null)
            {
                Point p1 = LpsToPlanFrac(_roiLpsMinX, _roiLpsMinY);
                Point p2 = LpsToPlanFrac(_roiLpsMaxX, _roiLpsMaxY);
                double fracX1 = Math.Min(p1.X, p2.X);
                double fracY1 = Math.Min(p1.Y, p2.Y);
                double fracX2 = Math.Max(p1.X, p2.X);
                double fracY2 = Math.Max(p1.Y, p2.Y);
                DrawRoiOnCanvas(PlannedCanvas, PlannedRoiRect, _planDose.GetLength(1), _planDose.GetLength(0), fracX1, fracY1, fracX2, fracY2);
            }
        }

        private void DrawRoiOnCanvas(Canvas canvas, Rectangle rect, int imgCols, int imgRows, double fracX1, double fracY1, double fracX2, double fracY2)
        {
            double cw = canvas.ActualWidth, ch = canvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            double imgAspect = (double)imgCols / imgRows;
            double canAspect = cw / ch;
            double rw, rh, ox, oy;
            if (imgAspect > canAspect) { rw = cw; rh = cw / imgAspect; ox = 0; oy = (ch - rh) / 2; }
            else { rh = ch; rw = ch * imgAspect; ox = (cw - rw) / 2; oy = 0; }

            Canvas.SetLeft(rect, ox + fracX1 * rw);
            Canvas.SetTop(rect, oy + fracY1 * rh);
            rect.Width = (fracX2 - fracX1) * rw;
            rect.Height = (fracY2 - fracY1) * rh;
            rect.Visibility = Visibility.Visible;
        }

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasRoi || (_filmDose == null && _planDose == null)) return;

            if (_filmDose != null)
            {
                double filmSpX = 25.4 / _filmDpiX;
                double filmSpY = 25.4 / _filmDpiY;
                double shiftX = XShiftInput?.Value ?? 0.0;
                double shiftY = YShiftInput?.Value ?? 0.0;

                double fmmMinX = _roiLpsMinX - _planRefX;
                double fmmMaxX = _roiLpsMaxX - _planRefX;
                double fmmMinY = _roiLpsMinY - _planRefY;
                double fmmMaxY = _roiLpsMaxY - _planRefY;

                int fx1 = (int)Math.Max(0, Math.Round((fmmMinX - shiftX - _filmOriginX) / filmSpX));
                int fx2 = (int)Math.Min(_filmDose.GetLength(1), Math.Round((fmmMaxX - shiftX - _filmOriginX) / filmSpX));
                int fy1 = (int)Math.Max(0, Math.Round((fmmMinY - shiftY - _filmOriginY) / filmSpY));
                int fy2 = (int)Math.Min(_filmDose.GetLength(0), Math.Round((fmmMaxY - shiftY - _filmOriginY) / filmSpY));

                if (fx2 > fx1 && fy2 > fy1)
                {
                    var cropped = new double[fy2 - fy1, fx2 - fx1];
                    for (int r = 0; r < fy2 - fy1; r++)
                        for (int c = 0; c < fx2 - fx1; c++)
                            cropped[r, c] = _filmDose[fy1 + r, fx1 + c];
                    
                    _filmDose = cropped;
                    _filmOriginX += fx1 * filmSpX;
                    _filmOriginY += fy1 * filmSpY;
                }
            }

            if (_planDose != null)
            {
                double planSpX = 25.4 / _planDpiX;
                double planSpY = 25.4 / _planDpiY;

                int px1 = (int)Math.Max(0, Math.Round((_roiLpsMinX - _planOriginX) / planSpX));
                int px2 = (int)Math.Min(_planDose.GetLength(1), Math.Round((_roiLpsMaxX - _planOriginX) / planSpX));

                int py1, py2;
                if (_planSpacingYSign > 0)
                {
                    py1 = (int)Math.Round((_roiLpsMinY - _planOriginY) / planSpY);
                    py2 = (int)Math.Round((_roiLpsMaxY - _planOriginY) / planSpY);
                }
                else
                {
                    py1 = (int)Math.Round((_roiLpsMaxY - _planOriginY) / (planSpY * -1));
                    py2 = (int)Math.Round((_roiLpsMinY - _planOriginY) / (planSpY * -1));
                }
                py1 = Math.Max(0, Math.Min(_planDose.GetLength(0), py1));
                py2 = Math.Max(0, Math.Min(_planDose.GetLength(0), py2));
                
                int startY = Math.Min(py1, py2);
                int endY = Math.Max(py1, py2);

                if (px2 > px1 && endY > startY)
                {
                    var cropped = new double[endY - startY, px2 - px1];
                    for (int r = 0; r < endY - startY; r++)
                        for (int c = 0; c < px2 - px1; c++)
                            cropped[r, c] = _planDose[startY + r, px1 + c];
                    
                    _planDose = cropped;
                    _planOriginX += px1 * planSpX;
                    _planOriginY += startY * planSpY * _planSpacingYSign;
                }
            }

            ClearRoi_Click(sender, e);
            RefreshImages();
            UpdateProfiles();
        }
    }
}
