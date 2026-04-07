using ScottPlot;
using ScottPlot.WPF;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Colors = ScottPlot.Colors;
using Wpf.Ui.Controls;

namespace FilmAnalysis
{
    public partial class JawSizeWindow : FluentWindow
    {
        private readonly double[,] _doseMap;
        private readonly double _dpi;
        private readonly double _mmPerPixelX;
        private readonly double _mmPerPixelY;
        private AppSettings _settings;

        public JawSizeWindow(double[,] doseMap, double dpi, AppSettings settings)
        {
            InitializeComponent();
            _doseMap = doseMap ?? throw new ArgumentNullException(nameof(doseMap));
            _dpi = dpi <= 0 ? 72.0 : dpi;
            _settings = settings;
            _mmPerPixelX = 25.4 / _dpi;
            _mmPerPixelY = 25.4 / _dpi;

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
            StatusText.Text = "Ready";
        }

        private void CalcButton_Click(object sender, RoutedEventArgs e)
        {
            if (_doseMap == null) return;

            if (!double.TryParse(PlateauXBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double plateauX)) plateauX = 0;
            if (!double.TryParse(PlateauYBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double plateauY)) plateauY = 0;
            plateauX = Math.Max(0, plateauX);
            plateauY = Math.Max(0, plateauY);

            // Save settings for next time
            _settings.LastPlateauX = plateauX;
            _settings.LastPlateauY = plateauY;
            _settings.LastJawMethod = ((ComboBoxItem)MethodBox.SelectedItem).Content.ToString();

            string method = ((MethodBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Maximum").ToLowerInvariant();

            try
            {
                ComputeAndPlot(plateauX, plateauY, method);
                StatusText.Text = "Calculated";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void ComputeAndPlot(double plateauX, double plateauY, string method)
        {
            int h = _doseMap.GetLength(0);
            int w = _doseMap.GetLength(1);

            // Coordinate vectors centered at 0 mm
            double[] xCoords = Enumerable.Range(0, w).Select(i => (i - (w - 1) / 2.0) * _mmPerPixelX).ToArray();
            double[] yCoords = Enumerable.Range(0, h).Select(i => (i - (h - 1) / 2.0) * _mmPerPixelY).ToArray();

            int centerXIdx = Array.IndexOf(xCoords.Select(v => Math.Abs(v)).ToArray(), xCoords.Select(v => Math.Abs(v)).Min());
            int centerYIdx = Array.IndexOf(yCoords.Select(v => Math.Abs(v)).ToArray(), yCoords.Select(v => Math.Abs(v)).Min());

            double centerX = xCoords[centerXIdx];
            double centerY = yCoords[centerYIdx];

            // rows for X profiles use plateauY; cols for Y profiles use plateauX
            var validRowIdx = Enumerable.Range(0, h)
                .Where(r => yCoords[r] >= centerY - plateauY / 2.0 && yCoords[r] <= centerY + plateauY / 2.0)
                .ToArray();
            var validColIdx = Enumerable.Range(0, w)
                .Where(c => xCoords[c] >= centerX - plateauX / 2.0 && xCoords[c] <= centerX + plateauX / 2.0)
                .ToArray();

            if (validRowIdx.Length == 0) validRowIdx = Enumerable.Range(0, h).ToArray();
            if (validColIdx.Length == 0) validColIdx = Enumerable.Range(0, w).ToArray();

            var leftX = new double[validRowIdx.Length];
            var rightX = new double[validRowIdx.Length];
            var leftY = new double[validColIdx.Length];
            var rightY = new double[validColIdx.Length];

            double[][] xProfiles = new double[validRowIdx.Length][];
            double[][] yProfiles = new double[validColIdx.Length][];

            // X profiles (rows)
            for (int i = 0; i < validRowIdx.Length; i++)
            {
                int r = validRowIdx[i];
                var profile = new double[w];
                for (int c = 0; c < w; c++) profile[c] = _doseMap[r, c];
                xProfiles[i] = profile;

                double[] plateauSlice = GetPlateauSlice(profile, xCoords, plateauX, centerX);
                double peak = SelectPeak(plateauSlice, method);
                double threshold = peak / 2.0;

                int maxIdx = Array.IndexOf(profile, profile.Max());
                int idxLeft = Array.FindIndex(profile, v => v >= threshold);
                leftX[i] = (idxLeft <= 0) ? xCoords[0] : InterpolateEdge(profile, xCoords, idxLeft - 1, idxLeft, threshold);

                int idxRightRel = Array.FindIndex(profile.Skip(maxIdx).ToArray(), v => v < threshold);
                if (idxRightRel < 0)
                    rightX[i] = xCoords[^1];
                else
                {
                    int g2 = maxIdx + idxRightRel;
                    int g1 = Math.Max(0, g2 - 1);
                    rightX[i] = InterpolateEdge(profile, xCoords, g1, g2, threshold);
                }
            }

            // Y profiles (columns)
            for (int j = 0; j < validColIdx.Length; j++)
            {
                int c = validColIdx[j];
                var profile = new double[h];
                for (int r = 0; r < h; r++) profile[r] = _doseMap[r, c];
                yProfiles[j] = profile;

                double[] plateauSlice = GetPlateauSlice(profile, yCoords, plateauY, centerY);
                double peak = SelectPeak(plateauSlice, method);
                double threshold = peak / 2.0;

                int maxIdx = Array.IndexOf(profile, profile.Max());
                int idxLeft = Array.FindIndex(profile, v => v >= threshold);
                leftY[j] = (idxLeft <= 0) ? yCoords[0] : InterpolateEdge(profile, yCoords, idxLeft - 1, idxLeft, threshold);

                int idxRightRel = Array.FindIndex(profile.Skip(maxIdx).ToArray(), v => v < threshold);
                if (idxRightRel < 0)
                    rightY[j] = yCoords[^1];
                else
                {
                    int g2 = maxIdx + idxRightRel;
                    int g1 = Math.Max(0, g2 - 1);
                    rightY[j] = InterpolateEdge(profile, yCoords, g1, g2, threshold);
                }
            }

            // Stats
            var fwhmX = leftX.Zip(rightX, (l, r) => Math.Abs(r - l)).ToArray();
            var fwhmY = leftY.Zip(rightY, (l, r) => Math.Abs(r - l)).ToArray();

            double meanX = Math.Round(fwhmX.Average(), 3);
            double stdX = Math.Round(StdDev(fwhmX), 4);
            double meanY = Math.Round(fwhmY.Average(), 3);
            double stdY = Math.Round(StdDev(fwhmY), 4);

            double coverage = Math.Round(plateauX * plateauY, 2);
            ResultText.Text = $"FWHM X = {meanX:F3} ± {stdX:F4} mm\n" +
                              $"FWHM Y = {meanY:F3} ± {stdY:F4} mm\n" +
                              $"Coverage = {coverage:F2} mm²\n" +
                              $"Method = {method}";

            PlotProfiles(XPlot, xCoords, xProfiles, leftX.Average(), rightX.Average(), method, "XX Profile", meanX, stdX, plateauX / 2.0);
            PlotProfiles(YPlot, yCoords, yProfiles, leftY.Average(), rightY.Average(), method, "YY Profile", meanY, stdY, plateauY / 2.0);
        }

        private static double[] GetPlateauSlice(double[] profile, double[] coords, double plateauWidth, double center)
        {
            if (plateauWidth <= 0) return profile;
            var idx = Enumerable.Range(0, profile.Length)
                .Where(i => coords[i] >= center - plateauWidth / 2.0 && coords[i] <= center + plateauWidth / 2.0)
                .ToArray();
            return idx.Length == 0 ? profile : idx.Select(i => profile[i]).ToArray();
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

        private static double StdDev(double[] data)
        {
            if (data == null || data.Length == 0) return 0;
            double mean = data.Average();
            double variance = data.Select(v => (v - mean) * (v - mean)).Average();
            return Math.Sqrt(variance);
        }

        private static void PlotProfiles(WpfPlot plot, double[] coords, double[][] profiles, double leftEdge, double rightEdge, string method, string title, double mean, double std, double plateauHalfWidth)
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
            double threshold = maxVal / 2.0;

            var hLine = plt.Add.HorizontalLine(threshold);
            hLine.LineStyle.Color = ScottPlot.Colors.Black;
            hLine.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            hLine.LegendText = "50%";

            var vLineL = plt.Add.VerticalLine(leftEdge);
            vLineL.LineStyle.Color = ScottPlot.Colors.Red;
            vLineL.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            vLineL.LegendText = "Left";

            var vLineR = plt.Add.VerticalLine(rightEdge);
            vLineR.LineStyle.Color = ScottPlot.Colors.ForestGreen;
            vLineR.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            vLineR.LegendText = "Right";

            plt.Title($"{title} | FWHM = {mean:F3} ± {std:F4} mm ({method})");
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
    }
}
