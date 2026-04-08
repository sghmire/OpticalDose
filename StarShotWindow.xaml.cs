using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Documents;
using System.Printing;
using Wpf.Ui.Controls;

namespace FilmQA
{
    public partial class StarShotWindow : FluentWindow
    {
        // ── Geometry ───────────────────────────────────────────────────────────────
        /// <summary>Line in normal form  a·x + b·y = c  with  a²+b² = 1.</summary>
        private class SpokeLine
        {
            public double A, B, C;
            public double AngleDeg;

            public double DistanceTo(double px, double py)
                => Math.Abs(A * px + B * py - C);

            public static Point? Intersect(SpokeLine l1, SpokeLine l2)
            {
                double det = l1.A * l2.B - l2.A * l1.B;
                if (Math.Abs(det) < 1e-10) return null;
                return new Point(
                    (l1.C * l2.B - l2.C * l1.B) / det,
                    (l1.A * l2.C - l2.A * l1.C) / det);
            }

            public static SpokeLine FromPointAndDirection(
                double px, double py, double dirX, double dirY)
            {
                double len = Math.Sqrt(dirX * dirX + dirY * dirY);
                if (len < 1e-12)
                    throw new InvalidOperationException("Degenerate spoke direction.");
                // Normal is perpendicular to direction
                double a = -dirY / len;
                double b =  dirX / len;
                double c = a * px + b * py;
                return new SpokeLine { A = a, B = b, C = c };
            }
        }

        // ── Fields ─────────────────────────────────────────────────────────────────
        private readonly double[,] _data;
        private readonly double    _dpi;
        private readonly Point     _pickedCenter;
        private readonly double    _mmPerPixel;

        private bool   _isDrawingCircle;
        private double _analysisRadiusPx = -1;

        private List<SpokeLine>? _lastSpokes;
        private Point            _lastIsocenter;
        private double           _lastIsoPxRadius;

        // ── Constructor ────────────────────────────────────────────────────────────
        public StarShotWindow(double[,] data, double dpi, Point pickedCenter)
        {
            InitializeComponent();
            _data         = data ?? throw new ArgumentNullException(nameof(data));
            _dpi          = dpi <= 0 ? 72.0 : dpi;
            _pickedCenter = pickedCenter;
            _mmPerPixel   = 25.4 / _dpi;

            UpdateStarShotDisplay();
            EmptyMessage.Visibility = Visibility.Collapsed;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  IMAGE DISPLAY
        // ════════════════════════════════════════════════════════════════════════════
        private void UpdateStarShotDisplay()
        {
            int h = _data.GetLength(0), w = _data.GetLength(1);
            double min = double.MaxValue, max = double.MinValue;
            foreach (var v in _data) { if (v < min) min = v; if (v > max) max = v; }
            if (max <= min) max = min + 1;

            byte[] px = new byte[w * h * 4];
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                {
                    byte val = (byte)(Math.Clamp((_data[r, c] - min) / (max - min), 0, 1) * 255);
                    int i = (r * w + c) * 4;
                    px[i] = px[i + 1] = px[i + 2] = val;
                    px[i + 3] = 255;
                }
            StarShotImage.Source =
                BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, px, w * 4);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  COORDINATE HELPERS
        // ════════════════════════════════════════════════════════════════════════════
        private double GetCanvasScale()
        {
            int imgW = _data.GetLength(1), imgH = _data.GetLength(0);
            double canW = MainCanvas.ActualWidth, canH = MainCanvas.ActualHeight;
            if (canW <= 0 || canH <= 0 || imgW <= 0 || imgH <= 0) return 1;
            return Math.Min(canW / imgW, canH / imgH);
        }

        private Point ToCanvas(double imgX, double imgY)
        {
            int imgW = _data.GetLength(1), imgH = _data.GetLength(0);
            double canW = MainCanvas.ActualWidth, canH = MainCanvas.ActualHeight;
            double scale = Math.Min(canW / imgW, canH / imgH);
            double offX  = (canW - imgW * scale) / 2;
            double offY  = (canH - imgH * scale) / 2;
            return new Point(imgX * scale + offX, imgY * scale + offY);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  INTERACTIVE CIRCLE
        // ════════════════════════════════════════════════════════════════════════════
        private void DrawCircle_Click(object sender, RoutedEventArgs e)
        {
            _isDrawingCircle = true;
            DrawCircleButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Caution;
            DrawCircleButton.Content    = "Drawing… drag on image";
            MainCanvas.Cursor           = Cursors.Cross;
            DrawHintBanner.Visibility   = Visibility.Visible;
        }

        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawingCircle || e.ChangedButton != MouseButton.Left) return;
            e.Handled = true;
            MainCanvas.CaptureMouse();
            UpdateCircleFromMouse(e.GetPosition(MainCanvas));
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawingCircle || e.LeftButton != MouseButtonState.Pressed) return;
            UpdateCircleFromMouse(e.GetPosition(MainCanvas));
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawingCircle && e.ChangedButton != MouseButton.Left) return;
            MainCanvas.ReleaseMouseCapture();
            _isDrawingCircle = false;
            DrawCircleButton.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            DrawCircleButton.Content    = "Draw Circle on Image";
            MainCanvas.Cursor           = Cursors.Arrow;
            DrawHintBanner.Visibility   = Visibility.Collapsed;

