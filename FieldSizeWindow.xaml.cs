using ScottPlot;
using ScottPlot.WPF;
using System;
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

namespace FilmQA
{
    public partial class FieldSizeWindow : FluentWindow
    {
        private readonly double[,] _doseMap;
        private readonly double _dpi;
        private readonly double _mmPerPixelX;
        private readonly double _mmPerPixelY;
        private AppSettings _settings;

        // Cached results for reporting
        private double _lastFwhmX, _lastFwhmY, _lastStdX, _lastStdY, _lastCoverage;
        private string _lastMethod = "Maximum";
        private double _lastPlateauX, _lastPlateauY;

        // Alignment Dragging State
        private Point? _customCenterPixel = null;
        private double _customRotationAngle = 0.0;
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private double _rectStartLeft;
        private double _rectStartTop;

        public FieldSizeWindow(double[,] doseMap, double dpi, AppSettings settings, BitmapSource imageSource = null)
        {
            InitializeComponent();
            _doseMap = doseMap ?? throw new ArgumentNullException(nameof(doseMap));
            _dpi = dpi <= 0 ? 72.0 : dpi;
            _settings = settings;
            _mmPerPixelX = 25.4 / _dpi;
            _mmPerPixelY = 25.4 / _dpi;

            if (imageSource != null)
            {
                FilmImage.Source = imageSource;
                FilmImage.Width = doseMap.GetLength(1);
                FilmImage.Height = doseMap.GetLength(0);
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
            StatusText.Text = "Ready. Set Target Box above to align.";
            
            // Auto click apply reticle to start immediately if default 50x50 is ok
            ApplyReticleSize_Click(null, null);
        }

        private void ApplyReticleSize_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(TargetSizeXBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double tX)) tX = 50;
            if (!double.TryParse(TargetSizeYBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double tY)) tY = 50;

            double pxWidth = tX / _mmPerPixelX;
            double pxHeight = tY / _mmPerPixelY;

            TargetRect.Width = pxWidth;
            TargetRect.Height = pxHeight;

            int w = _doseMap.GetLength(1);
            int h = _doseMap.GetLength(0);

            if (TargetRect.Visibility != Visibility.Visible)
            {
                TargetRect.Visibility = Visibility.Visible;
                TargetCenter.Visibility = Visibility.Visible;
                // Center it initially
                Canvas.SetLeft(TargetRect, (w - pxWidth) / 2.0);
                Canvas.SetTop(TargetRect, (h - pxHeight) / 2.0);
            }

            UpdateCenterFromRect();
            OverlayCanvas.Focus(); // So keyboard shortcuts work
        }

        private void DPadUp_Click(object sender, RoutedEventArgs e) => NudgeRect(0, -1);
        private void DPadDown_Click(object sender, RoutedEventArgs e) => NudgeRect(0, 1);
        private void DPadLeft_Click(object sender, RoutedEventArgs e) => NudgeRect(-1, 0);
        private void DPadRight_Click(object sender, RoutedEventArgs e) => NudgeRect(1, 0);

        private void DPadRotateCW_Click(object sender, RoutedEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;
            TargetRotation.Angle += 0.5;
            _customRotationAngle = TargetRotation.Angle;
        }

        private void DPadRotateCCW_Click(object sender, RoutedEventArgs e)
        {
            if (TargetRect.Visibility != Visibility.Visible) return;
            TargetRotation.Angle -= 0.5;
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
                if (StatusText != null) StatusText.Text = "Calculated Center/Rot: " + (_customCenterPixel.HasValue ? $"({_customCenterPixel.Value.X:F0}, {_customCenterPixel.Value.Y:F0}) @ {_customRotationAngle:F1}°" : "Auto");
            }
            catch (Exception ex)
            {
                if (StatusText != null) StatusText.Text = $"Error: {ex.Message}";
            }
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

            double pixelCenterX = _customCenterPixel.HasValue ? _customCenterPixel.Value.X : (w - 1) / 2.0;
            double pixelCenterY = _customCenterPixel.HasValue ? _customCenterPixel.Value.Y : (h - 1) / 2.0;

            double angleRad = _customRotationAngle * Math.PI / 180.0;
            double cosA = Math.Cos(angleRad);
            double sinA = Math.Sin(angleRad);

