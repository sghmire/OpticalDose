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
using System.Globalization;
using ScottPlot;

namespace OpticalDose
{
    public partial class AnalysisControl : UserControl
    {
        private double[,]? _filmDose;
        private double[,]? _planDose;
        private double _filmDpiX, _filmDpiY;
        private double _planDpiX, _planDpiY;
        private double[,]? _gammaMap;
        private double _lastPassRate = double.NaN;
        
        // Physical Reference (DICOM center/Isocenter)
        private double _planRefX, _planRefY, _planRefZ;
        private double _planOriginX, _planOriginY;
        private double _planSpacingYSign = 1.0;
        private string _planeOrientation = "Z";
        private string _filmFileName = "None";
        private string _dicomFileName = "None";

        // ROI Selection
        private bool _roiSelectMode = false;
        private bool _roiDragging = false;
        private Point _roiStart;
        private Canvas? _roiActiveCanvas;
        // ROI in physical DICOM LPS coords (mm)
        private double _roiLpsMinX, _roiLpsMaxX, _roiLpsMinY, _roiLpsMaxY;
        private bool _hasRoi = false;

        // Profile sampling
        private bool _profilePickMode = false;
        private bool _profileDragging = false;
        private bool _hasProfilePoint = false;
        private double _profileLpsX = 0, _profileLpsY = 0;
        
        // Film physical geometry
        private double _filmOriginX = 0, _filmOriginY = 0;

        public event EventHandler? AnalysisRequested;
        public event EventHandler? PlanRequested;
        
        public event Action<double>? ProgressUpdate;
        public event Action<bool>? ProgressActive;

        public AnalysisControl()
        {
            InitializeComponent();
            SetupPlots();

            // Ensure rulers are refreshed when the control becomes visible (e.g., tab switch)
            this.IsVisibleChanged += (s, e) => { if ((bool)e.NewValue) UpdateRulers(); };
            this.Loaded += (s, e) => UpdateRulers();
        }

        private void SetupPlots()
        {
            ProfilePlot.Plot.Title("Dose Profile Comparison");
            ProfilePlot.Plot.XLabel("Position (mm)");
            ProfilePlot.Plot.YLabel("Dose (cGy)");
            ProfilePlot.Plot.Grid.IsVisible = true;
            ProfilePlot.Refresh();
        }

        public void SetFilmDose(double[,] dose, double dpiX, double dpiY, string fileName, Point? referenceCenter = null)
        {
            _filmDose = dose;
            _filmDpiX = dpiX;
            _filmDpiY = dpiY;
            _filmFileName = System.IO.Path.GetFileName(fileName);
            
            if (referenceCenter.HasValue)
            {
                // Set origin relative to the picked reference center (Isocenter)
                _filmOriginX = -referenceCenter.Value.X * (25.4 / dpiX);
                _filmOriginY = -referenceCenter.Value.Y * (25.4 / dpiY);
            }
            else
            {
                // Fallback to geometric center if no reference center is provided
                _filmOriginX = -(dose.GetLength(1) / 2.0) * (25.4 / dpiX);
                _filmOriginY = -(dose.GetLength(0) / 2.0) * (25.4 / dpiY);
            }

            FilmOverlayText.Text = _filmFileName;
            FilmOverlayBorder.Visibility = Visibility.Visible;
            MeasuredEmptyMsg.Visibility = Visibility.Collapsed;
            RefreshImages();
            UpdateProfiles();
            UpdateProfileCrosshairs();
            CheckReady();
        }

        public void SetPlanDose(double[,] dose, double dpiX, double dpiY, double refX, double refY, double refZ, double origX, double origY, double spacingYSign, string orientation, string fileName, int fractions)
        {
            if (spacingYSign < 0)
            {
                int rows = dose.GetLength(0);
                int cols = dose.GetLength(1);
                var flipped = new double[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        flipped[r, c] = dose[rows - 1 - r, c];
                
                dose = flipped;
                double spYmm = (25.4 / dpiY);
                origY = origY + (rows - 1) * (spYmm * spacingYSign);
                spacingYSign = 1.0;
            }

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
            _dicomFileName = System.IO.Path.GetFileName(fileName);
            
            // Dose Scale defaults to 1.0 (Plan is already baked-in)
            DoseScaleInput.Value = 1.0;

            DicomOverlayText.Text = _dicomFileName;
            DicomOverlayBorder.Visibility = Visibility.Visible;
            PlannedEmptyMsg.Visibility = Visibility.Collapsed;
            RefreshImages();
            UpdateProfiles();
            UpdateProfileCrosshairs();
            CheckReady();
        }

        private void CheckReady()
        {
            bool hasFilm = _filmDose != null;
            bool hasPlan = _planDose != null;
            
            RunAnalysisButton.IsEnabled = (hasFilm && hasPlan);
            SelectRoiButton.IsEnabled = (hasFilm || hasPlan);
        }

        private void RefreshImages()
        {
            if (_filmDose != null)
                MeasuredImage.Source = GenerateHeatmap(_filmDose, _filmDpiX, _filmDpiY);
            if (_planDose != null)
                PlannedImage.Source = GenerateHeatmap(_planDose, _planDpiX, _planDpiY);

            UpdateRulers();
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateRulers();
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
                    var (R, G, B) = ColorMaps.GetJetColor(v);
                    int idx = y * stride + x * 4;
                    pixels[idx] = B;
                    pixels[idx + 1] = G;
                    pixels[idx + 2] = R;
                    pixels[idx + 3] = 255;
                }
            });

