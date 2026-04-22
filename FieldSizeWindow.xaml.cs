using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Colors = ScottPlot.Colors;
using Wpf.Ui.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Printing;

namespace OpticalDose
{
    public partial class FieldSizeWindow : FluentWindow
    {
        private double[,] _doseMap;
        private double _dpi;
        private double _mmPerPixelX;
        private double _mmPerPixelY;
        private AppSettings _settings;

        // Cached results for reporting
        private double _lastFwhmX, _lastFwhmY, _lastStdX, _lastStdY, _lastCoverage;
        private double _lastUncertaintyX, _lastUncertaintyY, _lastTolerance, _lastDeltaX, _lastDeltaY;
        private bool _lastPassX, _lastPassY;
        private string _lastMethod = "Maximum";
        private double _lastPlateauX, _lastPlateauY;

        // Alignment Dragging State
        private Point? _customCenterPixel = null;
        private double _customRotationAngle = 0.0;
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _rectStartLeft;
        private double _rectStartTop;
        private string _analysisImageName = "Main window image";
        private bool _hasAnalysisFilm;
        private readonly Func<(double[,]? Map, double Dpi, BitmapSource? Image, string Name)>? _syncFilmProvider;

        public FieldSizeWindow(
            double[,]? doseMap,
            double dpi,
            AppSettings settings,
            BitmapSource? imageSource = null,
            Func<(double[,]? Map, double Dpi, BitmapSource? Image, string Name)>? syncFilmProvider = null)
        {
            InitializeComponent();
            _hasAnalysisFilm = doseMap != null;
            _syncFilmProvider = syncFilmProvider;
            _doseMap = doseMap ?? new double[100, 100]; // Placeholder until user loads a film
            _dpi = dpi <= 0 ? 72.0 : dpi;
            _settings = settings;
            _mmPerPixelX = 25.4 / _dpi;
            _mmPerPixelY = 25.4 / _dpi;

            if (imageSource != null)
            {
                FilmImage.Source = imageSource;
                FilmImage.Width = _doseMap.GetLength(1);
                FilmImage.Height = _doseMap.GetLength(0);
                _originalImageSource = imageSource;
            }

            // Load last settings
            PlateauXBox.Text = _settings.LastPlateauX.ToString();
            PlateauYBox.Text = _settings.LastPlateauY.ToString();
            foreach (ComboBoxItem item in MethodBox.Items)
            {
                if (item.Content.ToString() == _settings.LastJawMethod)
                {
                    MethodBox.SelectedItem = item;
                    break;
                }
            }
            StatusText.Text = "Ready. Load an analysis film or use the image supplied by the main window.";
            if (ImageStatusText != null)
                RefreshAnalysisImageStatus();
            UpdateKnownDosePreview();
            
            // Auto click apply reticle to start immediately if default 50x50 is ok
            ApplyReticleSize_Click(null, null);
            if (WorkflowTabs != null) WorkflowTabs.SelectedIndex = 1;
        }

        private void UpdateOverlayVisualScale()
        {
            int width = Math.Max(1, GetDisplayedImageWidth());
            double stroke = Math.Clamp(width / 360.0, 2.5, 18.0);
            double centerSize = Math.Clamp(width / 80.0, 18.0, 120.0);

            TargetRect.StrokeThickness = stroke;
            TargetCenter.Width = centerSize;
            TargetCenter.Height = centerSize;
            TargetCenter.StrokeThickness = Math.Max(stroke * 0.45, 2.0);
            TargetCenter.Margin = new Thickness(-centerSize / 2.0, -centerSize / 2.0, 0, 0);
        }

        private void RefreshAnalysisImageStatus()
        {
            if (ImageStatusText == null) return;

            if (!_hasAnalysisFilm)
            {
                ImageStatusText.Text = "No analysis film synced yet. Load a film or sync the current main-window film.";
                return;
            }

            string cal = _quickCalCoeffs == null ? "No quick calibration curve active." : "Quick calibration curve active.";
            ImageStatusText.Text = $"Using {_analysisImageName} ({_doseMap.GetLength(1)}x{_doseMap.GetLength(0)}, {_dpi:F1} DPI). {cal}";
        }