            double stepX = _mmPerPixelX;
            double stepY = _mmPerPixelY;
            
            // X profiles span width 'w' physically, yielding 'w' points.
            int numPointsX = w;
            int numProfilesX = plateauY <= 0 ? 1 : Math.Max(1, (int)(plateauY / stepY) + 1);

            double[][] xProfiles = new double[numProfilesX][];
            var leftX = new double[numProfilesX];
            var rightX = new double[numProfilesX];
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

                    profile[j] = InterpolateBilinear(_doseMap, px, py);
                }
                xProfiles[i] = profile;

                double[] plateauSlice = GetPlateauSlice(profile, xCoords, plateauX, 0.0);
                double peak = SelectPeak(plateauSlice, method);
                double threshold = peak / 2.0;

                int maxIdx = Array.IndexOf(profile, profile.Max());
                
                int idxL = maxIdx;
                while (idxL > 0 && profile[idxL - 1] >= threshold) idxL--;
                leftX[i] = (idxL <= 0) ? xCoords[0] : InterpolateEdge(profile, xCoords, idxL - 1, idxL, threshold);

                int idxR = maxIdx;
                while (idxR < profile.Length - 1 && profile[idxR + 1] >= threshold) idxR++;
                rightX[i] = (idxR >= profile.Length - 1) ? xCoords[^1] : InterpolateEdge(profile, xCoords, idxR, idxR + 1, threshold);
            }

            // Y profiles
            int numPointsY = h;
            int numProfilesY = plateauX <= 0 ? 1 : Math.Max(1, (int)(plateauX / stepX) + 1);

            double[][] yProfiles = new double[numProfilesY][];
            var leftY = new double[numProfilesY];
            var rightY = new double[numProfilesY];
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

                    profile[j] = InterpolateBilinear(_doseMap, px, py);
                }
                yProfiles[i] = profile;

                double[] plateauSlice = GetPlateauSlice(profile, yCoords, plateauY, 0.0);
                double peak = SelectPeak(plateauSlice, method);
                double threshold = peak / 2.0;

                int maxIdx = Array.IndexOf(profile, profile.Max());
                
                int idxL = maxIdx;
                while (idxL > 0 && profile[idxL - 1] >= threshold) idxL--;
                leftY[i] = (idxL <= 0) ? yCoords[0] : InterpolateEdge(profile, yCoords, idxL - 1, idxL, threshold);

                int idxR = maxIdx;
                while (idxR < profile.Length - 1 && profile[idxR + 1] >= threshold) idxR++;
                rightY[i] = (idxR >= profile.Length - 1) ? yCoords[^1] : InterpolateEdge(profile, yCoords, idxR, idxR + 1, threshold);
            }

            // Stats
            var fwhmX = leftX.Zip(rightX, (l, r) => Math.Abs(r - l)).ToArray();
            var fwhmY = leftY.Zip(rightY, (l, r) => Math.Abs(r - l)).ToArray();

            double meanX = fwhmX.Length > 0 ? Math.Round(fwhmX.Average(), 3) : 0;
            double stdX = Math.Round(StdDev(fwhmX), 4);
            double meanY = fwhmY.Length > 0 ? Math.Round(fwhmY.Average(), 3) : 0;
            double stdY = Math.Round(StdDev(fwhmY), 4);

            double coverage = Math.Round(plateauX * plateauY, 2);
            if (ResultText != null)
            {
                ResultText.Text = $"FWHM X = {meanX:F3} ± {stdX:F4} mm\n" +
                                  $"FWHM Y = {meanY:F3} ± {stdY:F4} mm\n" +
                                  $"Coverage = {coverage:F2} mm²\n" +
                                  $"Method = {method}";
            }

            _lastFwhmX = meanX; _lastStdX = stdX;
            _lastFwhmY = meanY; _lastStdY = stdY;
            _lastCoverage = coverage;
            _lastMethod = method;
            _lastPlateauX = plateauX; _lastPlateauY = plateauY;

            if (leftX.Length > 0 && rightX.Length > 0)
                PlotProfiles(XPlot, xCoords, xProfiles, leftX.Average(), rightX.Average(), method, "XX Profile", meanX, stdX, plateauX / 2.0);
            
            if (leftY.Length > 0 && rightY.Length > 0)
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
    }
}