            return BitmapSource.Create(w, h, dpiX, dpiY, PixelFormats.Bgra32, null, pixels, stride);
        }

        private void UpdateProfiles()
        {
            if (_filmDose == null && _planDose == null) return;
            ProfilePlot.Plot.Clear();

            bool isX = ProfileAxisCombo.SelectedIndex == 0;
            
            double scaleFactor = DoseScaleInput.Value ?? 1.0;

            // Map profile point to film/plan indices if selected
            int filmRow = _filmDose != null ? _filmDose.GetLength(0) / 2 : 0;
            int filmCol = _filmDose != null ? _filmDose.GetLength(1) / 2 : 0;
            int planRow = _planDose != null ? _planDose.GetLength(0) / 2 : 0;
            int planCol = _planDose != null ? _planDose.GetLength(1) / 2 : 0;

            if (_hasProfilePoint)
            {
                if (_filmDose != null)
                {
                    var fracFilm = LpsToFilmFrac(_profileLpsX, _profileLpsY);
                    filmCol = (int)Math.Round(Math.Clamp(fracFilm.X, 0, 0.9999) * (_filmDose.GetLength(1) - 1));
                    filmRow = (int)Math.Round(Math.Clamp(fracFilm.Y, 0, 0.9999) * (_filmDose.GetLength(0) - 1));
                }
                if (_planDose != null)
                {
                    var fracPlan = LpsToPlanFrac(_profileLpsX, _profileLpsY);
                    planCol = (int)Math.Round(Math.Clamp(fracPlan.X, 0, 0.9999) * (_planDose.GetLength(1) - 1));
                    planRow = (int)Math.Round(Math.Clamp(fracPlan.Y, 0, 0.9999) * (_planDose.GetLength(0) - 1));
                }
            }

            if (_planDose != null)
            {
                var planProfile = GetProfileAt(_planDose, isX, planRow, planCol);
                double spX = 25.4 / _planDpiX;
                double spY = 25.4 / _planDpiY;
                
                // Map Plan indices to relative physical coordinates (mm from Reference/Isocenter)
                double[] x;
                if (isX) x = Enumerable.Range(0, planProfile.Length).Select(i => (_planOriginX + i * spX) - _planRefX).ToArray();
                else x = Enumerable.Range(0, planProfile.Length).Select(i => (_planOriginY + i * spY * _planSpacingYSign) - _planRefY).ToArray();

                // Plan dose is already scaled (baked-in) at extraction
                double[] planData = planProfile;

                var sig = ProfilePlot.Plot.Add.Scatter(x, planData);
                sig.LegendText = "Planned (DICOM)";
                sig.Color = ScottPlot.Color.FromHex("#0078D4"); // Blue
                sig.LineWidth = 1.5f;
                sig.MarkerSize = 0;
            }

            if (_filmDose != null)
            {
                var filmProfile = GetProfileAt(_filmDose, isX, filmRow, filmCol);
                double spacing = 25.4 / (isX ? _filmDpiX : _filmDpiY);
                double shift = isX ? (_filmDose != null ? (XShiftInput.Value ?? 0.0) : 0.0) : (_filmDose != null ? (YShiftInput.Value ?? 0.0) : 0.0);
                double fOrg = isX ? _filmOriginX : _filmOriginY;

                // Scale MEASURED (Film) dose by the scale factor
                double[] scaledMeasured = filmProfile.Select(p => p * scaleFactor).ToArray();

                double[] x = Enumerable.Range(0, filmProfile.Length).Select(i => i * spacing + fOrg + shift).ToArray();
                var sig = ProfilePlot.Plot.Add.Scatter(x, scaledMeasured);
                sig.LegendText = "Measured (Film)";
                sig.Color = ScottPlot.Color.FromHex("#E81123"); // Red
                sig.LineWidth = 1.5f;
                sig.MarkerSize = 0;
            }

            ProfilePlot.Plot.Legend.IsVisible = true;
            ProfilePlot.Plot.Axes.AutoScale();
            ProfilePlot.Refresh();
        }

        private double[] GetProfileAt(double[,] dose, bool horizontal, int fixedRow, int fixedCol)
        {
            int h = dose.GetLength(0);
            int w = dose.GetLength(1);
            fixedRow = Math.Clamp(fixedRow, 0, h - 1);
            fixedCol = Math.Clamp(fixedCol, 0, w - 1);

            if (horizontal)
            {
                double[] p = new double[w];
                for (int x = 0; x < w; x++) p[x] = dose[fixedRow, x];
                return p;
            }
            else
            {
                double[] p = new double[h];
                for (int y = 0; y < h; y++) p[y] = dose[y, fixedCol];
                return p;
            }
        }

        private async void RunAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            if (_filmDose == null || _planDose == null) return;

            var mainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;
            var settings = mainWindow._settings;

            double dd = GammaDdInput.Value ?? 2.0;
            double dta = GammaDtaInput.Value ?? 2.0;
            double threshold = GammaThresholdInput.Value ?? 10.0;
            bool isGlobal = GammaModeCombo.SelectedIndex == 0;
            double shiftX = XShiftInput.Value ?? 0.0;
            double shiftY = YShiftInput.Value ?? 0.0;
            double scaleFactor = DoseScaleInput.Value ?? 1.0;

            // Engine settings from central AppSettings
            double uncertainty = settings.GammaUncertainty;
            double step = settings.GammaSearchStep;
            double smoothingSigma = settings.GammaSmoothingSigma;
            bool useBicubic = settings.GammaUseBicubic;

            double[,] filmDoseToUse = _filmDose;
            if (smoothingSigma > 0.001)
            {
                filmDoseToUse = ApplyGaussianSmoothing(_filmDose, smoothingSigma, _filmDpiX);
            }

            ProgressActive?.Invoke(true);
            StatusIndicatorActive(true);

            var progress = new Progress<double>(v => {
                ProgressUpdate?.Invoke(v);
            });

            // No array cropping needed, Gamma loop handles the physical ROI bounds directly
            await Task.Run(() => PerformGammaAnalysis(filmDoseToUse, _planDose, dd, dta, uncertainty, threshold, isGlobal, shiftX, shiftY, scaleFactor, step, useBicubic, progress));

            DisplayGammaResults();
            ProgressActive?.Invoke(false);
            StatusIndicatorActive(false);
            RunAnalysisButton.IsEnabled = true;
        }

        private void PerformGammaAnalysis(double[,] filmDose, double[,] planDose, double dd_percent, double dta_mm, double uncertainty_percent, double threshold_percent, bool isGlobal, double shiftX, double shiftY, double doseScale, double step, bool useBicubic, IProgress<double>? progress)
        {
            int fh = filmDose.GetLength(0);
            int fw = filmDose.GetLength(1);
            int ph = planDose.GetLength(0);
            int pw = planDose.GetLength(1);

            var gammaMap = new double[fh, fw];

            double filmSpacingX = 25.4 / _filmDpiX;
            double filmSpacingY = 25.4 / _filmDpiY;
            double planSpacingX = 25.4 / _planDpiX;
            double planSpacingY = 25.4 / _planDpiY;

            double maxPlan = 0.001;
            foreach (var d in _planDose) if (d > maxPlan) maxPlan = d;
            
            // Plan is already baked-in
            // maxPlan /= fractions; // REMOVED

            double threshVal = maxPlan * (threshold_percent / 100.0);

            // Search window in Plan units
            int winX = (int)Math.Ceiling(dta_mm / planSpacingX) + 1;
            int winY = (int)Math.Ceiling(dta_mm / planSpacingY) + 1;

            // PRE-SAMPLING PLAN GRID: Up-sample plan dose to avoid repeated interpolation
            bool doPreSample = (step >= 0.05); // Cap upsampling to avoid OOM
            double[,] upsampledPlan = null!;
            int U = 1;
            if (doPreSample)
            {
                U = (int)Math.Round(1.0 / step);
                int upPh = (ph - 1) * U + 1;
                int upPw = (pw - 1) * U + 1;
                upsampledPlan = new double[upPh, upPw];
                Parallel.For(0, upPh, y =>
                {
                    double py = Math.Clamp((double)y / U, 0, ph - 1);
                    for (int x = 0; x < upPw; x++)
                    {
                        double px = Math.Clamp((double)x / U, 0, pw - 1);
                        upsampledPlan[y, x] = useBicubic ? GetBicubicPlanDose(px, py) : GetBilinearPlanDose(px, py);
                    }
                });
            }

            // PRE-CALCULATE RADIAL SPIRAL OFFSETS
            var offsets = new List<(double dx, double dy, double dSq)>();
            for (double dy = -winY; dy <= winY; dy += step)
            {
                for (double dx = -winX; dx <= winX; dx += step)
                {
                    double dSq = (dx * planSpacingX) * (dx * planSpacingX) + (dy * planSpacingY) * (dy * planSpacingY);
                    offsets.Add((dx, dy, dSq));
                }
            }
            offsets.Sort((a, b) => a.dSq.CompareTo(b.dSq));

            int completedRows = 0;
            Parallel.For(0, fh, fy =>
            {
                for (int fx = 0; fx < fw; fx++)
                {
                    double filmVal = filmDose[fy, fx] * doseScale;
                    if (filmVal < threshVal) { gammaMap[fy, fx] = double.NaN; continue; }

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
                            gammaMap[fy, fx] = double.NaN;
                            continue;
                        }
                    }

                    // Nearest Plan index — use origin-based mapping (Bug #6 fix)
                    int npx = (int)Math.Round((lpsX - _planOriginX) / planSpacingX);
                    int npy = (int)Math.Round((lpsY - _planOriginY) / (planSpacingY * _planSpacingYSign));

                    double minGammaSq = double.MaxValue;

                    foreach (var off in offsets)
                    {
                        double px = npx + off.dx;
                        double py = npy + off.dy;

                        if (px < 0 || px > pw - 1 || py < 0 || py > ph - 1) continue;

                        // Distance Component: Calculate physical LPS positions relative to Ref Point
                        double pmmX = (_planOriginX + px * planSpacingX) - _planRefX;
                        double pmmY = (_planOriginY + py * planSpacingY * _planSpacingYSign) - _planRefY;

                        double distSq = (fmmX - pmmX) * (fmmX - pmmX) + (fmmY - pmmY) * (fmmY - pmmY);
                        double gammaDistSq = distSq / (dta_mm * dta_mm);

                        // Early Exit Math: If physical distance alone exceeds minGammaSq mathematically, skip dose lookup!
                        // Since offsets are radially sorted, `gammaDistSq` inherently bounds future discoveries, vastly accelerating the loop.
                        if (gammaDistSq >= minGammaSq) continue;

                        double planVal;
                        if (doPreSample)
                        {
                            int ux = (int)Math.Round(px * U);
                            int uy = (int)Math.Round(py * U);
                            planVal = upsampledPlan[uy, ux];
                        }
                        else
                        {
                            planVal = useBicubic ? GetBicubicPlanDose(px, py) : GetBilinearPlanDose(px, py);
                        }
                        
                        // Dose Difference Component (compare scaled film to baked-in plan)
                        double doseDiff = filmVal - planVal;
                        double dDiffPercent;
                        if (isGlobal)
                        {
                            dDiffPercent = (doseDiff / maxPlan) * 100.0;
                        }
                        else
                        {
                            double localNorm = planVal > 0.001 * maxPlan ? planVal : 0.001 * maxPlan;
                            dDiffPercent = (doseDiff / localNorm) * 100.0;
                        }
                        
                        double effectiveDiff = Math.Max(0, Math.Abs(dDiffPercent) - uncertainty_percent);
                        double gammaSq = (effectiveDiff / dd_percent) * (effectiveDiff / dd_percent) + gammaDistSq;
                        if (gammaSq < minGammaSq) minGammaSq = gammaSq;
                    }
                    gammaMap[fy, fx] = Math.Sqrt(minGammaSq);
                }
                System.Threading.Interlocked.Increment(ref completedRows);
                if (fy % 10 == 0) progress?.Report((double)completedRows / fh * 100);
            });

            _gammaMap = gammaMap;
            progress?.Report(100);
        }

        private double GetBilinearPlanDose(double px, double py)
        {
            int ph = _planDose.GetLength(0);
            int pw = _planDose.GetLength(1);

            int x0 = (int)Math.Floor(px);
            int x1 = x0 + 1;
            int y0 = (int)Math.Floor(py);
            int y1 = y0 + 1;

            x0 = Math.Clamp(x0, 0, pw - 1);
            x1 = Math.Clamp(x1, 0, pw - 1);
            y0 = Math.Clamp(y0, 0, ph - 1);
            y1 = Math.Clamp(y1, 0, ph - 1);

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

        private double GetBicubicPlanDose(double px, double py)
        {
            int ph = _planDose.GetLength(0);
            int pw = _planDose.GetLength(1);
            return ImageFilters.BicubicSample(_planDose, py, px, ph, pw);
        }

        private static double[,] ApplyGaussianSmoothing(double[,] data, double sigmaMm, double dpi)
        {
            double pixelSigma = sigmaMm * (dpi / 25.4);
            if (pixelSigma < 0.1) return data;
            return ImageFilters.GaussianFilter2D(data, pixelSigma);
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
                        // Set to transparent
                        pixels[idx] = 0; pixels[idx + 1] = 0; pixels[idx + 2] = 0; pixels[idx + 3] = 0;
                        continue;
                    }

                    totalPoints++;
                    if (g <= 1.0) passPoints++;

                    // Color: Jet Mapping (0.0 to 1.5)
                    double v = Math.Clamp(g / 1.5, 0, 1);
                    var (R, G, B) = ColorMaps.GetJetColor(v);
                    pixels[idx] = B; 
                    pixels[idx + 1] = G; 
                    pixels[idx + 2] = R; 
                    pixels[idx + 3] = 255;
                }
            }

            GammaImage.Source = BitmapSource.Create(fw, fh, _filmDpiX, _filmDpiY, PixelFormats.Bgra32, null, pixels, stride);
            GammaEmptyMsg.Visibility = Visibility.Collapsed;

            double passRate = totalPoints > 0 ? (double)passPoints / totalPoints * 100.0 : 0.0;
            _lastPassRate = passRate;

            OverlayPassRateText.Text = $"{passRate:F1} %";
            OverlayPassRateText.Foreground = passRate >= 90 ? Brushes.Lime : Brushes.Red;
            OverlayStatusText.Text = passRate >= 90 ? "PASS (Criteria: 90%)" : "FAIL (Criteria: 90%)";
            GammaResultsOverlay.Visibility = Visibility.Visible;
            
            // Sync with sidebar results panel if it still exists (it might be hidden but let's keep it safe)
            GammaResultsOverlay.Visibility = Visibility.Visible;
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
            if (XShiftInput == null || YShiftInput == null || MeasuredImageTranslation == null || _filmDose == null) return;
            
            // Image translation is now handled by the Rulers (Coordinate shifting)
            // so we set the physical translation of the control to zero
            MeasuredImageTranslation.X = 0;
            MeasuredImageTranslation.Y = 0;

            UpdateRulers();
            UpdateProfiles();
            UpdateProfileCrosshairs();
        }

        #region Spatial Rulers Logic

        private void UpdateRulers()
        {
            if (!IsLoaded) return;
            UpdateMeasuredRulers();
            UpdatePlannedRulers();
        }

        private void UpdateMeasuredRulers()
        {
            if (MeasuredImage.Source == null || _filmDose == null || MeasuredTopRuler == null) return;

            Rect bounds = GetRenderedImageBounds(MeasuredImage, MeasuredCanvas);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            double mm_per_pixel_X = 25.4 / _filmDpiX;
            double mm_per_pixel_Y = 25.4 / _filmDpiY;
            double display_pixels_per_mm_X = bounds.Width / (_filmDose.GetLength(1) * mm_per_pixel_X);
            double display_pixels_per_mm_Y = bounds.Height / (_filmDose.GetLength(0) * mm_per_pixel_Y);

            double shiftX = XShiftInput.Value ?? 0.0;
            double shiftY = YShiftInput.Value ?? 0.0;

            // Visual Center of the Canvas
            double centerX = MeasuredCanvas.ActualWidth / 2.0;
            double centerY = MeasuredCanvas.ActualHeight / 2.0;

            DrawRuler(MeasuredTopRuler, MeasuredTopRuler.ActualWidth, 25, true, centerX, display_pixels_per_mm_X, shiftX);
            DrawRuler(MeasuredLeftRuler, 35, MeasuredLeftRuler.ActualHeight, false, centerY, display_pixels_per_mm_Y, shiftY);
        }

        private void UpdatePlannedRulers()
        {
            if (PlannedImage.Source == null || _planDose == null || PlannedTopRuler == null) return;

            Rect bounds = GetRenderedImageBounds(PlannedImage, PlannedCanvas);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            double mm_per_pixel_X = 25.4 / _planDpiX;
            double mm_per_pixel_Y = 25.4 / _planDpiY;
            double display_pixels_per_mm_X = bounds.Width / (_planDose.GetLength(1) * mm_per_pixel_X);
            double display_pixels_per_mm_Y = bounds.Height / (_planDose.GetLength(0) * mm_per_pixel_Y);

            // Visual Center
            double centerX = PlannedCanvas.ActualWidth / 2.0;
            double centerY = PlannedCanvas.ActualHeight / 2.0;

            DrawRuler(PlannedTopRuler, PlannedTopRuler.ActualWidth, 25, true, centerX, display_pixels_per_mm_X, 0);
            DrawRuler(PlannedLeftRuler, 35, PlannedLeftRuler.ActualHeight, false, centerY, display_pixels_per_mm_Y, 0);
        }

        private void DrawRuler(Canvas canvas, double width, double height, bool isTop, double centerPx, double pixelsPerMm, double shiftMm)
        {
            if (width <= 0 || height <= 0) return;

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                var rulerBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
                var majorBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 85, 85));
                var originBrush = Brushes.DarkOrange;
                var majorPen = new Pen(majorBrush, 1.0);
                var minorPen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 160, 160)), 0.6);
                var originPen = new Pen(originBrush, 2.0);
                rulerBrush.Freeze(); majorBrush.Freeze(); originBrush.Freeze(); majorPen.Freeze(); minorPen.Freeze(); originPen.Freeze();

                double majorStep = GetNiceStep(pixelsPerMm, 50);
                double minorStep = GetNiceStep(pixelsPerMm, 8);
                if (minorStep >= majorStep) minorStep = majorStep / 5.0;

                if (isTop)
                {
                    dc.DrawLine(new Pen(rulerBrush, 1), new System.Windows.Point(0, height - 1), new System.Windows.Point(width, height - 1));
                    
                    for (double mm = -1000; mm <= 1000; mm += minorStep)
                    {
                        double x = centerPx + (mm + shiftMm) * pixelsPerMm;
                        if (x < -10 || x > width + 10) continue;

                        bool isZero = Math.Abs(mm) < 0.001;
                        bool isMajor = Math.Abs(mm % majorStep) < 0.001 || Math.Abs(mm % majorStep - majorStep) < 0.001;
                        bool isMid = (majorStep > 5) && (Math.Abs(mm % (majorStep / 2)) < 0.001);
                        
                        double y1 = isMajor ? 0 : (isMid ? 10 : 18);
                        dc.DrawLine(isZero ? originPen : (isMajor ? majorPen : minorPen), new System.Windows.Point(x, y1), new System.Windows.Point(x, height));

                        if (isMajor)
                        {
                            var ft = new FormattedText(mm.ToString("0"), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), 10, isZero ? originBrush : majorBrush, 1.0);
                            dc.DrawText(ft, new System.Windows.Point(x + 3, 2));
                        }
                    }
                }
                else
                {
                    dc.DrawLine(new Pen(rulerBrush, 1), new System.Windows.Point(width - 1, 0), new System.Windows.Point(width - 1, height));
                    for (double mm = -1000; mm <= 1000; mm += minorStep)
                    {
                        double y = centerPx + (mm + shiftMm) * pixelsPerMm;
                        if (y < -10 || y > height + 10) continue;

                        bool isZero = Math.Abs(mm) < 0.001;
                        bool isMajor = Math.Abs(mm % majorStep) < 0.001 || Math.Abs(mm % majorStep - majorStep) < 0.001;
                        bool isMid = (majorStep > 5) && (Math.Abs(mm % (majorStep / 2)) < 0.001);

                        double x1 = isMajor ? 0 : (isMid ? 15 : 25);
                        dc.DrawLine(isZero ? originPen : (isMajor ? majorPen : minorPen), new System.Windows.Point(x1, y), new System.Windows.Point(width, y));

                        if (isMajor)
                        {
                            var ft = new FormattedText(mm.ToString("0"), System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), 10, isZero ? originBrush : majorBrush, 1.0);
                            dc.PushTransform(new RotateTransform(-90, 10, y));
                            dc.DrawText(ft, new System.Windows.Point(10, y - 10));
                            dc.Pop();
                        }
                    }
                }
            }

            canvas.Children.Clear();
            canvas.Children.Add(new VisualHost(visual));
        }

        private Rect GetRenderedImageBounds(System.Windows.Controls.Image img, Canvas container)
        {
            if (img.Source == null) return new Rect(0, 0, 0, 0);

            double controlWidth = container.ActualWidth;
            double controlHeight = container.ActualHeight;
            if (controlWidth == 0 || controlHeight == 0) return new Rect(0, 0, 0, 0);

            double imageWidth = img.Source.Width;
            double imageHeight = img.Source.Height;

            double ratioX = controlWidth / imageWidth;
            double ratioY = controlHeight / imageHeight;
            double ratio = Math.Min(ratioX, ratioY);

            double renderedWidth = imageWidth * ratio;
            double renderedHeight = imageHeight * ratio;

            double left = (controlWidth - renderedWidth) / 2.0;
            double top = (controlHeight - renderedHeight) / 2.0;

            return new Rect(left, top, renderedWidth, renderedHeight);
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

        // Helper class to host the drawing visual
        private class VisualHost : FrameworkElement
        {
            private readonly Visual _visual;
            public VisualHost(Visual visual) { _visual = visual; }
            protected override int VisualChildrenCount => 1;
            protected override Visual GetVisualChild(int index) => _visual;
        }

        #endregion 
        private void GammaParams_Changed(object sender, RoutedEventArgs e) { /* Auto-run optional */ }
        private void ProfileAxisCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateProfiles();
        private void PickProfilePoint_Click(object sender, RoutedEventArgs e)
        {
            _profilePickMode = true;
            Cursor = Cursors.Cross;
            PickProfileButton.Content = "Click map...";
        }

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
            Cursor = Cursors.Arrow;
        }

        private void RoiCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_profilePickMode)
            {
                var canvas = sender as Canvas;
                if (canvas == null) return;
                HandleProfilePick(canvas, e.GetPosition(canvas));
                return;
            }

            if (!_roiSelectMode)
            {
                // Check if user is trying to drag the profile crosshair
                if (_hasProfilePoint)
                {
                    var canvas = sender as Canvas;
                    if (canvas == null) return;
                    
                    var pos = e.GetPosition(canvas);
                    var (fx, fy) = GetFractionalFromCanvas(canvas, pos, 1, 1); // just for relative coords
                    
                    // Convert current profile LPS to canvas pixels for this canvas
                    var curFrac = canvas == MeasuredCanvas ? LpsToFilmFrac(_profileLpsX, _profileLpsY) : LpsToPlanFrac(_profileLpsX, _profileLpsY);
                    
                    // Actually checking distance in canvas pixels is better
                    double cw = canvas.ActualWidth, ch = canvas.ActualHeight;
                    int imgCols = canvas == MeasuredCanvas ? _filmDose.GetLength(1) : _planDose.GetLength(1);
                    int imgRows = canvas == MeasuredCanvas ? _filmDose.GetLength(0) : _planDose.GetLength(0);
                    
                    double imgAspect = (double)imgCols / imgRows;
                    double canAspect = cw / ch;
                    double rw, rh, ox, oy;
                    if (imgAspect > canAspect) { rw = cw; rh = cw / imgAspect; ox = 0; oy = (ch - rh) / 2; }
                    else { rh = ch; rw = ch * imgAspect; ox = (cw - rw) / 2; oy = 0; }
                    
                    double curX = ox + curFrac.X * rw;
                    double curY = oy + curFrac.Y * rh;
                    
                    double dist = Math.Sqrt(Math.Pow(pos.X - curX, 2) + Math.Pow(pos.Y - curY, 2));
                    if (dist < 25) // 25 pixel radius for dragging
                    {
                        _profileDragging = true;
                        canvas.CaptureMouse();
                        return;
                    }
                }
                return;
            }
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
            var canvas = sender as Canvas;
            if (canvas == null) return;

            if (_profilePickMode)
            {
                var pos = e.GetPosition(canvas);
                bool isMeasured = canvas == MeasuredCanvas;
                var dose = isMeasured ? _filmDose : _planDose;
                if (dose == null) return;

                int imgCols = dose.GetLength(1);
                int imgRows = dose.GetLength(0);
                (double fracX, double fracY) = GetFractionalFromCanvas(canvas, pos, imgCols, imgRows);

                Point lps = isMeasured ? FilmFracToLps(fracX, fracY) : PlanFracToLps(fracX, fracY);
                _profileLpsX = lps.X;
                _profileLpsY = lps.Y;
                _hasProfilePoint = true;
                UpdateProfileCrosshairs();
                return;
            }

            if (_profileDragging)
            {
                var pos = e.GetPosition(canvas);
                bool isMeasured = canvas == MeasuredCanvas;
                var dose = isMeasured ? _filmDose : _planDose;
                if (dose == null) return;

                int imgCols = dose.GetLength(1);
                int imgRows = dose.GetLength(0);
                (double fracX, double fracY) = GetFractionalFromCanvas(canvas, pos, imgCols, imgRows);

                Point lps = isMeasured ? FilmFracToLps(fracX, fracY) : PlanFracToLps(fracX, fracY);
                _profileLpsX = lps.X;
                _profileLpsY = lps.Y;

                UpdateProfileCrosshairs();
                UpdateProfiles();
                return;
            }

            if (!_roiDragging || _roiActiveCanvas == null) return;
            var posR = e.GetPosition(_roiActiveCanvas);

            var rect = _roiActiveCanvas == MeasuredCanvas ? MeasuredRoiRect : PlannedRoiRect;
            double x = Math.Min(_roiStart.X, posR.X);
            double y = Math.Min(_roiStart.Y, posR.Y);
            double w = Math.Abs(posR.X - _roiStart.X);
            double h = Math.Abs(posR.Y - _roiStart.Y);

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = w;
            rect.Height = h;
        }

        private void RoiCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_profileDragging)
            {
                _profileDragging = false;
                (sender as Canvas)?.ReleaseMouseCapture();
                return;
            }

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
        }

        private void HandleProfilePick(Canvas canvas, Point pos)
        {
            if ((_filmDose == null && canvas == MeasuredCanvas) || (_planDose == null && canvas == PlannedCanvas))
                return;

            int imgCols = canvas == MeasuredCanvas ? _filmDose.GetLength(1) : _planDose.GetLength(1);
            int imgRows = canvas == MeasuredCanvas ? _filmDose.GetLength(0) : _planDose.GetLength(0);
            (double fracX, double fracY) = GetFractionalFromCanvas(canvas, pos, imgCols, imgRows);

            Point lps = canvas == MeasuredCanvas ? FilmFracToLps(fracX, fracY) : PlanFracToLps(fracX, fracY);
            _profileLpsX = lps.X;
            _profileLpsY = lps.Y;
            _hasProfilePoint = true;
            _profilePickMode = false;
            Cursor = Cursors.Arrow;
            PickProfileButton.Content = "Pick Profile Point";

            UpdateProfileCrosshairs();
            UpdateProfiles();
        }

        private (double fracX, double fracY) GetFractionalFromCanvas(Canvas canvas, Point pos, int imgCols, int imgRows)
        {
            double cw = canvas.ActualWidth, ch = canvas.ActualHeight;
            double imgAspect = (double)imgCols / imgRows;
            double canAspect = cw / ch;
            double rw, rh, ox, oy;
            if (imgAspect > canAspect) { rw = cw; rh = cw / imgAspect; ox = 0; oy = (ch - rh) / 2; }
            else { rh = ch; rw = ch * imgAspect; ox = (cw - rw) / 2; oy = 0; }

            double fracX = Math.Clamp((pos.X - ox) / rw, 0, 1);
            double fracY = Math.Clamp((pos.Y - oy) / rh, 0, 1);
            return (fracX, fracY);
        }

        private void UpdateProfileCrosshairs()
        {
            if (!_hasProfilePoint) { MeasuredProfileLineH.Visibility = MeasuredProfileLineV.Visibility = Visibility.Collapsed; PlannedProfileLineH.Visibility = PlannedProfileLineV.Visibility = Visibility.Collapsed; return; }

            if (_filmDose != null)
            {
                var frac = LpsToFilmFrac(_profileLpsX, _profileLpsY);
                DrawCrosshair(MeasuredCanvas, frac.X, frac.Y, MeasuredProfileLineH, MeasuredProfileLineV, _filmDose.GetLength(1), _filmDose.GetLength(0));
            }
            if (_planDose != null)
            {
                var frac = LpsToPlanFrac(_profileLpsX, _profileLpsY);
                DrawCrosshair(PlannedCanvas, frac.X, frac.Y, PlannedProfileLineH, PlannedProfileLineV, _planDose.GetLength(1), _planDose.GetLength(0));
            }
        }

        private void DrawCrosshair(Canvas canvas, double fracX, double fracY, System.Windows.Shapes.Line hLine, System.Windows.Shapes.Line vLine, int imgCols, int imgRows)
        {
            double cw = canvas.ActualWidth, ch = canvas.ActualHeight;
            double imgAspect = (double)imgCols / imgRows;
            double canAspect = cw / ch;
            double rw, rh, ox, oy;
            if (imgAspect > canAspect) { rw = cw; rh = cw / imgAspect; ox = 0; oy = (ch - rh) / 2; }
            else { rh = ch; rw = ch * imgAspect; ox = (cw - rw) / 2; oy = 0; }

            double x = ox + Math.Clamp(fracX, 0, 1) * rw;
            double y = oy + Math.Clamp(fracY, 0, 1) * rh;

            hLine.X1 = ox; hLine.X2 = ox + rw; hLine.Y1 = hLine.Y2 = y;
            vLine.Y1 = oy; vLine.Y2 = oy + rh; vLine.X1 = vLine.X2 = x;
            hLine.Visibility = vLine.Visibility = Visibility.Visible;
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

        // ===== Reporting Helpers =====

        public ReportSnapshot GetReportSnapshot()
        {
            if (_filmDose == null || _planDose == null || _gammaMap == null || double.IsNaN(_lastPassRate))
                throw new InvalidOperationException("Load film & plan doses and run gamma analysis before printing.");

            // Ensure layout is up to date before rendering visuals
            UpdateLayout();

            return new ReportSnapshot
            {
                FilmFileName = _filmFileName,
                PlanFileName = _dicomFileName,
                PassRate = _lastPassRate,
                DtaMm = GammaDtaInput.Value ?? 0,
                DdPercent = GammaDdInput.Value ?? 0,
                DoseScale = DoseScaleInput.Value ?? 1.0,
                Mode = GammaModeCombo.SelectedIndex == 0 ? "Global" : "Local",
                ShiftX = XShiftInput.Value ?? 0,
                ShiftY = YShiftInput.Value ?? 0,
                FilmImage = CaptureElement(MeasuredCanvas),
                PlanImage = CaptureElement(PlannedCanvas),
                GammaImage = CaptureElement(GammaImage),
                ProfileImage = CaptureElement(ProfilePlot)
            };
        }

        private static BitmapSource CaptureElement(FrameworkElement element, double scale = 1.0)
        {
            element.UpdateLayout();
            var bounds = VisualTreeHelper.GetDescendantBounds(element);
            if (bounds.IsEmpty || bounds.Width < 1 || bounds.Height < 1)
                throw new InvalidOperationException($"Element '{element.Name}' has no renderable size.");

            int pixelWidth = (int)Math.Ceiling(bounds.Width * scale);
            int pixelHeight = (int)Math.Ceiling(bounds.Height * scale);

            var rtb = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }
    }

    public class ReportSnapshot
    {
        public string FilmFileName { get; set; } = "Film";
        public string PlanFileName { get; set; } = "Plan";
        public double PassRate { get; set; }
        public double DtaMm { get; set; }
        public double DdPercent { get; set; }
        public double ThresholdPercent { get; set; }
        public double DoseScale { get; set; }
        public string Mode { get; set; } = "Global";
        public double ShiftX { get; set; }
        public double ShiftY { get; set; }
        public required BitmapSource FilmImage { get; set; }
        public required BitmapSource PlanImage { get; set; }
        public required BitmapSource GammaImage { get; set; }
        public required BitmapSource ProfileImage { get; set; }
    }
}