        private bool TrySyncAnalysisFilmFromMain(bool showErrors)
        {
            if (_syncFilmProvider == null)
            {
                if (showErrors)
                {
                    System.Windows.MessageBox.Show(
                        "This Field Size window was opened without a main-window sync source. Load an analysis film instead.",
                        "Sync Main Film",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                return false;
            }

            var film = _syncFilmProvider();
            if (film.Map == null)
            {
                if (showErrors)
                {
                    System.Windows.MessageBox.Show(
                        "No film or dose map is currently available in the main window.",
                        "Sync Main Film",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
                return false;
            }

            SetAnalysisFilm(film.Map, film.Dpi, film.Image, film.Name);
            return true;
        }

        private void SetAnalysisFilm(double[,] map, double dpi, BitmapSource? imageSource, string name)
        {
            _doseMap = map;
            _dpi = dpi <= 0 ? 72.0 : dpi;
            _mmPerPixelX = 25.4 / _dpi;
            _mmPerPixelY = 25.4 / _dpi;
            _activeExtractMap = null;
            _originalImageSource = imageSource;
            _hasAnalysisFilm = true;

            FilmImage.Source = imageSource;
            FilmImage.Width = _doseMap.GetLength(1);
            FilmImage.Height = _doseMap.GetLength(0);

            _analysisImageName = string.IsNullOrWhiteSpace(name) ? "Main window image" : name;
            ResetTargetForCurrentImage();
            RefreshAnalysisImageStatus();

            string calStatus = _quickCalCoeffs != null ? " Cal curve ready." : "";
            if (StatusText != null)
                StatusText.Text = $"Synced {_analysisImageName} ({_doseMap.GetLength(1)}x{_doseMap.GetLength(0)}).{calStatus} Align the target and run field size analysis.";
        }

        private void ResetTargetForCurrentImage()
        {
            TargetRect.Visibility = Visibility.Hidden;
            TargetCenter.Visibility = Visibility.Hidden;
            TargetRotation.Angle = 0;
            _customRotationAngle = 0.0;
            _customCenterPixel = null;
            ApplyReticleSize_Click(null, null);
        }

        private int GetDisplayedImageWidth()
        {
            return FilmImage != null && FilmImage.Width > 0
                ? (int)Math.Round(FilmImage.Width)
                : _doseMap.GetLength(1);
        }

        private int GetDisplayedImageHeight()
        {
            return FilmImage != null && FilmImage.Height > 0
                ? (int)Math.Round(FilmImage.Height)
                : _doseMap.GetLength(0);
        }

        private void ApplyReticleSize_Click(object? sender, RoutedEventArgs? e)
        {
            if (!double.TryParse(TargetSizeXBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double tX)) tX = 50;
            if (!double.TryParse(TargetSizeYBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double tY)) tY = 50;

            double pxWidth = tX / _mmPerPixelX;
            double pxHeight = tY / _mmPerPixelY;

            bool wasVisible = TargetRect.Visibility == Visibility.Visible;
            double cx = 0, cy = 0;

            if (wasVisible)
            {
                double currentLeft = Canvas.GetLeft(TargetRect);
                double currentTop = Canvas.GetTop(TargetRect);
                if (double.IsNaN(currentLeft)) currentLeft = 0;
                if (double.IsNaN(currentTop)) currentTop = 0;
                cx = currentLeft + TargetRect.Width / 2.0;
                cy = currentTop + TargetRect.Height / 2.0;
            }

            TargetRect.Width = pxWidth;
            TargetRect.Height = pxHeight;
            UpdateOverlayVisualScale();

            int w = GetDisplayedImageWidth();
            int h = GetDisplayedImageHeight();

            if (!wasVisible)
            {
                TargetRect.Visibility = Visibility.Visible;
                TargetCenter.Visibility = Visibility.Visible;
                Canvas.SetLeft(TargetRect, (w - pxWidth) / 2.0);
                Canvas.SetTop(TargetRect, (h - pxHeight) / 2.0);
            }
            else
            {
                Canvas.SetLeft(TargetRect, cx - pxWidth / 2.0);
                Canvas.SetTop(TargetRect, cy - pxHeight / 2.0);
            }

            UpdateCenterFromRect();
            if (StatusText != null) StatusText.Text = $"Target set to {tX:F1} x {tY:F1} mm. Drag it over the field, then run analysis.";
            OverlayCanvas.Focus();
        }

        private void CenterTarget_Click(object sender, RoutedEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;

            int w = GetDisplayedImageWidth();
            int h = GetDisplayedImageHeight();

            Canvas.SetLeft(TargetRect, (w - TargetRect.Width) / 2.0);
            Canvas.SetTop(TargetRect, (h - TargetRect.Height) / 2.0);

            TargetRotation.Angle = 0;
            _customRotationAngle = 0.0;

            UpdateCenterFromRect();
            if (StatusText != null) StatusText.Text = "Target centered to image boundaries.";
            OverlayCanvas.Focus();
        }

        private void DPadUp_Click(object sender, RoutedEventArgs e) => NudgeRect(0, -1);
        private void DPadDown_Click(object sender, RoutedEventArgs e) => NudgeRect(0, 1);
        private void DPadLeft_Click(object sender, RoutedEventArgs e) => NudgeRect(-1, 0);
        private void DPadRight_Click(object sender, RoutedEventArgs e) => NudgeRect(1, 0);

        private void DPadRotateCW_Click(object sender, RoutedEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;
            TargetRotation.Angle += 0.1;
            _customRotationAngle = TargetRotation.Angle;
        }

        private void DPadRotateCCW_Click(object sender, RoutedEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;
            TargetRotation.Angle -= 0.1;
            _customRotationAngle = TargetRotation.Angle;
        }

        private void NudgeRect(double dx, double dy)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;
            double left = Canvas.GetLeft(TargetRect);
            double top = Canvas.GetTop(TargetRect);
            if (double.IsNaN(left)) left = 0;
            if (double.IsNaN(top)) top = 0;
            Canvas.SetLeft(TargetRect, left + dx);
            Canvas.SetTop(TargetRect, top + dy);
            UpdateCenterFromRect();
        }

        private void UpdateCenterFromRect()
        {
            double left = Canvas.GetLeft(TargetRect);
            if (double.IsNaN(left)) left = 0;
            double top = Canvas.GetTop(TargetRect);
            if (double.IsNaN(top)) top = 0;

            double cx = left + TargetRect.Width / 2.0;
            double cy = top + TargetRect.Height / 2.0;

            _customCenterPixel = new Point(cx, cy);

            Canvas.SetLeft(TargetCenter, cx);
            Canvas.SetTop(TargetCenter, cy);
        }

        private void OverlayCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;
            
            Point pt = e.GetPosition(OverlayCanvas);
            double left = Canvas.GetLeft(TargetRect);
            double top = Canvas.GetTop(TargetRect);
            
            // simple bounding box hit test for the entire rectangle to make dragging easy
            if (pt.X >= left && pt.X <= left + TargetRect.Width &&
                pt.Y >= top && pt.Y <= top + TargetRect.Height)
            {
                _isDragging = true;
                _dragStartPoint = pt;
                _rectStartLeft = left;
                _rectStartTop = top;
                OverlayCanvas.CaptureMouse();
                OverlayCanvas.Focus();
                e.Handled = true;
            }
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPt = e.GetPosition(OverlayCanvas);
                double offsetX = currentPt.X - _dragStartPoint.X;
                double offsetY = currentPt.Y - _dragStartPoint.Y;

                Canvas.SetLeft(TargetRect, _rectStartLeft + offsetX);
                Canvas.SetTop(TargetRect, _rectStartTop + offsetY);

                UpdateCenterFromRect();
            }
        }

        private void OverlayCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                OverlayCanvas.ReleaseMouseCapture();
            }
        }

        private void OverlayCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;

            double left = Canvas.GetLeft(TargetRect);
            double top = Canvas.GetTop(TargetRect);

            double nudge = 1.0;
            bool handled = false;

            if (e.Key == Key.Up) { Canvas.SetTop(TargetRect, top - nudge); handled = true; }
            else if (e.Key == Key.Down) { Canvas.SetTop(TargetRect, top + nudge); handled = true; }
            else if (e.Key == Key.Left) { Canvas.SetLeft(TargetRect, left - nudge); handled = true; }
            else if (e.Key == Key.Right) { Canvas.SetLeft(TargetRect, left + nudge); handled = true; }