            if (_analysisRadiusPx > 5)
                CircleRadiusText.Text =
                    $"Radius: {_analysisRadiusPx * _mmPerPixel:F1} mm  ({_analysisRadiusPx:F0} px)";
            else
                CircleRadiusText.Text = "No circle drawn yet";
        }

        private void UpdateCircleFromMouse(Point canvasPos)
        {
            var cc = ToCanvas(_pickedCenter.X, _pickedCenter.Y);
            double dx = canvasPos.X - cc.X, dy = canvasPos.Y - cc.Y;
            _analysisRadiusPx = Math.Sqrt(dx * dx + dy * dy) / GetCanvasScale();
            PositionAnalysisCircle();
        }

        private void PositionAnalysisCircle()
        {
            if (_analysisRadiusPx <= 0)
            { AnalysisCircleEl.Visibility = Visibility.Collapsed; return; }

            double scale = GetCanvasScale();
            double canR  = _analysisRadiusPx * scale;
            var cc = ToCanvas(_pickedCenter.X, _pickedCenter.Y);

            AnalysisCircleEl.Width  = canR * 2;
            AnalysisCircleEl.Height = canR * 2;
            Canvas.SetLeft(AnalysisCircleEl, cc.X - canR);
            Canvas.SetTop (AnalysisCircleEl, cc.Y - canR);
            AnalysisCircleEl.Visibility = Visibility.Visible;

            if (_isDrawingCircle)
                CircleRadiusText.Text =
                    $"Radius: {_analysisRadiusPx * _mmPerPixel:F1} mm";
        }

        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositionAnalysisCircle();
            if (_lastSpokes != null)
                DrawOverlays(_lastSpokes, _lastIsocenter, _lastIsoPxRadius);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  MAIN ANALYSIS
        // ════════════════════════════════════════════════════════════════════════════
        private void RunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (_analysisRadiusPx <= 0)
            {
                System.Windows.MessageBox.Show(
                    "Draw an analysis circle first.", "No Analysis Region");
                return;
            }
            try
            {
                StatusRun("Analyzing…");
                PerformStarShotAnalysis();
                StatusRun("Analysis Complete", true);
            }
            catch (Exception ex)
            {
                StatusRun($"Error: {ex.Message}", false);
                System.Windows.MessageBox.Show(ex.Message, "Analysis Error");
            }
        }

        private void PerformStarShotAnalysis()
        {
            double thresholdPct = ThresholdInput.Value ?? 50;
            double R = _analysisRadiusPx;
            int cx   = (int)_pickedCenter.X;
            int cy   = (int)_pickedCenter.Y;

            // ── Step 1: averaged angular profile ───────────────────────────────
            var (profile, isInverted) = BuildAveragedProfile(cx, cy, R);

            // ── Step 2: detect ALL crossings (no de-dup yet) ───────────────────
            var allCrossings = DetectAllCrossings(profile, isInverted, thresholdPct);

            if (allCrossings.Count < 3)
                throw new Exception(
                    $"Only {allCrossings.Count} crossings found (need ≥ 3).\n" +
                    "Adjust Threshold or enlarge the circle.");

            // ── Step 2b: pair crossings ~180° apart → physical beams ───────────
            var beams = PairCrossingsIntoBeams(allCrossings);

            if (beams.Count < 3)
                throw new Exception(
                    $"Only {beams.Count} beams found (need ≥ 3).\n" +
                    "Adjust Threshold or enlarge the circle.");

            // ── Step 3: fit centrelines using both sides of each beam ──────────
            // Adaptive perpendicular search width from inter-crossing spacing
            double minGapDeg = 360.0;
            var sortedAll = allCrossings.Select(c => c.angle).OrderBy(a => a).ToList();
            for (int i = 0; i < sortedAll.Count; i++)
            {
                double gap = sortedAll[(i + 1) % sortedAll.Count] - sortedAll[i];
                if (gap <= 0) gap += 360;
                if (gap < minGapDeg) minGapDeg = gap;
            }
            double searchHalfPx = R * Math.Sin(minGapDeg / 2.0 * Math.PI / 180.0) * 0.40;
            searchHalfPx = Math.Max(searchHalfPx, 8);

            var spokes = new List<SpokeLine>();
            // For the profile plot, collect one representative angle per beam
            var beamAngles = new List<double>();

            foreach (var beam in beams)
            {
                var line = FitBeamCenterline(
                    cx, cy, beam.angle1, beam.angle2, R, searchHalfPx, isInverted);
                spokes.Add(line);
                beamAngles.Add(beam.angle1);
            }

            // ── Step 4: minimum tangent circle (Chebyshev centre) ──────────────
            var (isoX, isoY, radiusMm) = FindMinimumTangentCircle(spokes);
            double diameterMm  = radiusMm * 2;
            double isoPxRadius = radiusMm / _mmPerPixel;

            _lastSpokes      = spokes;
            _lastIsocenter   = new Point(isoX, isoY);
            _lastIsoPxRadius = isoPxRadius;

            UpdateResultsUI(diameterMm, spokes.Count, _lastIsocenter);
            DrawOverlays(spokes, _lastIsocenter, isoPxRadius);
            PlotProfile(profile, isInverted, beamAngles, thresholdPct);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  STEP 1 — AVERAGED CIRCULAR PROFILE
        // ════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Builds an angular profile averaged over many concentric rings from
        /// 50 % to 95 % of the drawn radius, then Gaussian-smoothes it.
        /// This dramatically reduces pixel noise compared to a single ring.
        /// </summary>
        private (double[] profile, bool isInverted) BuildAveragedProfile(
            int cx, int cy, double maxR)
        {
            const int N       = 720;
            const int nRings  = 25;

            var accum = new double[N];

            for (int ri = 0; ri < nRings; ri++)
            {
                double r = maxR * (0.50 + 0.45 * ri / (nRings - 1));
                for (int i = 0; i < N; i++)
                {
                    double theta = i * 2.0 * Math.PI / N;
                    accum[i] += BilinearSample(
                        cx + r * Math.Cos(theta),
                        cy + r * Math.Sin(theta));
                }
            }
            for (int i = 0; i < N; i++) accum[i] /= nRings;

            var smooth = GaussianSmooth(accum, 3);

            double mean = smooth.Average();
            double max  = smooth.Max();
            double min  = smooth.Min();
            bool isInverted = (max - mean) < (mean - min);

            return (smooth, isInverted);
        }

        private static double[] GaussianSmooth(double[] data, int sigma)
        {
            int N = data.Length;
            int hw = sigma * 3;
            var kernel = new double[2 * hw + 1];
            double ksum = 0;
            for (int k = -hw; k <= hw; k++)
            {
                kernel[k + hw] = Math.Exp(-0.5 * k * k / (sigma * sigma));
                ksum += kernel[k + hw];
            }
            for (int k = 0; k < kernel.Length; k++) kernel[k] /= ksum;

            var result = new double[N];
            for (int i = 0; i < N; i++)
                for (int k = -hw; k <= hw; k++)
                    result[i] += data[((i + k) % N + N) % N] * kernel[k + hw];
            return result;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  STEP 2 — DETECT ALL CROSSINGS (no de-dup)
        // ════════════════════════════════════════════════════════════════════════════
        private record struct Crossing(double angle, double strength);

        /// <summary>
        /// Finds every threshold crossing in the angular profile.
        /// For 5 through-beams this will return ~10 crossings.
        /// Each crossing records its angle and its peak signal strength.
        /// </summary>
        private List<Crossing> DetectAllCrossings(
            double[] profile, bool isInverted, double thresholdPct)
        {
            int N = profile.Length;
            double max = profile.Max(), min = profile.Min();
            double threshold = isInverted
                ? max - (max - min) * (thresholdPct / 100.0)
                : min + (max - min) * (thresholdPct / 100.0);

            var regions = new List<(int start, int end)>();
            bool inSpoke = false;
            int startIdx = 0;

            for (int i = 0; i < N; i++)
            {
                bool s = isInverted ? profile[i] < threshold
                                    : profile[i] > threshold;
                if (s && !inSpoke) { startIdx = i; inSpoke = true; }
                else if (!s && inSpoke)
                { regions.Add((startIdx, i - 1)); inSpoke = false; }
            }
            if (inSpoke) regions.Add((startIdx, N - 1));

            // Wraparound merge
            if (regions.Count >= 2)
            {
                var first = regions[0];
                var last  = regions[^1];
                if (first.start == 0 && last.end == N - 1)
                {
                    regions[0] = (last.start - N, first.end);
                    regions.RemoveAt(regions.Count - 1);
                }
            }

            var crossings = new List<Crossing>();
            foreach (var (rs, re) in regions)
            {
                double wSum = 0, aSum = 0, peakStrength = 0;
                for (int i = rs; i <= re; i++)
                {
                    int idx = ((i % N) + N) % N;
                    double val = profile[idx];
                    double w = isInverted ? (threshold - val) : (val - threshold);
                    if (w < 0) w = 0;
                    if (w > peakStrength) peakStrength = w;
                    w *= w;
                    aSum += w * (i * 360.0 / N);
                    wSum += w;
                }
                if (wSum < 1e-12) continue;
                double mid = aSum / wSum;
                mid = ((mid % 360) + 360) % 360;
                crossings.Add(new Crossing(mid, peakStrength));
            }

            return crossings;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  STEP 2b — PAIR CROSSINGS INTO PHYSICAL BEAMS
        // ════════════════════════════════════════════════════════════════════════════
        private record struct Beam(double angle1, double angle2);

        /// <summary>
        /// Groups crossings that are ~180° apart into physical beams.
        /// Each beam is represented by two angles (entry and exit side).
        /// For a crossing with no partner (e.g. C-arm single-side), angle2 = angle1+180.
        /// </summary>
        private List<Beam> PairCrossingsIntoBeams(List<Crossing> crossings)
        {
            var used = new bool[crossings.Count];
            var beams = new List<Beam>();

            for (int i = 0; i < crossings.Count; i++)
            {
                if (used[i]) continue;

                double bestDiff = double.MaxValue;
                int bestJ = -1;

                for (int j = i + 1; j < crossings.Count; j++)
                {
                    if (used[j]) continue;
                    double diff = Math.Abs(
                        ((crossings[i].angle - crossings[j].angle + 540) % 360) - 180);
                    if (diff < bestDiff) { bestDiff = diff; bestJ = j; }
                }

                if (bestJ >= 0 && bestDiff < 20)
                {
                    // Found a partner — pair them
                    used[i] = true;
                    used[bestJ] = true;
                    beams.Add(new Beam(crossings[i].angle, crossings[bestJ].angle));
                }
                else
                {
                    // No partner (single-sided spoke, e.g. C-arm)
                    used[i] = true;
                    beams.Add(new Beam(crossings[i].angle,
                        (crossings[i].angle + 180) % 360));
                }
            }

            return beams;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  STEP 3 — FIT CENTRELINE USING BOTH SIDES
        // ════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Samples the beam from both sides of center (angle1 and angle2),
        /// collecting sub-pixel centroid points along the full diameter.
        /// This gives a much longer baseline than single-sided sampling.
        /// </summary>
        private SpokeLine FitBeamCenterline(
            int cx, int cy, double angle1Deg, double angle2Deg,
            double R, double searchHalfPx, bool isInverted)
        {
            const int nRadiiPerSide = 30;
            const int nPerp = 100;

            var points = new List<(double x, double y)>();

            // Sample both sides
            double[] sides = { angle1Deg, angle2Deg };
            foreach (double angleDeg in sides)
            {
                double rad = angleDeg * Math.PI / 180.0;
                double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
                double cosP = -sinA, sinP = cosA;

                for (int ri = 1; ri <= nRadiiPerSide; ri++)
                {
                    double r   = R * (0.20 + 0.70 * ri / nRadiiPerSide);
                    double px0 = cx + r * cosA;
                    double py0 = cy + r * sinA;

                    var vals = new double[2 * nPerp + 1];
                    for (int k = -nPerp; k <= nPerp; k++)
                    {
                        double t = k * searchHalfPx / nPerp;
                        vals[k + nPerp] = BilinearSample(
                            px0 + t * cosP, py0 + t * sinP);
                    }

                    if (isInverted)
                        for (int k = 0; k < vals.Length; k++)
                            vals[k] = -vals[k];

                    double vMin = vals.Min();
                    double wSum = 0, tSum = 0;
                    for (int k = 0; k < vals.Length; k++)
                    {
                        double w = Math.Max(vals[k] - vMin, 0);
                        w *= w;
                        double t = (k - nPerp) * searchHalfPx / nPerp;
                        tSum += w * t;
                        wSum += w;
                    }
                    if (wSum < 1e-12) continue;

                    double offset = tSum / wSum;
                    points.Add((px0 + offset * cosP, py0 + offset * sinP));
                }
            }

            if (points.Count < 8)
                throw new Exception(
                    $"Too few valid points on beam at {angle1Deg:F0}°/{angle2Deg:F0}°.");

            // Use angle1 as the reference direction for TLS sign alignment
            double refRad = angle1Deg * Math.PI / 180.0;
            double refCos = Math.Cos(refRad), refSin = Math.Sin(refRad);

            var line = FitLineTLS(points, refCos, refSin);

            // Outlier rejection: discard points > 3 px from fit, re-fit
            var inliers = points
                .Where(p => line.DistanceTo(p.x, p.y) < 3.0)
                .ToList();
            if (inliers.Count >= 8)
                line = FitLineTLS(inliers, refCos, refSin);

            line.AngleDeg = angle1Deg;
            return line;
        }

        /// <summary>
        /// Total Least Squares line fit.  Returns a <see cref="SpokeLine"/>
        /// whose direction aligns with the expected spoke direction (cosA, sinA).
        /// </summary>
        private static SpokeLine FitLineTLS(
            List<(double x, double y)> pts, double cosA, double sinA)
        {
            double mx = pts.Average(p => p.x);
            double my = pts.Average(p => p.y);
            double sxx = 0, syy = 0, sxy = 0;
            foreach (var (x, y) in pts)
            {
                double dx = x - mx, dy = y - my;
                sxx += dx * dx;
                syy += dy * dy;
                sxy += dx * dy;
            }
            // Eigenvector for the larger eigenvalue of [[sxx,sxy],[sxy,syy]]
            double theta = 0.5 * Math.Atan2(2 * sxy, sxx - syy);
            double dirX = Math.Cos(theta), dirY = Math.Sin(theta);

            // Ensure direction aligns with the expected spoke direction
            if (dirX * cosA + dirY * sinA < 0)
            { dirX = -dirX; dirY = -dirY; }

            return SpokeLine.FromPointAndDirection(mx, my, dirX, dirY);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  STEP 4 — MINIMUM TANGENT CIRCLE  (Nelder–Mead on min-max distance)
        // ════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Finds the Chebyshev centre of the spoke lines — the point that
        /// minimises the maximum perpendicular distance to all spokes.
        /// The radius of the resulting circle is the star-shot isocenter radius.
        /// </summary>
        private (double isoX, double isoY, double radiusMm)
            FindMinimumTangentCircle(List<SpokeLine> spokes)
        {
            double MaxDist(double px, double py)
                => spokes.Max(s => s.DistanceTo(px, py));

            // Seed: median of pairwise intersections (more robust than mean)
            var ixPts = new List<Point>();
            for (int i = 0; i < spokes.Count; i++)
                for (int j = i + 1; j < spokes.Count; j++)
                {
                    var pt = SpokeLine.Intersect(spokes[i], spokes[j]);
                    if (pt.HasValue) ixPts.Add(pt.Value);
                }
            if (ixPts.Count == 0)
                throw new Exception("No spoke intersections found.");

            // Use median X and Y (robust against outlier intersections)
            var sortedX = ixPts.Select(p => p.X).OrderBy(v => v).ToList();
            var sortedY = ixPts.Select(p => p.Y).OrderBy(v => v).ToList();
            double seedX = sortedX[sortedX.Count / 2];
            double seedY = sortedY[sortedY.Count / 2];

            // Adaptive initial step: half the current max distance
            double initDist = MaxDist(seedX, seedY);
            double step = Math.Max(initDist * 0.5, 2.0);

            // Nelder–Mead simplex
            double[,] s = {
                { seedX, seedY },
                { seedX + step, seedY },
                { seedX, seedY + step }
            };
            double[] fv = {
                MaxDist(s[0, 0], s[0, 1]),
                MaxDist(s[1, 0], s[1, 1]),
                MaxDist(s[2, 0], s[2, 1])
            };

            for (int iter = 0; iter < 5000; iter++)
            {
                // Sort
                int best, mid, worst;
                if (fv[0] <= fv[1])
                {
                    if (fv[1] <= fv[2])      { best = 0; mid = 1; worst = 2; }
                    else if (fv[0] <= fv[2]) { best = 0; mid = 2; worst = 1; }
                    else                      { best = 2; mid = 0; worst = 1; }
                }
                else
                {
                    if (fv[0] <= fv[2])      { best = 1; mid = 0; worst = 2; }
                    else if (fv[1] <= fv[2]) { best = 1; mid = 2; worst = 0; }
                    else                      { best = 2; mid = 1; worst = 0; }
                }

                // Convergence: simplex has collapsed
                double span = Math.Max(
                    Math.Abs(s[0, 0] - s[1, 0]) + Math.Abs(s[0, 1] - s[1, 1]),
                    Math.Max(
                        Math.Abs(s[0, 0] - s[2, 0]) + Math.Abs(s[0, 1] - s[2, 1]),
                        Math.Abs(s[1, 0] - s[2, 0]) + Math.Abs(s[1, 1] - s[2, 1])));
                if (span < 1e-6) break;

                // Centroid of best two
                double cx2 = (s[best, 0] + s[mid, 0]) / 2;
                double cy2 = (s[best, 1] + s[mid, 1]) / 2;

                // Reflect
                double rx = 2 * cx2 - s[worst, 0];
                double ry = 2 * cy2 - s[worst, 1];
                double fr = MaxDist(rx, ry);

                if (fr < fv[best])
                {
                    // Expand
                    double ex = cx2 + 2 * (rx - cx2);
                    double ey = cy2 + 2 * (ry - cy2);
                    double fe = MaxDist(ex, ey);
                    if (fe < fr) { s[worst, 0] = ex; s[worst, 1] = ey; fv[worst] = fe; }
                    else         { s[worst, 0] = rx; s[worst, 1] = ry; fv[worst] = fr; }
                }
                else if (fr < fv[mid])
                {
                    s[worst, 0] = rx; s[worst, 1] = ry; fv[worst] = fr;
                }
                else
                {
                    // Contract
                    double kx, ky;
                    if (fr < fv[worst])
                    { kx = cx2 + 0.5 * (rx - cx2); ky = cy2 + 0.5 * (ry - cy2); }
                    else
                    { kx = cx2 + 0.5 * (s[worst, 0] - cx2);
                      ky = cy2 + 0.5 * (s[worst, 1] - cy2); }

                    double fk = MaxDist(kx, ky);
                    if (fk < fv[worst])
                    {
                        s[worst, 0] = kx; s[worst, 1] = ky; fv[worst] = fk;
                    }
                    else
                    {
                        // Shrink toward best
                        for (int si = 0; si < 3; si++)
                        {
                            if (si == best) continue;
                            s[si, 0] = s[best, 0] + 0.5 * (s[si, 0] - s[best, 0]);
                            s[si, 1] = s[best, 1] + 0.5 * (s[si, 1] - s[best, 1]);
                            fv[si] = MaxDist(s[si, 0], s[si, 1]);
                        }
                    }
                }
            }

            int b = fv[0] <= fv[1] ? (fv[0] <= fv[2] ? 0 : 2) : (fv[1] <= fv[2] ? 1 : 2);
            double fx = s[b, 0], fy = s[b, 1];
            return (fx, fy, MaxDist(fx, fy) * _mmPerPixel);
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  BILINEAR INTERPOLATION
        // ════════════════════════════════════════════════════════════════════════════
        private double BilinearSample(double px, double py)
        {
            int x0 = (int)Math.Floor(px), y0 = (int)Math.Floor(py);
            int x1 = x0 + 1, y1 = y0 + 1;
            int h = _data.GetLength(0), w = _data.GetLength(1);
            if (x0 < 0 || y0 < 0 || x1 >= w || y1 >= h) return 0;
            double tx = px - x0, ty = py - y0;
            return _data[y0, x0] * (1 - tx) * (1 - ty)
                 + _data[y0, x1] *      tx  * (1 - ty)
                 + _data[y1, x0] * (1 - tx) *      ty
                 + _data[y1, x1] *      tx  *      ty;
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  UI  (results, overlays, plot)
        // ════════════════════════════════════════════════════════════════════════════
        private void UpdateResultsUI(double diameterMm, int nSpokes, Point iso)
        {
            IsoDiameterText.Text = $"{diameterMm:F3} mm";
            var sb = new StringBuilder();
            sb.AppendLine($"Spokes detected:  {nSpokes}");
            sb.AppendLine($"Isocenter:  ({iso.X:F1}, {iso.Y:F1}) px");
            sb.AppendLine($"Diameter:   {diameterMm:F3} mm");
            ResultSummaryText.Text = sb.ToString();
        }

        private void DrawOverlays(List<SpokeLine> spokes, Point iso, double isoPxR)
        {
            OverlayCanvas.Children.Clear();
            int imgH = _data.GetLength(0), imgW = _data.GetLength(1);

            foreach (var sp in spokes)
            {
                double px1, py1, px2, py2;
                if (Math.Abs(sp.B) > Math.Abs(sp.A))
                {
                    px1 = 0;    py1 = (sp.C - sp.A * px1) / sp.B;
                    px2 = imgW; py2 = (sp.C - sp.A * px2) / sp.B;
                }
                else
                {
                    py1 = 0;    px1 = (sp.C - sp.B * py1) / sp.A;
                    py2 = imgH; px2 = (sp.C - sp.B * py2) / sp.A;
                }

                var c1 = ToCanvas(px1, py1);
                var c2 = ToCanvas(px2, py2);

                OverlayCanvas.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = c1.X, Y1 = c1.Y, X2 = c2.X, Y2 = c2.Y,
                    Stroke = Brushes.Cyan, StrokeThickness = 1.5,
                    Opacity = 0.8,
                    StrokeDashArray = new DoubleCollection { 6, 3 }
                });
            }

            var cc = ToCanvas(iso.X, iso.Y);
            double scale = GetCanvasScale();
            double canR  = Math.Max(isoPxR * scale, 1);

            var circle = new Ellipse
            {
                Width  = canR * 2, Height = canR * 2,
                Stroke = Brushes.LimeGreen, StrokeThickness = 2.5,
                Fill   = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(35, 0, 255, 0))
            };
            Canvas.SetLeft(circle, cc.X - canR);
            Canvas.SetTop (circle, cc.Y - canR);
            OverlayCanvas.Children.Add(circle);

            const double arm = 9;
            OverlayCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = cc.X - arm, Y1 = cc.Y,
                X2 = cc.X + arm, Y2 = cc.Y,
                Stroke = Brushes.Red, StrokeThickness = 2
            });
            OverlayCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = cc.X, Y1 = cc.Y - arm,
                X2 = cc.X, Y2 = cc.Y + arm,
                Stroke = Brushes.Red, StrokeThickness = 2
            });
        }

        private void PlotProfile(
            double[] profile, bool isInverted, List<double> angles, double thresholdPct)
        {
            var plt = AnalysisPlot.Plot;
            plt.Clear();

            int N = profile.Length;
            double[] x = Enumerable.Range(0, N)
                .Select(i => i * 360.0 / N).ToArray();

            var trace = plt.Add.ScatterLine(x, profile);
            trace.LineStyle.Color = ScottPlot.Color.FromHex("#1f77b4");
            trace.LineStyle.Width = 1.5f;

            double maxV = profile.Max(), minV = profile.Min();
            double thr = isInverted
                ? maxV - (maxV - minV) * (thresholdPct / 100.0)
                : minV + (maxV - minV) * (thresholdPct / 100.0);

            var tl = plt.Add.HorizontalLine(thr);
            tl.LineStyle.Color   = ScottPlot.Colors.Orange;
            tl.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;

            foreach (var a in angles)
            {
                var vl = plt.Add.VerticalLine(a);
                vl.LineStyle.Color   = ScottPlot.Colors.Red;
                vl.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            }

            plt.Title("Angular Spoke Profile (averaged)");
            plt.XLabel("Angle (°)");
            plt.YLabel(isInverted ? "Intensity" : "Dose");
            plt.Axes.AutoScale();
            AnalysisPlot.Refresh();
        }

        // ════════════════════════════════════════════════════════════════════════════
        //  STATUS / PRINT / CLOSE
        // ════════════════════════════════════════════════════════════════════════════
        private void StatusRun(string msg, bool? success = null)
        {
            RunButton.Content    = msg;
            RunButton.Appearance = success switch
            {
                true  => Wpf.Ui.Controls.ControlAppearance.Success,
                false => Wpf.Ui.Controls.ControlAppearance.Danger,
                _     => Wpf.Ui.Controls.ControlAppearance.Primary
            };
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var doc = new FlowDocument
                {
                    PagePadding = new Thickness(40),
                    ColumnWidth = double.PositiveInfinity,
                    FontFamily  = new FontFamily("Segoe UI")
                };
                doc.Blocks.Add(new Paragraph(new Run("Star Shot Analysis Report"))
                {
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(173, 216, 230)),
                    Padding = new Thickness(6),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                var sec = new Section();
                sec.Blocks.Add(new Paragraph(
                    new Run($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}")));
                sec.Blocks.Add(new Paragraph(
                    new Run($"Isocenter Diameter: {IsoDiameterText.Text}"))
                { FontSize = 18, FontWeight = FontWeights.Bold });
                sec.Blocks.Add(new Paragraph(new Run(ResultSummaryText.Text)));
                doc.Blocks.Add(sec);

                // Star shot figure (image + overlays)
                var canvasBmp = CaptureElement(MainCanvas);
                var figImg = new System.Windows.Controls.Image
                {
                    Source = canvasBmp, Width = 500,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                doc.Blocks.Add(new Paragraph(new Run("Star Shot Image with Overlays"))
                { FontSize = 14, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center });
                doc.Blocks.Add(new BlockUIContainer(figImg));

                // Profile plot
                var plotBmp = CaptureElement(AnalysisPlot);
                var plotImg = new System.Windows.Controls.Image
                {
                    Source = plotBmp, Width = 600,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                doc.Blocks.Add(new Paragraph(new Run("Angular Spoke Profile"))
                { FontSize = 14, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center });
                doc.Blocks.Add(new BlockUIContainer(plotImg));

                var pd = new PrintDialog();
                if (pd.ShowDialog() == true)
                    pd.PrintDocument(
                        ((IDocumentPaginatorSource)doc).DocumentPaginator,
                        "Star Shot Report");
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message); }
        }

        private static BitmapSource CaptureElement(FrameworkElement element)
        {
            var rtb = new RenderTargetBitmap(
                (int)element.ActualWidth, (int)element.ActualHeight,
                96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);
            return rtb;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingCircle) return;
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
    }

    internal static class UiExt
    {
        public static T Also<T>(this T obj, Action<T> action)
        { action(obj); return obj; }
    }
}