            if (handled)
            {
                UpdateCenterFromRect();
                e.Handled = true;
            }
        }

        private void CalcButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasAnalysisFilm && !TrySyncAnalysisFilmFromMain(false))
            {
                System.Windows.MessageBox.Show(
                    "Load an analysis film or sync the current main-window film before running field size analysis.",
                    "No Analysis Film",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(PlateauXBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double plateauX)) plateauX = 0;
            if (!double.TryParse(PlateauYBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double plateauY)) plateauY = 0;
            plateauX = Math.Max(0, plateauX);
            plateauY = Math.Max(0, plateauY);

            // Save settings for next time
            _settings.LastPlateauX = plateauX;
            _settings.LastPlateauY = plateauY;
            string selectedMethod = ((MethodBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Maximum");
            _settings.LastJawMethod = selectedMethod;

            string method = selectedMethod.ToLowerInvariant();

            try
            {
                UpdateKnownDosePreview();
                ComputeAndPlot(plateauX, plateauY, method);
                if (StatusText != null) StatusText.Text = "Calculated Center/Rot: " + (_customCenterPixel.HasValue ? $"({_customCenterPixel.Value.X:F0}, {_customCenterPixel.Value.Y:F0}) @ {_customRotationAngle:F1}°" : "Auto");
            }
            catch (Exception ex)
            {
                if (StatusText != null) StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private double? GetKnownFullDose()
        {
            if (KnownFullDoseBox != null &&
                double.TryParse(KnownFullDoseBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double fullDose) &&
                fullDose > 0)
            {
                return fullDose;
            }

            return _quickCalFullDose;
        }

        private void UpdateKnownDosePreview()
        {
            if (KnownHalfDoseText == null) return;

            double? fullDose = GetKnownFullDose();
            KnownHalfDoseText.Text = fullDose.HasValue
                ? (fullDose.Value / 2.0).ToString("F3", CultureInfo.InvariantCulture)
                : "---";
        }

        private void KnownFullDoseBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateKnownDosePreview();
        }

        private static double InterpolateBilinear(double[,] map, double x, double y)
        {
            int w = map.GetLength(1);
            int h = map.GetLength(0);

            if (x < 0 || x >= w - 1 || y < 0 || y >= h - 1)
                return 0; // Out of bounds returns 0 dose

            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            double dx = x - x0;
            double dy = y - y0;

            double c00 = map[y0, x0];
            double c10 = map[y0, x1];
            double c01 = map[y1, x0];
            double c11 = map[y1, x1];

            return c00 * (1 - dx) * (1 - dy) +
                   c10 * dx * (1 - dy) +
                   c01 * (1 - dx) * dy +
                   c11 * dx * dy;
        }

        private void ComputeAndPlot(double plateauX, double plateauY, string method)
        {
            int h = _doseMap.GetLength(0);
            int w = _doseMap.GetLength(1);
            double? knownFullDose = method == "known dose" ? GetKnownFullDose() : null;
            double? knownHalfDose = knownFullDose / 2.0;
            string methodLabel = FormatMethodLabel(method, knownFullDose);

            if (method == "known dose" && (!knownHalfDose.HasValue || knownHalfDose.Value <= 0))
                throw new InvalidOperationException("Enter a positive known full dose before using Known Dose analysis.");

            double pixelCenterX = _customCenterPixel.HasValue ? _customCenterPixel.Value.X : (w - 1) / 2.0;
            double pixelCenterY = _customCenterPixel.HasValue ? _customCenterPixel.Value.Y : (h - 1) / 2.0;

            double angleRad = _customRotationAngle * Math.PI / 180.0;
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            double stepX = _mmPerPixelX;
            double stepY = _mmPerPixelY;

            // Determine profile span from the TargetRect ROI + 20% margin for penumbra
            double roiWidthMm = TargetRect.Visibility == Visibility.Visible ? TargetRect.Width * stepX : w * stepX;
            double roiHeightMm = TargetRect.Visibility == Visibility.Visible ? TargetRect.Height * stepY : h * stepY;
            double marginFraction = 0.20;

            double xSpanMm = roiWidthMm * (1.0 + 2.0 * marginFraction);
            double ySpanMm = roiHeightMm * (1.0 + 2.0 * marginFraction);
            
            // X profiles span the ROI width + margin
            int numPointsX = Math.Max(10, (int)(xSpanMm / stepX) + 1);
            int numProfilesX = plateauY <= 0 ? 1 : Math.Max(1, (int)(plateauY / stepY) + 1);

            double[][] xProfiles = new double[numProfilesX][];
            var leftX = new System.Collections.Generic.List<double>();
            var rightX = new System.Collections.Generic.List<double>();
            var fwhmXList = new System.Collections.Generic.List<double>();
            var xEdgeSamples = new List<EdgeSample>();
            double[] xCoords = Enumerable.Range(0, numPointsX).Select(j => (j - (numPointsX - 1) / 2.0) * stepX).ToArray();

            for(int i = 0; i < numProfilesX; i++)
            {
                double yOffsetPhys = numProfilesX == 1 ? 0 : -plateauY / 2.0 + i * (plateauY / (numProfilesX - 1));
                var profile = new double[numPointsX];
                
                for(int j = 0; j < numPointsX; j++)
                {
                    double xOffsetPhys = xCoords[j];
                    double physX = xOffsetPhys * cosA - yOffsetPhys * sinA;
                    double physY = xOffsetPhys * sinA + yOffsetPhys * cosA;

                    double px = pixelCenterX + physX / stepX;
                    double py = pixelCenterY + physY / stepY;

                    profile[j] = PixelToDose(InterpolateBilinear(_doseMap, px, py));
                }
                xProfiles[i] = profile;

                var edges = AnalyzeFwhmProfile(profile, xCoords, plateauX, method, knownHalfDose);
                if (edges.IsValid)
                {
                    leftX.Add(edges.Left);
                    rightX.Add(edges.Right);
                    fwhmXList.Add(Math.Abs(edges.Right - edges.Left));
                    xEdgeSamples.Add(new EdgeSample(yOffsetPhys, edges.Left, edges.Right));
                }
            }

            // Y profiles span the ROI height + margin
            int numPointsY = Math.Max(10, (int)(ySpanMm / stepY) + 1);
            int numProfilesY = plateauX <= 0 ? 1 : Math.Max(1, (int)(plateauX / stepX) + 1);

            double[][] yProfiles = new double[numProfilesY][];
            var leftY = new System.Collections.Generic.List<double>();
            var rightY = new System.Collections.Generic.List<double>();
            var fwhmYList = new System.Collections.Generic.List<double>();
            var yEdgeSamples = new List<EdgeSample>();
            double[] yCoords = Enumerable.Range(0, numPointsY).Select(j => (j - (numPointsY - 1) / 2.0) * stepY).ToArray();

            for(int i = 0; i < numProfilesY; i++)
            {
                double xOffsetPhys = numProfilesY == 1 ? 0 : -plateauX / 2.0 + i * (plateauX / (numProfilesY - 1));
                var profile = new double[numPointsY];
                
                for(int j = 0; j < numPointsY; j++)
                {
                    double yOffsetPhys = yCoords[j];
                    double physX = xOffsetPhys * cosA - yOffsetPhys * sinA;
                    double physY = xOffsetPhys * sinA + yOffsetPhys * cosA;

                    double px = pixelCenterX + physX / stepX;
                    double py = pixelCenterY + physY / stepY;

                    profile[j] = PixelToDose(InterpolateBilinear(_doseMap, px, py));
                }
                yProfiles[i] = profile;

                var edges = AnalyzeFwhmProfile(profile, yCoords, plateauY, method, knownHalfDose);
                if (edges.IsValid)
                {
                    leftY.Add(edges.Left);
                    rightY.Add(edges.Right);
                    fwhmYList.Add(Math.Abs(edges.Right - edges.Left));
                    yEdgeSamples.Add(new EdgeSample(xOffsetPhys, edges.Left, edges.Right));
                }
            }

            // Stats
            var fwhmX = fwhmXList.ToArray();
            var fwhmY = fwhmYList.ToArray();
            double plotLeftX = leftX.Count > 0 ? leftX.Average() : 0;
            double plotRightX = rightX.Count > 0 ? rightX.Average() : 0;
            double plotLeftY = leftY.Count > 0 ? leftY.Average() : 0;
            double plotRightY = rightY.Count > 0 ? rightY.Average() : 0;

            if (method == "known dose")
            {
                var xFit = FitMultiProfileEdges(xEdgeSamples);
                if (xFit.IsValid)
                {
                    fwhmX = xFit.Widths;
                    plotLeftX = xFit.LeftAtCenter;
                    plotRightX = xFit.RightAtCenter;
                }

                var yFit = FitMultiProfileEdges(yEdgeSamples);
                if (yFit.IsValid)
                {
                    fwhmY = yFit.Widths;
                    plotLeftY = yFit.LeftAtCenter;
                    plotRightY = yFit.RightAtCenter;
                }
            }

            double meanX = fwhmX.Length > 0 ? Math.Round(RobustAverage(fwhmX), 3) : 0;
            double stdX = Math.Round(StdDev(fwhmX), 4);
            double meanY = fwhmY.Length > 0 ? Math.Round(RobustAverage(fwhmY), 3) : 0;
            double stdY = Math.Round(StdDev(fwhmY), 4);
            double uncX = Math.Round(EstimateFieldSizeUncertainty(fwhmX, stepX), 4);
            double uncY = Math.Round(EstimateFieldSizeUncertainty(fwhmY, stepY), 4);

            double targetX = TryReadTargetSize(TargetSizeXBox.Text, 0);
            double targetY = TryReadTargetSize(TargetSizeYBox.Text, 0);
            double tolerance = CalculateFieldTolerance(targetX, targetY);
            double deltaX = Math.Round(meanX - targetX, 3);
            double deltaY = Math.Round(meanY - targetY, 3);
            bool passX = Math.Abs(deltaX) + uncX <= tolerance;
            bool passY = Math.Abs(deltaY) + uncY <= tolerance;

            double coverage = Math.Round(meanX * meanY, 2);
            if (ResultText != null)
            {
                ResultText.Text = $"FWHM X = {meanX:F3} +/- {stdX:F4} mm\n" +
                                  $"FWHM Y = {meanY:F3} +/- {stdY:F4} mm\n" +
                                  $"Tolerance = +/- {tolerance:F2} mm\n" +
                                  $"Method = {methodLabel}";
            }

            _lastFwhmX = meanX; _lastStdX = stdX;
            _lastFwhmY = meanY; _lastStdY = stdY;
            _lastUncertaintyX = uncX; _lastUncertaintyY = uncY;
            _lastCoverage = coverage;
            _lastMethod = methodLabel;
            _lastPlateauX = plateauX; _lastPlateauY = plateauY;
            _lastTolerance = tolerance;
            _lastDeltaX = deltaX; _lastDeltaY = deltaY;
            _lastPassX = passX; _lastPassY = passY;

            if (leftX.Count > 0 && rightX.Count > 0)
                PlotProfiles(XPlot, xCoords, xProfiles, plotLeftX, plotRightX, methodLabel, "XX Profile", meanX, uncX, plateauX / 2.0, knownHalfDose);
            
            if (leftY.Count > 0 && rightY.Count > 0)
                PlotProfiles(YPlot, yCoords, yProfiles, plotLeftY, plotRightY, methodLabel, "YY Profile", meanY, uncY, plateauY / 2.0, knownHalfDose);

            // Update the image to a Jet colormap if calibration is active
            UpdateJetHeatmap();
        }

        private static double[] GetPlateauSlice(double[] profile, double[] coords, double plateauWidth, double center)
        {
            if (plateauWidth <= 0) return profile;
            var idx = Enumerable.Range(0, profile.Length)
                .Where(i => coords[i] >= center - plateauWidth / 2.0 && coords[i] <= center + plateauWidth / 2.0)
                .ToArray();
            return idx.Length == 0 ? profile : idx.Select(i => profile[i]).ToArray();
        }

        private readonly struct FwhmEdgeResult
        {
            public FwhmEdgeResult(double left, double right, bool isValid)
            {
                Left = left;
                Right = right;
                IsValid = isValid;
            }

            public double Left { get; }
            public double Right { get; }
            public bool IsValid { get; }
        }

        private readonly struct EdgeSample
        {
            public EdgeSample(double offset, double left, double right)
            {
                Offset = offset;
                Left = left;
                Right = right;
            }

            public double Offset { get; }
            public double Left { get; }
            public double Right { get; }
            public double Width => Math.Abs(Right - Left);
        }

        private readonly struct EdgeFitResult
        {
            public EdgeFitResult(double leftAtCenter, double rightAtCenter, double[] widths, bool isValid)
            {
                LeftAtCenter = leftAtCenter;
                RightAtCenter = rightAtCenter;
                Widths = widths;
                IsValid = isValid;
            }

            public double LeftAtCenter { get; }
            public double RightAtCenter { get; }
            public double[] Widths { get; }
            public bool IsValid { get; }
        }

        private static EdgeFitResult FitMultiProfileEdges(IReadOnlyList<EdgeSample> samples)
        {
            if (samples == null || samples.Count < 3)
                return new EdgeFitResult(0, 0, Array.Empty<double>(), false);

            var accepted = RejectWidthOutliers(samples);
            if (accepted.Count < 3)
                accepted = samples.ToList();

            var leftLine = FitLine(accepted.Select(s => s.Offset).ToArray(), accepted.Select(s => s.Left).ToArray());
            var rightLine = FitLine(accepted.Select(s => s.Offset).ToArray(), accepted.Select(s => s.Right).ToArray());
            if (!leftLine.IsValid || !rightLine.IsValid)
                return new EdgeFitResult(0, 0, Array.Empty<double>(), false);

            accepted = RejectLineResidualOutliers(accepted, leftLine.Slope, leftLine.Intercept, rightLine.Slope, rightLine.Intercept);
            if (accepted.Count >= 3)
            {
                leftLine = FitLine(accepted.Select(s => s.Offset).ToArray(), accepted.Select(s => s.Left).ToArray());
                rightLine = FitLine(accepted.Select(s => s.Offset).ToArray(), accepted.Select(s => s.Right).ToArray());
            }

            if (!leftLine.IsValid || !rightLine.IsValid)
                return new EdgeFitResult(0, 0, Array.Empty<double>(), false);

            double leftAtCenter = leftLine.Intercept;
            double rightAtCenter = rightLine.Intercept;
            var fittedWidths = accepted
                .Select(s => (rightLine.Slope * s.Offset + rightLine.Intercept) - (leftLine.Slope * s.Offset + leftLine.Intercept))
                .Where(w => double.IsFinite(w) && w > 0)
                .ToArray();

            return fittedWidths.Length > 0
                ? new EdgeFitResult(leftAtCenter, rightAtCenter, fittedWidths, true)
                : new EdgeFitResult(0, 0, Array.Empty<double>(), false);
        }

        private static List<EdgeSample> RejectWidthOutliers(IReadOnlyList<EdgeSample> samples)
        {
            var widths = samples.Select(s => s.Width).Where(double.IsFinite).ToArray();
            if (widths.Length < 5)
                return samples.ToList();

            double median = Median(widths);
            double mad = Median(widths.Select(w => Math.Abs(w - median)).ToArray());
            double robustSigma = 1.4826 * mad;
            double limit = Math.Max(0.25, robustSigma * 3.0);

            return samples
                .Where(s => double.IsFinite(s.Width) && Math.Abs(s.Width - median) <= limit)
                .ToList();
        }

        private static List<EdgeSample> RejectLineResidualOutliers(IReadOnlyList<EdgeSample> samples, double leftSlope, double leftIntercept, double rightSlope, double rightIntercept)
        {
            if (samples.Count < 5)
                return samples.ToList();

            var residuals = samples
                .Select(s =>
                {
                    double leftResidual = s.Left - (leftSlope * s.Offset + leftIntercept);
                    double rightResidual = s.Right - (rightSlope * s.Offset + rightIntercept);
                    return Math.Sqrt(leftResidual * leftResidual + rightResidual * rightResidual);
                })
                .ToArray();

            double median = Median(residuals);
            double mad = Median(residuals.Select(r => Math.Abs(r - median)).ToArray());
            double robustSigma = 1.4826 * mad;
            double limit = Math.Max(0.20, median + robustSigma * 3.0);

            return samples
                .Zip(residuals, (sample, residual) => new { sample, residual })
                .Where(x => x.residual <= limit)
                .Select(x => x.sample)
                .ToList();
        }

        private readonly struct LineFitResult
        {
            public LineFitResult(double slope, double intercept, bool isValid)
            {
                Slope = slope;
                Intercept = intercept;
                IsValid = isValid;
            }

            public double Slope { get; }
            public double Intercept { get; }
            public bool IsValid { get; }
        }

        private static LineFitResult FitLine(double[] x, double[] y)
        {
            if (x.Length != y.Length || x.Length < 2)
                return new LineFitResult(0, 0, false);

            double meanX = x.Average();
            double meanY = y.Average();
            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < x.Length; i++)
            {
                double dx = x[i] - meanX;
                numerator += dx * (y[i] - meanY);
                denominator += dx * dx;
            }

            double slope = denominator > 1e-12 ? numerator / denominator : 0;
            double intercept = meanY - slope * meanX;
            bool valid = double.IsFinite(slope) && double.IsFinite(intercept);
            return new LineFitResult(slope, intercept, valid);
        }

        private static double Median(double[] values)
        {
            if (values == null || values.Length == 0) return 0;
            var sorted = values.OrderBy(v => v).ToArray();
            int mid = sorted.Length / 2;
            return sorted.Length % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];
        }

        private static FwhmEdgeResult AnalyzeFwhmProfile(double[] profile, double[] coords, double plateauWidth, string method, double? knownHalfDose)
        {
            if (profile.Length < 4 || coords.Length != profile.Length)
                return new FwhmEdgeResult(0, 0, false);

            double[] smoothedProfile = GaussianSmooth1D(profile, 1.5);
            double[] plateauSlice = GetPlateauSlice(smoothedProfile, coords, plateauWidth, 0.0);
            double peak = SelectPeak(plateauSlice, method);

            if (!double.IsFinite(peak) || peak <= 1e-9)
                return new FwhmEdgeResult(0, 0, false);

            double threshold = knownHalfDose ?? peak / 2.0;
            if (!double.IsFinite(threshold) || threshold <= 0 || threshold >= peak)
                return new FwhmEdgeResult(0, 0, false);

            int maxIdx = FindPeakIndexNearCenter(smoothedProfile, coords, plateauWidth);

            int idxL = maxIdx;
            while (idxL > 0 && smoothedProfile[idxL - 1] >= threshold) idxL--;
            if (idxL <= 0) return new FwhmEdgeResult(0, 0, false);
            double left = FitLogisticEdge(smoothedProfile, coords, idxL - 1, idxL, threshold, peak);

            int idxR = maxIdx;
            while (idxR < smoothedProfile.Length - 1 && smoothedProfile[idxR + 1] >= threshold) idxR++;
            if (idxR >= smoothedProfile.Length - 1) return new FwhmEdgeResult(0, 0, false);
            double right = FitLogisticEdge(smoothedProfile, coords, idxR, idxR + 1, threshold, peak);

            bool valid = double.IsFinite(left) && double.IsFinite(right) && right > left;
            return new FwhmEdgeResult(left, right, valid);
        }

        private static int FindPeakIndexNearCenter(double[] profile, double[] coords, double plateauWidth)
        {
            var candidates = Enumerable.Range(0, profile.Length)
                .Where(i => plateauWidth <= 0 || Math.Abs(coords[i]) <= plateauWidth / 2.0)
                .ToArray();

            if (candidates.Length == 0)
                candidates = Enumerable.Range(0, profile.Length).ToArray();

            return candidates
                .OrderByDescending(i => profile[i])
                .ThenBy(i => Math.Abs(coords[i]))
                .First();
        }

        private static double EstimateProfileBaseline(double[] profile)
        {
            if (profile.Length == 0) return 0;
            int count = Math.Max(1, (int)Math.Ceiling(profile.Length * 0.10));
            return profile.OrderBy(v => v).Take(count).Average();
        }

        private static double EstimateFieldSizeUncertainty(double[] measurements, double pixelStepMm)
        {
            if (measurements.Length == 0) return 0;

            double repeatabilitySem = measurements.Length > 1
                ? StdDev(measurements) / Math.Sqrt(measurements.Length)
                : 0.0;

            double twoEdgeResolution = Math.Sqrt(2.0) * pixelStepMm / Math.Sqrt(12.0);
            return Math.Sqrt(repeatabilitySem * repeatabilitySem + twoEdgeResolution * twoEdgeResolution);
        }

        private static double RobustAverage(double[] measurements)
        {
            if (measurements.Length == 0) return 0;
            if (measurements.Length < 5) return measurements.Average();

            var sorted = measurements.OrderBy(v => v).ToArray();
            int trim = Math.Max(1, (int)Math.Floor(sorted.Length * 0.10));
            int count = sorted.Length - 2 * trim;
            if (count <= 0) return sorted[sorted.Length / 2];

            return sorted.Skip(trim).Take(count).Average();
        }

        private static double TryReadTargetSize(string text, double fallback)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? Math.Max(0, value)
                : fallback;
        }

        private static double CalculateFieldTolerance(double targetX, double targetY)
        {
            double narrowSide = Math.Min(targetX > 0 ? targetX : double.MaxValue, targetY > 0 ? targetY : double.MaxValue);
            if (double.IsInfinity(narrowSide) || narrowSide == double.MaxValue)
                return 0.20;

            return Math.Round(Math.Max(0.05, narrowSide / 100.0), 2);
        }

        private static double SelectPeak(double[] values, string method)
        {
            if (values == null || values.Length == 0) return 0;
            return method switch
            {
                "mean" => values.Average(),
                "median" => values.OrderBy(v => v).ElementAt(values.Length / 2),
                _ => values.Max()
            };
        }

        private static string FormatMethodLabel(string method, double? knownFullDose)
        {
            if (method == "known dose" && knownFullDose.HasValue)
                return $"Known Dose Edge Fit ({knownFullDose.Value:F3} full, {knownFullDose.Value / 2.0:F3} half)";

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(method);
        }

        private static double InterpolateEdge(double[] profile, double[] coords, int idx1, int idx2, double threshold)
        {
            idx1 = Math.Clamp(idx1, 0, profile.Length - 1);
            idx2 = Math.Clamp(idx2, 0, profile.Length - 1);
            double v1 = profile[idx1];
            double v2 = profile[idx2];
            double c1 = coords[idx1];
            double c2 = coords[idx2];
            if (Math.Abs(v2 - v1) < 1e-12) return c1;
            double frac = (threshold - v1) / (v2 - v1);
            return c1 + frac * (c2 - c1);
        }

        private static double FitLogisticEdge(double[] profile, double[] coords, int idx1, int idx2, double threshold, double peak)
        {
            return FitLogisticEdge(profile, coords, idx1, idx2, threshold, 0.0, peak);
        }

        private static double FitLogisticEdge(double[] profile, double[] coords, int idx1, int idx2, double threshold, double baseline, double peak)
        {
            double lowerBound = peak * 0.20;
            double upperBound = peak * 0.80;
            double signal = peak - baseline;
            if (signal > 0)
            {
                lowerBound = baseline + signal * 0.20;
                upperBound = baseline + signal * 0.80;
            }

            var xPoints = new System.Collections.Generic.List<double>();
            var yLogits = new System.Collections.Generic.List<double>();

            int searchStart = Math.Min(idx1, idx2);
            while (searchStart > 0 && profile[searchStart] > lowerBound && profile[searchStart] < upperBound) searchStart--;
            
            int searchEnd = Math.Max(idx1, idx2);
            while (searchEnd < profile.Length - 1 && profile[searchEnd] > lowerBound && profile[searchEnd] < upperBound) searchEnd++;

            searchStart = Math.Max(0, searchStart - 1);
            searchEnd = Math.Min(profile.Length - 1, searchEnd + 1);

            for (int i = searchStart; i <= searchEnd; i++)
            {
                double val = profile[i];
                if (val >= lowerBound && val <= upperBound)
                {
                    double ratio = signal > 0 ? (val - baseline) / signal : val / peak;
                    ratio = Math.Clamp(ratio, 0.01, 0.99);
                    double logit = Math.Log(ratio / (1.0 - ratio));
                    xPoints.Add(coords[i]);
                    yLogits.Add(logit);
                }
            }

            if (xPoints.Count < 3)
            {
                return InterpolateEdge(profile, coords, idx1, idx2, threshold);
            }

            try
            {
                double[] coeffs = FittingMath.PolyFit(xPoints.ToArray(), yLogits.ToArray(), 1);
                double a = coeffs[0];
                double b = coeffs[1];

                if (Math.Abs(a) < 1e-12) return InterpolateEdge(profile, coords, idx1, idx2, threshold);

                double targetRatio = signal > 0 ? (threshold - baseline) / signal : threshold / peak;
                if (targetRatio <= 0 || targetRatio >= 1 || !double.IsFinite(targetRatio))
                    return InterpolateEdge(profile, coords, idx1, idx2, threshold);

                double targetLogit = Math.Log(targetRatio / (1.0 - targetRatio));
                return (targetLogit - b) / a;
            }
            catch
            {
                return InterpolateEdge(profile, coords, idx1, idx2, threshold);
            }
        }

        private static double[] GaussianSmooth1D(double[] data, double sigma)
        {
            if (data == null || data.Length == 0) return Array.Empty<double>();
            if (sigma <= 0) return data;
            
            int radius = (int)Math.Ceiling(3 * sigma);
            int length = data.Length;
            double[] smoothed = new double[length];
            double[] kernel = new double[radius * 2 + 1];
            double sum = 0;
            
            for (int i = -radius; i <= radius; i++)
            {
                kernel[i + radius] = Math.Exp(-(i * i) / (2 * sigma * sigma));
                sum += kernel[i + radius];
            }
            for (int i = 0; i < kernel.Length; i++) kernel[i] /= sum;
            
            for (int i = 0; i < length; i++)
            {
                double val = 0;
                for (int j = -radius; j <= radius; j++)
                {
                    int idx = i + j;
                    if (idx < 0) idx = 0;
                    if (idx >= length) idx = length - 1;
                    val += data[idx] * kernel[j + radius];
                }
                smoothed[i] = val;
            }
            return smoothed;
        }

        private static double StdDev(double[] data)

        {
            if (data == null || data.Length == 0) return 0;
            double mean = data.Average();
            double variance = data.Select(v => (v - mean) * (v - mean)).Average();
            return Math.Sqrt(variance);
        }

        private static void PlotProfiles(WpfPlot plot, double[] coords, double[][] profiles, double leftEdge, double rightEdge, string method, string title, double mean, double std, double plateauHalfWidth, double? thresholdOverride)
        {
            var plt = plot.Plot;
            plt.Clear();

            if (profiles == null || profiles.Length == 0 || coords == null || coords.Length == 0)
            {
                plot.Refresh();
                return;
            }

            // Calculate mean profile across all slices
            double[] meanProfile = new double[coords.Length];
            int n = profiles.Length;
            for (int i = 0; i < coords.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < n; j++)
                {
                    if (profiles[j] != null && i < profiles[j].Length)
                        sum += profiles[j][i];
                }
                meanProfile[i] = sum / n;
            }

            // Add plateau shade (background)
            var span = plt.Add.HorizontalSpan(-plateauHalfWidth, plateauHalfWidth);
            // Use alpha value of 15 (out of 255) for ~6% opacity blue
            span.FillStyle.Color = new ScottPlot.Color(0, 0, 255, 15); 
            span.LineStyle.Width = 0;
            // Background profiles (Sampled context)
            if (profiles.Length > 1)
            {
                int numSamples = Math.Min(8, profiles.Length);
                for (int i = 0; i < numSamples; i++)
                {
                    int idx = (i * profiles.Length) / numSamples;
                    var bkgLine = plt.Add.ScatterLine(coords, profiles[idx]);
                    bkgLine.LineStyle.Color = ScottPlot.Color.FromHex("#30000000"); // 19% opacity Black (more visible)
                    bkgLine.LineStyle.Width = 1.0f;
                }
            }

            var sp = plt.Add.Scatter(coords, meanProfile);
            sp.LineStyle.Color = ScottPlot.Color.FromHex("#1f77b4"); // Modern Blue
            sp.MarkerStyle.Size = 0;
            sp.LineStyle.Width = 2.5f;
            sp.LegendText = "Mean Profile";

            // Add threshold and edge markers
            double maxVal = meanProfile.Any() ? meanProfile.Max() : 1.0;
            double threshold = thresholdOverride ?? maxVal / 2.0;

            var hLine = plt.Add.HorizontalLine(threshold);
            hLine.LineStyle.Color = ScottPlot.Colors.Black;
            hLine.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            hLine.LegendText = thresholdOverride.HasValue ? "Known half dose" : "50%";

            var vLineL = plt.Add.VerticalLine(leftEdge);
            vLineL.LineStyle.Color = ScottPlot.Colors.Red;
            vLineL.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            vLineL.LegendText = "Left";

            var vLineR = plt.Add.VerticalLine(rightEdge);
            vLineR.LineStyle.Color = ScottPlot.Colors.ForestGreen;
            vLineR.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            vLineR.LegendText = "Right";

            plt.Title($"{title} | FWHM = {mean:F3} +/- {std:F4} mm ({method})");
            plt.XLabel("Distance (mm)");
            plt.YLabel("Dose");
            plt.Axes.AutoScale();
            plt.ShowLegend(Alignment.LowerRight);
            plot.Refresh();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (double.IsNaN(_lastFwhmX) || _lastFwhmX == 0)
                    ComputeAndPlot(_settings.LastPlateauX, _settings.LastPlateauY, _settings.LastJawMethod.ToLowerInvariant());

                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40),
                    ColumnWidth = double.PositiveInfinity,
                    FontFamily = new FontFamily("Segoe UI")
                };

                var header = new Paragraph(new Run("FWHM Field Size Report"))
                {
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(173, 216, 230)), // light blue, matches film report
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 8)
                };
                doc.Blocks.Add(header);

                var info = new Table();
                info.Columns.Add(new TableColumn { Width = new GridLength(180) });
                info.Columns.Add(new TableColumn());
                info.RowGroups.Add(new TableRowGroup());
                void AddRow(string label, string value)
                {
                    var row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(label)) { FontWeight = FontWeights.Bold }));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(value))));
                    info.RowGroups[0].Rows.Add(row);
                }
                AddRow("Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                AddRow("Method", _lastMethod);
                AddRow("Plateau (mm)", $"X {_lastPlateauX:F2}, Y {_lastPlateauY:F2}");
                AddRow("Center (px)", _customCenterPixel.HasValue ? $"{_customCenterPixel.Value.X:F1}, {_customCenterPixel.Value.Y:F1} @ {_customRotationAngle:F1}°" : "Auto");
                AddRow("FWHM X (mm)", $"{_lastFwhmX:F3} ± {_lastStdX:F4}");
                AddRow("FWHM Y (mm)", $"{_lastFwhmY:F3} ± {_lastStdY:F4}");
                AddRow("Coverage (mm²)", $"{_lastCoverage:F2}");
                AddRow("Uncertainty X (mm)", $"{_lastUncertaintyX:F4}");
                AddRow("Uncertainty Y (mm)", $"{_lastUncertaintyY:F4}");
                AddRow("Delta X/Y (mm)", $"X {_lastDeltaX:+0.000;-0.000;0.000}, Y {_lastDeltaY:+0.000;-0.000;0.000}");
                AddRow("Tolerance (mm)", $"+/- {_lastTolerance:F2} ({(_lastPassX && _lastPassY ? "PASS" : "CHECK")})");
                doc.Blocks.Add(info);

                // Plots
                AddImage(doc, "X Profile", CapturePlot(XPlot));
                AddImage(doc, "Y Profile", CapturePlot(YPlot));

                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                {
                    doc.PageHeight = pd.PrintableAreaHeight;
                    doc.PageWidth = pd.PrintableAreaWidth;
                    pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Field size Report");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Unable to print jaw report: {ex.Message}", "Print Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private static void AddImage(FlowDocument doc, string title, BitmapSource bmp)
        {
            doc.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 14, 0, 6) });
            var img = new System.Windows.Controls.Image
            {
                Source = bmp,
                Width = 475,
                Stretch = Stretch.Uniform
            };
            doc.Blocks.Add(new BlockUIContainer(img) { Margin = new Thickness(0, 0, 0, 10) });
        }

        private static BitmapSource CapturePlot(WpfPlot plot)
        {
            plot.Refresh();
            var rtb = new RenderTargetBitmap((int)plot.ActualWidth, (int)plot.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(plot);
            rtb.Freeze();
            return rtb;
        }

        #region Quick 3-Point Calibration Logic
        
        private double[,]? _activeExtractMap;
        private BitmapSource? _originalImageSource;
        private double[]? _quickCalCoeffs; // Stored calibration curve coefficients (degree 2)
        private double? _quickCalFullDose;

        private void ExtractPoint1_Click(object sender, RoutedEventArgs e) => ExtractROI(QD0Box);
        private void ExtractPoint2_Click(object sender, RoutedEventArgs e) => ExtractROI(QD300Box);
        private void ExtractPoint3_Click(object sender, RoutedEventArgs e) => ExtractROI(QD600Box);

        private void ExtractROI(System.Windows.Controls.TextBox targetBox)
        {
            double[,] extractSource = _activeExtractMap ?? _doseMap;

            if (TargetRect.Visibility != Visibility.Visible)
            {
                System.Windows.MessageBox.Show("Please apply the Target Square over the patch you want to measure first.", "Setup Target");
                return;
            }
            
            int left = (int)Canvas.GetLeft(TargetRect);
            int top = (int)Canvas.GetTop(TargetRect);
            int width = (int)TargetRect.Width;
            int height = (int)TargetRect.Height;
            
            double avgOd = CalculateAverageOD(extractSource, left, top, width, height);
            targetBox.Text = avgOd.ToString("F6");
            
            if (StatusText != null) StatusText.Text = $"Extracted OD = {avgOd:F6} from ROI ({width}x{height} px)";
        }

        /// <summary>
        /// Computes the average Optical Density inside the specified rectangle.
        /// Raw 16-bit pixel values are converted to OD = -log10(pixel / 65535).
        /// </summary>
        private double CalculateAverageOD(double[,] array, int left, int top, int width, int height)
        {
            int arrH = array.GetLength(0);
            int arrW = array.GetLength(1);
            
            left = Math.Max(0, Math.Min(left, arrW - 1));
            top = Math.Max(0, Math.Min(top, arrH - 1));
            int right = Math.Min(left + width, arrW);
            int bottom = Math.Min(top + height, arrH);

            if (right <= left || bottom <= top) return 0;

            // Determine if data is raw 16-bit by sampling the center of the ROI
            // Raw 16-bit scanner values are typically in the range 1000-65535
            // OD values are typically 0.0 - 3.0
            int sampleY = (top + bottom) / 2;
            int sampleX = (left + right) / 2;
            bool isRaw = array[sampleY, sampleX] > 100; 
            
            double sum = 0;
            int count = 0;
            
            for (int y = top; y < bottom; y++)
            {
                for (int x = left; x < right; x++)
                {
                    double val = array[y, x];
                    if (isRaw)
                    {
                        val = Math.Clamp(val, 1, 65535);
                        sum += -Math.Log10(val / 65535.0);
                    }
                    else
                    {
                        sum += val; // Already OD
                    }
                    count++;
                }
            }
            return count > 0 ? sum / count : 0;
        }

        private void LoadCalScan_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image files|*.tif;*.tiff;*.png;*.jpg;*.bmp|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var bmp = new BitmapImage(new Uri(dlg.FileName));
                    int w = bmp.PixelWidth;
                    int h = bmp.PixelHeight;

                    double newDpiX = bmp.DpiX > 0 ? bmp.DpiX : 72.0;
                    double newDpiY = bmp.DpiY > 0 ? bmp.DpiY : 72.0;

                    // If it's a TIFF, prefer LibTiff for precise physical scan resolution
                    string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                    if (ext == ".tif" || ext == ".tiff")
                    {
                        try
                        {
                            using (BitMiracle.LibTiff.Classic.Tiff tiffImg = BitMiracle.LibTiff.Classic.Tiff.Open(dlg.FileName, "r"))
                            {
                                if (tiffImg != null)
                                {
                                    BitMiracle.LibTiff.Classic.FieldValue[] res = tiffImg.GetField(BitMiracle.LibTiff.Classic.TiffTag.XRESOLUTION);
                                    if (res != null)
                                    {
                                        float xRes = res[0].ToFloat();
                                        res = tiffImg.GetField(BitMiracle.LibTiff.Classic.TiffTag.RESOLUTIONUNIT);
                                        short unit = (res != null) ? res[0].ToShort() : (short)2; // 2 = Inch

                                        if (unit == 3) // Centimeter
                                        {
                                            newDpiX = xRes * 2.54;
                                            newDpiY = xRes * 2.54;
                                        }
                                        else
                                        {
                                            newDpiX = xRes;
                                            newDpiY = xRes;
                                        }
                                    }
                                }
                            }
                        }
                        catch { /* Fallback to WPF Dpi */ }
                    }

                    _dpi = (newDpiX + newDpiY) / 2.0;
                    if (_dpi <= 0) _dpi = 72; // final fallback check
                    _mmPerPixelX = 25.4 / newDpiX;
                    _mmPerPixelY = 25.4 / newDpiY;
                    
                    int bytesPerPixel = bmp.Format.BitsPerPixel / 8;
                    if (bytesPerPixel == 0) throw new Exception("Unsupported pixel format.");
                    
                    int stride = w * bytesPerPixel;
                    var rawBytes = new byte[h * stride];
                    bmp.CopyPixels(rawBytes, stride, 0);
                    
                    double[,] channel = new double[h, w];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int idx = y * stride + x * bytesPerPixel;
                            if (bytesPerPixel >= 6) // true 48-bit RGB (16-bit per channel)
                            {
                                channel[y, x] = BitConverter.ToUInt16(rawBytes, idx);
                            }
                            else if (bytesPerPixel >= 3) // 24-bit RGB or 32-bit BGRA
                            {
                                byte r = bytesPerPixel == 3 ? rawBytes[idx + 0] : rawBytes[idx + 2];
                                channel[y, x] = r * 257.0; // scale up to 16-bit equivalent
                            }
                            else if (bytesPerPixel == 2) // 16-bit Gray
                            {
                                channel[y, x] = BitConverter.ToUInt16(rawBytes, idx);
                            }
                            else
                            {
                                channel[y, x] = rawBytes[idx] * 257.0;
                            }
                        }
                    }

                    _activeExtractMap = channel;
                    FilmImage.Source = bmp;
                    FilmImage.Width = w;
                    FilmImage.Height = h;
                    ResetTargetForCurrentImage();
                    
                    if (StatusText != null) StatusText.Text = $"Calibration scan loaded ({w}x{h}). Position the target over each patch and extract OD.";
                    if (ImageStatusText != null) ImageStatusText.Text = "Calibration scan is displayed for OD extraction. Load an analysis film on the Analyze tab when calibration is done.";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error loading scan: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Converts a raw pixel value to calibrated dose using the stored Quick Cal curve.
        /// If no calibration is stored, returns the raw value unchanged.
        /// </summary>
        private double PixelToDose(double rawValue)
        {
            if (_quickCalCoeffs == null) return rawValue;
            
            double od;
            if (rawValue > 100) // raw 16-bit
            {
                rawValue = Math.Clamp(rawValue, 1, 65535);
                od = -Math.Log10(rawValue / 65535.0);
            }
            else
            {
                od = rawValue; // already OD
            }
            return Math.Max(0, FittingMath.PolyVal(_quickCalCoeffs, od));
        }

        private void ReloadOriginalImage_Click(object sender, RoutedEventArgs e)
        {
            if (_originalImageSource != null)
            {
                _activeExtractMap = null;
                FilmImage.Source = _originalImageSource;
                FilmImage.Width = _doseMap.GetLength(1);
                FilmImage.Height = _doseMap.GetLength(0);
                if (StatusText != null) StatusText.Text = "Original image restored.";
            }
        }

        private void SyncMainFilmButton_Click(object sender, RoutedEventArgs e)
        {
            TrySyncAnalysisFilmFromMain(true);
        }

        /// <summary>
        /// Loads a new analysis film into the FieldSize window, replacing
        /// the current _doseMap and image display. The saved calibration curve is preserved.
        /// </summary>
        private void LoadAnalysisFilm_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image files|*.tif;*.tiff;*.png;*.jpg;*.bmp|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var bmp = new BitmapImage(new Uri(dlg.FileName));
                    int w = bmp.PixelWidth;
                    int h = bmp.PixelHeight;
                    
                    double newDpiX = bmp.DpiX > 0 ? bmp.DpiX : 72.0;
                    double newDpiY = bmp.DpiY > 0 ? bmp.DpiY : 72.0;

                    // If it's a TIFF, prefer LibTiff for precise physical scan resolution
                    string ext = System.IO.Path.GetExtension(dlg.FileName).ToLower();
                    if (ext == ".tif" || ext == ".tiff")
                    {
                        try
                        {
                            using (BitMiracle.LibTiff.Classic.Tiff tiffImg = BitMiracle.LibTiff.Classic.Tiff.Open(dlg.FileName, "r"))
                            {
                                if (tiffImg != null)
                                {
                                    BitMiracle.LibTiff.Classic.FieldValue[] res = tiffImg.GetField(BitMiracle.LibTiff.Classic.TiffTag.XRESOLUTION);
                                    if (res != null)
                                    {
                                        float xRes = res[0].ToFloat();
                                        res = tiffImg.GetField(BitMiracle.LibTiff.Classic.TiffTag.RESOLUTIONUNIT);
                                        short unit = (res != null) ? res[0].ToShort() : (short)2; // 2 = Inch

                                        if (unit == 3) // Centimeter
                                        {
                                            newDpiX = xRes * 2.54;
                                            newDpiY = xRes * 2.54;
                                        }
                                        else
                                        {
                                            newDpiX = xRes;
                                            newDpiY = xRes;
                                        }
                                    }
                                }
                            }
                        }
                        catch { /* Fallback to WPF Dpi */ }
                    }

                    _dpi = (newDpiX + newDpiY) / 2.0;
                    if (_dpi <= 0) _dpi = 72; // final fallback check
                    _mmPerPixelX = 25.4 / newDpiX;
                    _mmPerPixelY = 25.4 / newDpiY;
                    
                    int bytesPerPixel = bmp.Format.BitsPerPixel / 8;
                    if (bytesPerPixel == 0) throw new Exception("Unsupported pixel format.");
                    
                    int stride = w * bytesPerPixel;
                    var rawBytes = new byte[h * stride];
                    bmp.CopyPixels(rawBytes, stride, 0);
                    
                    var newMap = new double[h, w];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int idx = y * stride + x * bytesPerPixel;
                            if (bytesPerPixel >= 6)
                                newMap[y, x] = BitConverter.ToUInt16(rawBytes, idx);
                            else if (bytesPerPixel >= 3)
                            {
                                byte r = bytesPerPixel == 3 ? rawBytes[idx + 0] : rawBytes[idx + 2];
                                newMap[y, x] = r * 257.0;
                            }
                            else if (bytesPerPixel == 2)
                                newMap[y, x] = BitConverter.ToUInt16(rawBytes, idx);
                            else
                                newMap[y, x] = rawBytes[idx] * 257.0;
                        }
                    }

                    // Replace _doseMap with the new film data
                    _doseMap = newMap;
                    _hasAnalysisFilm = true;

                    _originalImageSource = bmp;
                    _activeExtractMap = null;
                    FilmImage.Source = bmp;
                    FilmImage.Width = w;
                    FilmImage.Height = h;
                    _analysisImageName = System.IO.Path.GetFileName(dlg.FileName);
                    ResetTargetForCurrentImage();
                    RefreshAnalysisImageStatus();
                    
                    string calStatus = _quickCalCoeffs != null ? " Cal curve ready." : "";
                    if (StatusText != null) StatusText.Text = $"Analysis film loaded ({w}x{h}).{calStatus} Align the target and run field size analysis.";
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error loading analysis film: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Creates and stores the calibration curve from the 3 OD input values.
        /// Does NOT modify the dose map — coefficients are applied on-the-fly during Recalculate.
        /// </summary>
        private void ApplyQuickCal_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(QD0Box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double od0) ||
                !double.TryParse(QD300Box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double od300) ||
                !double.TryParse(QD600Box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double od600))
            {
                System.Windows.MessageBox.Show("Please ensure all three OD values are filled with valid numbers.", "Invalid Input", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(QD0DoseBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double dose0) ||
                !double.TryParse(QD300DoseBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double dose300) ||
                !double.TryParse(QD600DoseBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double dose600))
            {
                System.Windows.MessageBox.Show("Please ensure all three dose/MU values are filled with valid numbers.", "Invalid Input", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (Math.Abs(dose0 - dose300) < 1e-9 || Math.Abs(dose0 - dose600) < 1e-9 || Math.Abs(dose300 - dose600) < 1e-9)
            {
                System.Windows.MessageBox.Show("Each calibration point needs a unique dose/MU value.", "Invalid Calibration Points", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (od0 >= od300 || od300 >= od600)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Warning: OD values should increase with dose.\n\n" +
                    $"  {dose0:F1} OD = {od0:F6}\n" +
                    $"  {dose300:F1} OD = {od300:F6}\n" +
                    $"  {dose600:F1} OD = {od600:F6}\n\n" +
                    $"These values appear out of order. Continue anyway?",
                    "OD Order Check", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.Yes) return;
            }

            try
            {
                double[] xVals = { od0, od300, od600 };
                double[] yVals = { dose0, dose300, dose600 };
                
                _quickCalCoeffs = FittingMath.PolyFit(xVals, yVals, 2);
                _quickCalFullDose = yVals.Max();
                if (KnownFullDoseBox != null)
                    KnownFullDoseBox.Text = _quickCalFullDose.Value.ToString("F3", CultureInfo.InvariantCulture);
                UpdateKnownDosePreview();

                foreach (ComboBoxItem item in MethodBox.Items)
                {
                    if (item.Content?.ToString() == "Known Dose")
                    {
                        MethodBox.SelectedItem = item;
                        break;
                    }
                }

                string info = $"Calibration curve created!\n\n" +
                              $"Dose = {_quickCalCoeffs[0]:F4}·OD² + {_quickCalCoeffs[1]:F4}·OD + {_quickCalCoeffs[2]:F4}\n\n" +
                              $"Verification:\n" +
                              $"  f({od0:F4}) = {FittingMath.PolyVal(_quickCalCoeffs, od0):F1} (expect {dose0:F1})\n" +
                              $"  f({od300:F4}) = {FittingMath.PolyVal(_quickCalCoeffs, od300):F1} (expect {dose300:F1})\n" +
                              $"  f({od600:F4}) = {FittingMath.PolyVal(_quickCalCoeffs, od600):F1} (expect {dose600:F1})\n\n" +
                              $"Known Dose analysis will use {_quickCalFullDose.Value:F3} full dose and {_quickCalFullDose.Value / 2.0:F3} half-dose threshold.\n\n" +
                              $"Now load your analysis film and run analysis.";
                
                if (CalStatusText != null) CalStatusText.Text = $"✓ {_quickCalCoeffs[0]:F2}·OD² + {_quickCalCoeffs[1]:F2}·OD + {_quickCalCoeffs[2]:F2}";
                if (StatusText != null) StatusText.Text = "Calibration saved. Load the analysis film, align the target, and run analysis.";
                RefreshAnalysisImageStatus();
                if (WorkflowTabs != null) WorkflowTabs.SelectedIndex = 1;
                
                System.Windows.MessageBox.Show(info, "Calibration Created", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Failed to create calibration curve: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Generates a Jet colormap heatmap of the dose-converted image and updates the display.
        /// </summary>
        private void UpdateJetHeatmap()
        {
            if (_quickCalCoeffs == null) return;

            int h = _doseMap.GetLength(0);
            int w = _doseMap.GetLength(1);
            
            double maxDose = 1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double d = PixelToDose(_doseMap[y, x]);
                    if (d > maxDose) maxDose = d;
                }

            int stride = w * 4;
            byte[] pixels = new byte[h * stride];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double dose = PixelToDose(_doseMap[y, x]);
                    double norm = Math.Clamp(dose / maxDose, 0, 1);
                    var (r, g, b) = ColorMaps.GetJetColor(norm);
                    
                    int idx = y * stride + x * 4;
                    pixels[idx + 0] = b;
                    pixels[idx + 1] = g;
                    pixels[idx + 2] = r;
                    pixels[idx + 3] = 255;
                }
            }

            var bmp = BitmapSource.Create(w, h, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, stride);
            bmp.Freeze();
            FilmImage.Source = bmp;
            FilmImage.Width = w;
            FilmImage.Height = h;
            UpdateOverlayVisualScale();
        }
        #endregion


    }
}
