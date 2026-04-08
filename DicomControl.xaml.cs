using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Win32;

namespace FilmQA
{
    public class DoseExtractedEventArgs : EventArgs
    {
        public double[,] DoseMap { get; set; } = new double[0, 0];
        public double SpacingX { get; set; }
        public double SpacingY { get; set; }
        public string FileName { get; set; } = "";
        public double MaxDose { get; set; }
        
        // Physical Reference (Isocenter/Selected Point)
        public double RefX { get; set; }
        public double RefY { get; set; }
        public double RefZ { get; set; }
        public string PlaneOrientation { get; set; } = "Z"; // Z (Axial), Y (Coronal), X (Sagittal)

        // Physical coordinates of doseMap[0,0]
        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public double SpacingYSign { get; set; } = 1.0; // -1 for Z-flipped planes (Coronal/Sagittal)
    }

    public partial class DicomControl : UserControl
    {
        public event EventHandler<DoseExtractedEventArgs>? DosePlaneExtracted;

        private float[,,]? _doseVolume; // [frame/Z, row/Y, col/X]
        private double[]? _zPositions;
        private double[]? _xPositions;
        private double[]? _yPositions;
        private double _pixelSpacingX;
        private double _pixelSpacingY;
        private string? _loadedFilePath;
        private string? _patientName;
        private double _doseGridScaling;
        private double _isoX = 0, _isoY = 0, _isoZ = 0;

        // Structure Set
        private List<StructureContour> _structures = new();

        private int _currentX, _currentY, _currentZ;
        private int _maxDoseX, _maxDoseY, _maxDoseZ;
        private bool _isUpdatingSliders = false;

        public DicomControl()
        {
            InitializeComponent();
        }

        private void LoadDicomSet_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "DICOM files|*.dcm;*.dicom|All files|*.*",
                Multiselect = true,
                Title = "Select RT Dose (RD), Structure Set (RS), and/or Plan (RP) files"
            };
            if (dlg.ShowDialog() != true || dlg.FileNames.Length == 0) return;

            foreach (var file in dlg.FileNames)
            {
                try
                {
                    var ds = DicomFile.Open(file).Dataset;
                    string modality = ds.GetSingleValueOrDefault(DicomTag.Modality, "");

                    switch (modality)
                    {
                        case "RTDOSE":
                            LoadDicomFile(file);
                            RdStatus.Text = $"\u25cf RD: {System.IO.Path.GetFileName(file)}";
                            RdStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
                            break;
                        case "RTSTRUCT":
                            LoadStructureSet(ds);
                            RsStatus.Text = $"\u25cf RS: {_structures.Count} structures";
                            RsStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
                            break;
                        case "RTPLAN":
                            LoadRTPlan(ds);
                            RpStatus.Text = $"\u25cf RP: Isocenter loaded";
                            RpStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
                            break;
                        default:
                            // Try as dose if modality is missing
                            LoadDicomFile(file);
                            RdStatus.Text = $"\u25cf RD: {System.IO.Path.GetFileName(file)}";
                            RdStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load {System.IO.Path.GetFileName(file)}: {ex.Message}", "Error");
                }
            }

            StatusText.Text = "DICOM set loaded";
        }

        // Keep legacy single-file support
        private void LoadDicom_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "DICOM files|*.dcm;*.dicom|All files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                try { LoadDicomFile(dlg.FileName); }
                catch (Exception ex) { MessageBox.Show($"Failed to load DICOM: {ex.Message}", "Error"); }
            }
        }

        private void LoadDicomFile(string filePath)
        {
            _loadedFilePath = filePath;
            var dataset = DicomFile.Open(filePath).Dataset;

            _patientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, "N/A");
            MetaPatient.Text = _patientName;

            string patientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, "N/A");
            MetaPatientId.Text = patientId;

            int rows = dataset.GetSingleValue<ushort>(DicomTag.Rows);
            int cols = dataset.GetSingleValue<ushort>(DicomTag.Columns);
            int frames = dataset.GetSingleValueOrDefault(DicomTag.NumberOfFrames, 1);
            MetaDim.Text = $"{cols} x {rows} x {frames}";

            var pixelSpacing = dataset.GetValues<double>(DicomTag.PixelSpacing);
            _pixelSpacingX = pixelSpacing[1];
            _pixelSpacingY = pixelSpacing[0];
            MetaSpacing.Text = $"{_pixelSpacingX:F2}, {_pixelSpacingY:F2} mm";

            var imagePos = dataset.GetValues<double>(DicomTag.ImagePositionPatient);
            double startX = imagePos[0];
            double startY = imagePos[1];
            double startZ = imagePos[2];

            // 1. Setup Positions (LPS)
            _xPositions = Enumerable.Range(0, cols).Select(i => startX + i * _pixelSpacingX).ToArray();
            _yPositions = Enumerable.Range(0, rows).Select(i => startY + i * _pixelSpacingY).ToArray();
            
            _zPositions = new double[frames];
            if (dataset.Contains(DicomTag.GridFrameOffsetVector))
            {
                var offsets = dataset.GetValues<double>(DicomTag.GridFrameOffsetVector);
                for (int i = 0; i < frames; i++) _zPositions[i] = startZ + offsets[i];
            }
            else
            {
                double thickness = dataset.GetSingleValueOrDefault(DicomTag.SliceThickness, _pixelSpacingX);
                for (int i = 0; i < frames; i++) _zPositions[i] = startZ + (i * thickness);
                MetaThk.Text = $"{thickness:F2} mm";
            }
            if (frames > 1 && _zPositions.Length > 1) MetaThk.Text = $"{Math.Abs(_zPositions[1] - _zPositions[0]):F2} mm (Avg)";

            _doseGridScaling = dataset.GetSingleValueOrDefault(DicomTag.DoseGridScaling, 1.0);

            // 2. Load Pixel Data (Robust check for 16 vs 32 bit)
            _doseVolume = new float[frames, rows, cols];
            
            try
            {
                // Try reading as ushort first (most common for RT Dose OW/OB)
                if (dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, (ushort)16) <= 16)
                {
                    var pixelData = dataset.GetValues<ushort>(DicomTag.PixelData);
                    for (int f = 0; f < frames; f++)
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                                _doseVolume[f, r, c] = pixelData[f * rows * cols + r * cols + c];
                }
                else
                {
                    var pixelData = dataset.GetValues<int>(DicomTag.PixelData);
                    for (int f = 0; f < frames; f++)
                        for (int r = 0; r < rows; r++)
                            for (int c = 0; c < cols; c++)
                                _doseVolume[f, r, c] = pixelData[f * rows * cols + r * cols + c];
                }
            }
            catch
            {
                // Last resort: raw byte access and manual conversion
                var buffer = dataset.GetDicomItem<DicomElement>(DicomTag.PixelData).Buffer.Data;
                bool is32 = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, (ushort)16) == 32;
                
                for (int f = 0; f < frames; f++)
                {
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            int idx = f * rows * cols + r * cols + c;
                            if (is32) _doseVolume[f, r, c] = BitConverter.ToInt32(buffer, idx * 4);
                            else _doseVolume[f, r, c] = BitConverter.ToUInt16(buffer, idx * 2);
                        }
                    }
                }
            }

            double maxVal = -1;
            _maxDoseX = _maxDoseY = _maxDoseZ = 0;
            for (int f = 0; f < frames; f++)
            {
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (_doseVolume[f, r, c] > maxVal)
                        {
                            maxVal = _doseVolume[f, r, c];
                            _maxDoseZ = f; _maxDoseY = r; _maxDoseX = c;
                        }
                    }
                }
            }
            MetaMaxDose.Text = $"{maxVal * _doseGridScaling * 100.0:F2} cGy";

            // 3. UI Setup
            _isUpdatingSliders = true;
            ZSlider.Maximum = frames - 1; ZSlider.Value = frames / 2;
            YSlider.Maximum = rows - 1; YSlider.Value = rows / 2;
            XSlider.Maximum = cols - 1; XSlider.Value = cols / 2;

            // Update Input Ranges (MM)
            ZCoordInput.Minimum = Math.Min(_zPositions![0], _zPositions![frames - 1]);
            ZCoordInput.Maximum = Math.Max(_zPositions![0], _zPositions![frames - 1]);
            ZCoordInput.SmallChange = Math.Abs(_zPositions![1] - _zPositions![0]);

            YCoordInput.Minimum = Math.Min(_yPositions![0], _yPositions![rows - 1]);
            YCoordInput.Maximum = Math.Max(_yPositions![0], _yPositions![rows - 1]);
            YCoordInput.SmallChange = _pixelSpacingY;

            XCoordInput.Minimum = Math.Min(_xPositions![0], _xPositions![cols - 1]);
            XCoordInput.Maximum = Math.Max(_xPositions![0], _xPositions![cols - 1]);
            XCoordInput.SmallChange = _pixelSpacingX;
            
            _isUpdatingSliders = false;

            _currentX = (int)XSlider.Value;
            _currentY = (int)YSlider.Value;
            _currentZ = (int)ZSlider.Value;

            ExtractPlaneButton.IsEnabled = true;
            ExportPlaneButton.IsEnabled = true;
            GoToMaxDoseBtn.IsEnabled = true;

            UpdateAllViews();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_doseVolume == null || _isUpdatingSliders) return;
            _currentX = (int)XSlider.Value;
            _currentY = (int)YSlider.Value;
            _currentZ = (int)ZSlider.Value;
            UpdateAllViews();
        }

        private void CoordInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            SyncCoordInputToSlider((Wpf.Ui.Controls.NumberBox)sender);
        }

        private void CoordInput_ValueChanged(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null || _isUpdatingSliders) return;
            SyncCoordInputToSlider((Wpf.Ui.Controls.NumberBox)sender);
        }

        private void SyncCoordInputToSlider(Wpf.Ui.Controls.NumberBox input)
        {
            if (_doseVolume == null || _zPositions == null || _yPositions == null || _xPositions == null) return;

            _isUpdatingSliders = true;
            try
            {
                bool tps = TpsModeToggle?.IsChecked == true;
                double val = input.Value ?? 0.0;

                if (input == ZCoordInput) ZSlider.Value = FindNearestIndex(val + (tps ? (IsoZInput.Value ?? 0) : 0), _zPositions);
                else if (input == YCoordInput) YSlider.Value = FindNearestIndex(val + (tps ? (IsoYInput.Value ?? 0) : 0), _yPositions);
                else if (input == XCoordInput) XSlider.Value = FindNearestIndex(val + (tps ? (IsoXInput.Value ?? 0) : 0), _xPositions);

                _currentX = (int)XSlider.Value;
                _currentY = (int)YSlider.Value;
                _currentZ = (int)ZSlider.Value;
                UpdateAllViews();
            }
            finally
            {
                _isUpdatingSliders = false;
            }
        }

        private int FindNearestIndex(double target, double[] positions)
        {
            int bestIdx = 0;
            double minDist = double.MaxValue;
            for (int i = 0; i < positions.Length; i++)
            {
                double dist = Math.Abs(positions[i] - target);
                if (dist < minDist) { minDist = dist; bestIdx = i; }
            }
            return bestIdx;
        }

        private void UpdateAllViews()
        {
            if (_doseVolume == null) return;

            bool wasUpdating = _isUpdatingSliders;
            if (!wasUpdating) _isUpdatingSliders = true;

            bool tps = TpsModeToggle?.IsChecked == true;
            double offZ = tps ? (IsoZInput.Value ?? 0) : 0;
            double offY = tps ? (IsoYInput.Value ?? 0) : 0;
            double offX = tps ? (IsoXInput.Value ?? 0) : 0;

            // Bug #1 fix: Update Min/Max to reflect TPS-shifted range
            int frames = _doseVolume.GetLength(0), rows = _doseVolume.GetLength(1), cols = _doseVolume.GetLength(2);
            ZCoordInput.Minimum = Math.Min(_zPositions![0], _zPositions![frames - 1]) - offZ;
            ZCoordInput.Maximum = Math.Max(_zPositions![0], _zPositions![frames - 1]) - offZ;
            YCoordInput.Minimum = Math.Min(_yPositions![0], _yPositions![rows - 1]) - offY;
            YCoordInput.Maximum = Math.Max(_yPositions![0], _yPositions![rows - 1]) - offY;
            XCoordInput.Minimum = Math.Min(_xPositions![0], _xPositions![cols - 1]) - offX;
            XCoordInput.Maximum = Math.Max(_xPositions![0], _xPositions![cols - 1]) - offX;

            ZCoordInput.Value = _zPositions![_currentZ] - offZ;
            YCoordInput.Value = _yPositions![_currentY] - offY;
            XCoordInput.Value = _xPositions![_currentX] - offX;

            if (!wasUpdating) _isUpdatingSliders = false;

            string prefix = tps ? "TPS" : "LPS";
            LpsCoordText.Text = $"{prefix}: X: {XCoordInput.Value:F1}, Y: {YCoordInput.Value:F1}, Z: {ZCoordInput.Value:F1}";

            // Dose at Cursor
            float doseVal = _doseVolume[_currentZ, _currentY, _currentX] * (float)_doseGridScaling * 100.0f;
            CursorDoseText.Text = $"{doseVal:F2} cGy";

            UpdateAxial();
            UpdateCoronal();
            UpdateSagittal();
            UpdateCrosshairs();
            DrawContours();
        }

        private void UpdateAxial()
        {
            int rows = _doseVolume!.GetLength(1);
            int cols = _doseVolume!.GetLength(2);
            float max = FindLocalMax(_currentZ, -1, -1);
            byte[] pixels = GenerateHeatmapPixels(_doseVolume, _currentZ, -1, -1, max);
            AxialImage.Source = BitmapSource.Create(cols, rows, 96, 96, PixelFormats.Bgra32, null, pixels, cols * 4);
        }

        private void UpdateCoronal()
        {
            int frames = _doseVolume!.GetLength(0);
            int rows = _doseVolume!.GetLength(1);
            int cols = _doseVolume!.GetLength(2);
            
            // X-Z Plane for Coronal
            float max = FindLocalMax(-1, _currentY, -1);
            byte[] pixels = GenerateHeatmapPixels(_doseVolume, -1, _currentY, -1, max);

            // Correct Aspect Ratio: Z spacing might be different from X spacing
            double zSpacing = _zPositions!.Length > 1
                ? Math.Abs(_zPositions[1] - _zPositions[0])
                : _pixelSpacingX; // fallback for single-slice volumes

            var bitmap = BitmapSource.Create(cols, frames, 96, 96, PixelFormats.Bgra32, null, pixels, cols * 4);
            CoronalImage.Source = bitmap;
        }

        private void UpdateSagittal()
        {
            int frames = _doseVolume!.GetLength(0);
            int rows = _doseVolume!.GetLength(1);
            int cols = _doseVolume!.GetLength(2);

            // Y-Z Plane for Sagittal
            float max = FindLocalMax(-1, -1, _currentX);
            byte[] pixels = GenerateHeatmapPixels(_doseVolume, -1, -1, _currentX, max);

            // Correct Aspect Ratio: Z spacing vs Y spacing
            double zSpacing = _zPositions!.Length > 1
                ? Math.Abs(_zPositions[1] - _zPositions[0])
                : _pixelSpacingY; // fallback for single-slice volumes

            var bitmap = BitmapSource.Create(rows, frames, 96, 96, PixelFormats.Bgra32, null, pixels, rows * 4);
            SagittalImage.Source = bitmap;
        }

        private void UpdateCrosshairs()
        {
            if (_doseVolume == null) return;
            int dCols = _doseVolume.GetLength(2), dRows = _doseVolume.GetLength(1), dFrames = _doseVolume.GetLength(0);

            // --- Axial (XY) ---
            double aw = AxialCanvas.ActualWidth, ah = AxialCanvas.ActualHeight;
            if (aw > 0 && ah > 0)
            {
                double imgAsp = (double)dCols / dRows;
                double canAsp = aw / ah;
                double rw, rh, ox, oy;
                if (imgAsp > canAsp) { rw = aw; rh = aw / imgAsp; ox = 0; oy = (ah - rh) / 2; }
                else { rh = ah; rw = ah * imgAsp; ox = (aw - rw) / 2; oy = 0; }

                double fx = XSlider.Value / Math.Max(XSlider.Maximum, 1);
                double fy = YSlider.Value / Math.Max(YSlider.Maximum, 1);
                AxialLineX.X1 = AxialLineX.X2 = ox + fx * rw;
                AxialLineX.Y1 = oy; AxialLineX.Y2 = oy + rh;
                AxialLineY.Y1 = AxialLineY.Y2 = oy + fy * rh;
                AxialLineY.X1 = ox; AxialLineY.X2 = ox + rw;
            }

            // --- Coronal (XZ) ---
            double cw = CoronalCanvas.ActualWidth, ch = CoronalCanvas.ActualHeight;
            if (cw > 0 && ch > 0)
            {
                double imgAsp = (double)dCols / dFrames;
                double canAsp = cw / ch;
                double rw, rh, ox, oy;
                if (imgAsp > canAsp) { rw = cw; rh = cw / imgAsp; ox = 0; oy = (ch - rh) / 2; }
                else { rh = ch; rw = ch * imgAsp; ox = (cw - rw) / 2; oy = 0; }

                double fx = XSlider.Value / Math.Max(XSlider.Maximum, 1);
                double fz = ZSlider.Value / Math.Max(ZSlider.Maximum, 1);
                CoronalLineX.X1 = CoronalLineX.X2 = ox + fx * rw;
                CoronalLineX.Y1 = oy; CoronalLineX.Y2 = oy + rh;
                CoronalLineZ.Y1 = CoronalLineZ.Y2 = oy + (1.0 - fz) * rh;
                CoronalLineZ.X1 = ox; CoronalLineZ.X2 = ox + rw;
            }

            // --- Sagittal (YZ) ---
            double sw = SagittalCanvas.ActualWidth, sh = SagittalCanvas.ActualHeight;
            if (sw > 0 && sh > 0)
            {
                double imgAsp = (double)dRows / dFrames;
                double canAsp = sw / sh;
                double rw, rh, ox, oy;
                if (imgAsp > canAsp) { rw = sw; rh = sw / imgAsp; ox = 0; oy = (sh - rh) / 2; }
                else { rh = sh; rw = sh * imgAsp; ox = (sw - rw) / 2; oy = 0; }

                double fy = YSlider.Value / Math.Max(YSlider.Maximum, 1);
                double fz = ZSlider.Value / Math.Max(ZSlider.Maximum, 1);
                SagittalLineY.X1 = SagittalLineY.X2 = ox + fy * rw;
                SagittalLineY.Y1 = oy; SagittalLineY.Y2 = oy + rh;
                SagittalLineZ.Y1 = SagittalLineZ.Y2 = oy + (1.0 - fz) * rh;
                SagittalLineZ.X1 = ox; SagittalLineZ.X2 = ox + rw;
            }
        }

        private float FindLocalMax(int z, int y, int x)
        {
            int f = _doseVolume!.GetLength(0), r = _doseVolume!.GetLength(1), c = _doseVolume!.GetLength(2);
            float max = 0;
            if (z != -1)
            {
                for (int i = 0; i < r; i++) for (int j = 0; j < c; j++) if (_doseVolume[z, i, j] > max) max = _doseVolume[z, i, j];
            }
            else if (y != -1)
            {
                for (int i = 0; i < f; i++) for (int j = 0; j < c; j++) if (_doseVolume[i, y, j] > max) max = _doseVolume[i, y, j];
            }
            else if (x != -1)
            {
                for (int i = 0; i < f; i++) for (int j = 0; j < r; j++) if (_doseVolume[i, j, x] > max) max = _doseVolume[i, j, x];
            }
            return max == 0 ? 1 : max;
        }

        private byte[] GenerateHeatmapPixels(float[,,] vol, int fixedZ, int fixedY, int fixedX, float max)
        {
            int f = vol.GetLength(0), r = vol.GetLength(1), c = vol.GetLength(2);
            int outRows, outCols;
            if (fixedZ != -1) { outRows = r; outCols = c; }
            else if (fixedY != -1) { outRows = f; outCols = c; }
            else { outRows = f; outCols = r; }

            byte[] pixels = new byte[outRows * outCols * 4];
            for (int i = 0; i < outRows; i++)
            {
                for (int j = 0; j < outCols; j++)
                {
                    float val;
                    if (fixedZ != -1) val = vol[fixedZ, i, j];
                    else if (fixedY != -1) val = vol[f - 1 - i, fixedY, j]; // Flip Z for display
                    else val = vol[f - 1 - i, j, fixedX]; // Flip Z for display
                    
                    Color col = GetJetColor(val / max);
                    int idx = (i * outCols + j) * 4;
                    pixels[idx] = col.B; pixels[idx + 1] = col.G; pixels[idx + 2] = col.R; pixels[idx + 3] = 255;
                }
            }
            return pixels;
        }

        private Color GetJetColor(float v)
        {
            v = Math.Clamp(v, 0, 1);
            byte r = (byte)(255 * Math.Clamp(Math.Min(4 * v - 1.5, -4 * v + 4.5), 0, 1));
            byte g = (byte)(255 * Math.Clamp(Math.Min(4 * v - 0.5, -4 * v + 3.5), 0, 1));
            byte b = (byte)(255 * Math.Clamp(Math.Min(4 * v + 0.5, -4 * v + 2.5), 0, 1));
            return Color.FromRgb(r, g, b);
        }

        private void ExtractSelectedPlane_Click(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null || PlaneCombo.SelectedItem is not ComboBoxItem item) return;
            string axis = item.Tag?.ToString() ?? "Z";
            ExtractPlaneForAxis(axis);
        }

        private void ExportPlane_Click(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null || PlaneCombo.SelectedItem is not ComboBoxItem item) return;
            string axis = item.Tag?.ToString() ?? "Z";
            
            var plane = PrepareDosePlane(axis);
            if (plane.DoseMap == null) return;

            int height = plane.DoseMap.GetLength(0);
            int width = plane.DoseMap.GetLength(1);
            
            // Map spacing directly backwards to physical Film DPI expectations
            double dpiX = 25.4 / plane.SpacingX;
            double dpiY = 25.4 / plane.SpacingY;

            var dlg = new Microsoft.Win32.SaveFileDialog 
            { 
                 Filter = "Text Dose Maps (*.txt)|*.txt",
                 FileName = $"{System.IO.Path.GetFileNameWithoutExtension(_loadedFilePath)}_{plane.Suffix}.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                using var writer = new System.IO.StreamWriter(dlg.FileName);
                writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd}");
                writer.WriteLine($"DPI_X: {dpiX:F3}");
                writer.WriteLine($"DPI_Y: {dpiY:F3}");
                writer.WriteLine($"Origin_X: {plane.OriginX:F3}");
                writer.WriteLine($"Origin_Y: {plane.OriginY:F3}");
                writer.WriteLine($"Ref_X: {plane.RefX:F3}");
                writer.WriteLine($"Ref_Y: {plane.RefY:F3}");
                writer.WriteLine($"Ref_Z: {plane.RefZ:F3}");
                writer.WriteLine($"Plane_Orientation: {axis}");
                writer.WriteLine($"Spacing_Y_Sign: {plane.SpacingYSign:F1}");
                writer.WriteLine($"Interpolation: 1");
                writer.WriteLine($"X Res: {width}");
                writer.WriteLine($"Y Res: {height}");
                writer.WriteLine("Array Start:");

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        writer.Write(plane.DoseMap[y, x].ToString("F5", System.Globalization.CultureInfo.InvariantCulture));
                        if (x < width - 1) writer.Write("\t");
                    }
                    writer.WriteLine();
                }
                
                System.Windows.MessageBox.Show("Plane exported successfully.", "Export Complete");
            }
        }

        private void ExtractPlane_Click(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null) return;
            string axis = ((Button)sender).Tag?.ToString() ?? "Z";
            ExtractPlaneForAxis(axis);
        }

        private class PreparedPlane
        {
            public double[,] DoseMap;
            public double SpacingX, SpacingY;
            public double OriginX, OriginY;
            public double RefX, RefY, RefZ;
            public double SpacingYSign;
            public string Suffix;
        }

        private PreparedPlane PrepareDosePlane(string axis)
        {
            int rows, cols;
            double[,] doseMap;
            double spX, spY;
            string suffix;

            if (axis == "Z")
            {
                rows = _doseVolume.GetLength(1); cols = _doseVolume.GetLength(2);
                doseMap = new double[rows, cols];
                for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) doseMap[r, c] = _doseVolume[_currentZ, r, c] * _doseGridScaling * 100.0;
                spX = _pixelSpacingX; spY = _pixelSpacingY; suffix = $"Axial_Z{_zPositions?[_currentZ]:F1}";
            }
            else if (axis == "Y")
            {
                int f = _doseVolume.GetLength(0); cols = _doseVolume.GetLength(2);
                doseMap = new double[f, cols];
                double zSpacing = Math.Abs(_zPositions![1] - _zPositions![0]);
                for (int r = 0; r < f; r++) for (int c = 0; c < cols; c++) doseMap[r, c] = _doseVolume[f - 1 - r, _currentY, c] * _doseGridScaling * 100.0;
                spX = _pixelSpacingX; spY = zSpacing; suffix = $"Coronal_Y{_yPositions?[_currentY]:F1}";
            }
            else
            {
                int f = _doseVolume.GetLength(0); int yr = _doseVolume.GetLength(1);
                doseMap = new double[f, yr];
                double zSpacing = Math.Abs(_zPositions![1] - _zPositions![0]);
                for (int r = 0; r < f; r++) for (int c = 0; c < yr; c++) doseMap[r, c] = _doseVolume[f - 1 - r, c, _currentX] * _doseGridScaling * 100.0;
                spX = _pixelSpacingY; spY = zSpacing; suffix = $"Sagittal_X{_xPositions?[_currentX]:F1}";
            }

            double originX, originY;
            double spYSign = 1.0;
            if (axis == "Z") { originX = _xPositions?[0] ?? 0; originY = _yPositions?[0] ?? 0; }
            else if (axis == "Y") { originX = _xPositions?[0] ?? 0; originY = _zPositions?[_doseVolume.GetLength(0) - 1] ?? 0; spYSign = -1.0; }
            else { originX = _yPositions?[0] ?? 0; originY = _zPositions?[_doseVolume.GetLength(0) - 1] ?? 0; spYSign = -1.0; }

            return new PreparedPlane
            {
                DoseMap = doseMap,
                SpacingX = spX,
                SpacingY = spY,
                OriginX = originX,
                OriginY = originY,
                RefX = _xPositions?[_currentX] ?? 0,
                RefY = _yPositions?[_currentY] ?? 0,
                RefZ = _zPositions?[_currentZ] ?? 0,
                SpacingYSign = spYSign,
                Suffix = suffix
            };
        }

        private void ExtractPlaneForAxis(string axis)
        {
            var plane = PrepareDosePlane(axis);
            if (plane.DoseMap == null) return;

            double max = 0; foreach (var d in plane.DoseMap) if (d > max) max = d;

            DosePlaneExtracted?.Invoke(this, new DoseExtractedEventArgs
            {
                DoseMap = plane.DoseMap, 
                SpacingX = plane.SpacingX, 
                SpacingY = plane.SpacingY, 
                MaxDose = max,
                FileName = $"{System.IO.Path.GetFileNameWithoutExtension(_loadedFilePath)}_{plane.Suffix}.dcm",
                RefX = plane.RefX, RefY = plane.RefY, RefZ = plane.RefZ,
                PlaneOrientation = axis,
                OriginX = plane.OriginX,
                OriginY = plane.OriginY,
                SpacingYSign = plane.SpacingYSign
            });

            StatusText.Text = $"Extracted {axis} plane at {(_zPositions?[_currentZ]):F1} mm";
            MessageBox.Show($"Extracted {axis} plane to dosimetry tool.", "Extraction Successful");
        }

        private void SetIsocenter_Click(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null) return;
            IsoXInput.Value = _xPositions![_currentX];
            IsoYInput.Value = _yPositions![_currentY];
            IsoZInput.Value = _zPositions![_currentZ];
            UpdateAllViews();
        }

        private void TpsMode_Checked(object sender, RoutedEventArgs e) => UpdateAllViews();
        private void IsoInput_ValueChanged(object sender, RoutedEventArgs e) => UpdateAllViews();

        // ===== RT Structure Set Parsing =====

        private void LoadStructureSet(DicomDataset ds)
        {
            _structures.Clear();

            // 1. Read ROI names
            var roiNames = new Dictionary<int, string>();
            if (ds.Contains(DicomTag.StructureSetROISequence))
            {
                foreach (var item in ds.GetSequence(DicomTag.StructureSetROISequence))
                {
                    int num = item.GetSingleValue<int>(DicomTag.ROINumber);
                    string name = item.GetSingleValueOrDefault(DicomTag.ROIName, $"ROI_{num}");
                    roiNames[num] = name;
                }
            }

            // 2. Read display colors from ROIObservationsSequence or ROIContourSequence
            var roiColors = new Dictionary<int, Color>();

            // 3. Read contour data
            if (ds.Contains(DicomTag.ROIContourSequence))
            {
                foreach (var roiItem in ds.GetSequence(DicomTag.ROIContourSequence))
                {
                    int refNum = roiItem.GetSingleValue<int>(DicomTag.ReferencedROINumber);
                    string name = roiNames.ContainsKey(refNum) ? roiNames[refNum] : $"ROI_{refNum}";

                    // Try to get color
                    Color color = Colors.Yellow;
                    if (roiItem.Contains(DicomTag.ROIDisplayColor))
                    {
                        try
                        {
                            var rgb = roiItem.GetValues<int>(DicomTag.ROIDisplayColor);
                            if (rgb.Length >= 3) color = Color.FromRgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
                        }
                        catch { /* Use default */ }
                    }

                    var structure = new StructureContour
                    {
                        Name = name,
                        ROINumber = refNum,
                        DisplayColor = color
                    };

                    if (roiItem.Contains(DicomTag.ContourSequence))
                    {
                        foreach (var contourItem in roiItem.GetSequence(DicomTag.ContourSequence))
                        {
                            if (!contourItem.Contains(DicomTag.ContourData)) continue;
                            var data = contourItem.GetValues<double>(DicomTag.ContourData);
                            if (data.Length < 9) continue; // Need at least 3 points

                            var points = new Point[data.Length / 3];
                            double z = data[2]; // Z coordinate of this slice

                            for (int i = 0; i < data.Length; i += 3)
                            {
                                double x = data[i], y = data[i + 1];
                                points[i / 3] = new Point(x, y);

                                // Update bounding box
                                if (x < structure.MinX) structure.MinX = x;
                                if (x > structure.MaxX) structure.MaxX = x;
                                if (y < structure.MinY) structure.MinY = y;
                                if (y > structure.MaxY) structure.MaxY = y;
                            }
                            if (z < structure.MinZ) structure.MinZ = z;
                            if (z > structure.MaxZ) structure.MaxZ = z;

                            double zKey = Math.Round(z, 1);
                            if (!structure.SliceContours.ContainsKey(zKey))
                                structure.SliceContours[zKey] = new List<Point[]>();
                            structure.SliceContours[zKey].Add(points);
                        }
                    }

                    if (structure.SliceContours.Count > 0)
                        _structures.Add(structure);
                }
            }

            // Populate dropdown
            StructureCombo.ItemsSource = _structures;
            if (_structures.Count > 0)
                GoToCenterBtn.IsEnabled = true;

            StatusText.Text = $"Loaded {_structures.Count} structures";
        }

        // ===== RT Plan Parsing (Isocenter) =====

        private void LoadRTPlan(DicomDataset ds)
        {
            try
            {
                if (ds.Contains(DicomTag.BeamSequence))
                {
                    var beamSeq = ds.GetSequence(DicomTag.BeamSequence);
                    foreach (var beam in beamSeq)
                    {
                        if (!beam.Contains(DicomTag.ControlPointSequence)) continue;
                        var cpSeq = beam.GetSequence(DicomTag.ControlPointSequence);
                        foreach (var cp in cpSeq)
                        {
                            if (cp.Contains(DicomTag.IsocenterPosition))
                            {
                                var iso = cp.GetValues<double>(DicomTag.IsocenterPosition);
                                if (iso.Length >= 3)
                                {
                                    IsoXInput.Value = iso[0];
                                    IsoYInput.Value = iso[1];
                                    IsoZInput.Value = iso[2];
                                    StatusText.Text = $"Plan Isocenter: ({iso[0]:F1}, {iso[1]:F1}, {iso[2]:F1}) mm";
                                    return; // Found, stop searching
                                }
                            }
                        }
                    }
                }

                // Fallback: Try IonBeamSequence for proton plans
                if (ds.Contains(DicomTag.IonBeamSequence))
                {
                    var ionSeq = ds.GetSequence(DicomTag.IonBeamSequence);
                    foreach (var beam in ionSeq)
                    {
                        if (!beam.Contains(DicomTag.IonControlPointSequence)) continue;
                        var cpSeq = beam.GetSequence(DicomTag.IonControlPointSequence);
                        foreach (var cp in cpSeq)
                        {
                            if (cp.Contains(DicomTag.IsocenterPosition))
                            {
                                var iso = cp.GetValues<double>(DicomTag.IsocenterPosition);
                                if (iso.Length >= 3)
                                {
                                    IsoXInput.Value = iso[0];
                                    IsoYInput.Value = iso[1];
                                    IsoZInput.Value = iso[2];
                                    StatusText.Text = $"Ion Plan Isocenter: ({iso[0]:F1}, {iso[1]:F1}, {iso[2]:F1}) mm";
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"RP: Could not extract isocenter ({ex.Message})";
            }
        }

        // ===== Contour Overlay =====

        private void DrawContours()
        {
            // Clear existing contour shapes from axial canvas
            var toRemove = AxialCanvas.Children.OfType<System.Windows.Shapes.Polyline>()
                .Where(p => p.Tag?.ToString() == "contour").ToList();
            foreach (var p in toRemove) AxialCanvas.Children.Remove(p);

            if (_doseVolume == null || _xPositions == null || _yPositions == null || _zPositions == null) return;
            if (_structures.Count == 0) return;

            // Determine which structures to render
            bool showAll = ShowContoursToggle?.IsChecked == true;
            var selected = StructureCombo?.SelectedItem as StructureContour;
            
            List<StructureContour> visibleStructures;
            if (!showAll && selected == null) return; // Nothing to show
            if (showAll)
                visibleStructures = _structures;
            else
                visibleStructures = selected != null ? new List<StructureContour> { selected } : _structures;

            double currentZ = Math.Round(_zPositions[_currentZ], 1);
            double canvasW = AxialCanvas.ActualWidth, canvasH = AxialCanvas.ActualHeight;
            if (canvasW <= 0 || canvasH <= 0) return;

            int cols = _doseVolume.GetLength(2);
            int rows = _doseVolume.GetLength(1);

            // Calculate actual rendered image bounds within the canvas (Stretch=Uniform)
            double imageAspect = (double)cols / rows;
            double canvasAspect = canvasW / canvasH;
            double renderedW, renderedH, offsetX, offsetY;

            if (imageAspect > canvasAspect)
            {
                renderedW = canvasW;
                renderedH = canvasW / imageAspect;
                offsetX = 0;
                offsetY = (canvasH - renderedH) / 2.0;
            }
            else
            {
                renderedH = canvasH;
                renderedW = canvasH * imageAspect;
                offsetX = (canvasW - renderedW) / 2.0;
                offsetY = 0;
            }

            // Physical extent of the dose grid
            double gridPhysW = _pixelSpacingX * cols;
            double gridPhysH = _pixelSpacingY * rows;

            foreach (var structure in visibleStructures)
            {
                if (!structure.SliceContours.ContainsKey(currentZ)) continue;

                foreach (var polygon in structure.SliceContours[currentZ])
                {
                    var polyline = new System.Windows.Shapes.Polyline
                    {
                        Stroke = new SolidColorBrush(structure.DisplayColor),
                        StrokeThickness = 1.5,
                        Tag = "contour"
                    };

                    foreach (var pt in polygon)
                    {
                        // Map DICOM LPS (mm) → fraction of dose grid → rendered image coords
                        double fracX = (pt.X - _xPositions[0]) / gridPhysW;
                        double fracY = (pt.Y - _yPositions[0]) / gridPhysH;
                        double px = offsetX + fracX * renderedW;
                        double py = offsetY + fracY * renderedH;
                        polyline.Points.Add(new Point(px, py));
                    }

                    // Close the polygon
                    if (polygon.Length > 0)
                    {
                        double fracX = (polygon[0].X - _xPositions[0]) / gridPhysW;
                        double fracY = (polygon[0].Y - _yPositions[0]) / gridPhysH;
                        polyline.Points.Add(new Point(offsetX + fracX * renderedW, offsetY + fracY * renderedH));
                    }

                    AxialCanvas.Children.Add(polyline);
                }
            }

            // --- Coronal (XZ) contours: horizontal extent lines at each Z ---
            var corToRemove = CoronalCanvas.Children.OfType<System.Windows.Shapes.Line>()
                .Where(l => l.Tag?.ToString() == "contour").ToList();
            foreach (var l in corToRemove) CoronalCanvas.Children.Remove(l);

            double ccw = CoronalCanvas.ActualWidth, cch = CoronalCanvas.ActualHeight;
            if (ccw > 0 && cch > 0)
            {
                int dFrames = _doseVolume.GetLength(0);
                double corImgAsp = (double)cols / dFrames;
                double corCanAsp = ccw / cch;
                double crw, crh, cox, coy;
                if (corImgAsp > corCanAsp) { crw = ccw; crh = ccw / corImgAsp; cox = 0; coy = (cch - crh) / 2; }
                else { crh = cch; crw = cch * corImgAsp; cox = (ccw - crw) / 2; coy = 0; }

                double zMin = _zPositions[0], zMax = _zPositions[dFrames - 1];
                double zRange = zMax - zMin;

                foreach (var structure in visibleStructures)
                {
                    foreach (var kvp in structure.SliceContours)
                    {
                        double z = kvp.Key;
                        // Find X extent at this Z
                        double xMin = double.MaxValue, xMax = double.MinValue;
                        foreach (var poly in kvp.Value)
                            foreach (var pt in poly) { if (pt.X < xMin) xMin = pt.X; if (pt.X > xMax) xMax = pt.X; }

                        double fzPos = 1.0 - (z - zMin) / zRange; // Flipped for display
                        double canvasY = coy + fzPos * crh;
                        double canvasX1 = cox + (xMin - _xPositions[0]) / gridPhysW * crw;
                        double canvasX2 = cox + (xMax - _xPositions[0]) / gridPhysW * crw;

                        var line = new System.Windows.Shapes.Line
                        {
                            X1 = canvasX1, X2 = canvasX2, Y1 = canvasY, Y2 = canvasY,
                            Stroke = new SolidColorBrush(structure.DisplayColor),
                            StrokeThickness = 1.0, Tag = "contour"
                        };
                        CoronalCanvas.Children.Add(line);
                    }
                }
            }

            // --- Sagittal (YZ) contours: horizontal extent lines at each Z ---
            var sagToRemove = SagittalCanvas.Children.OfType<System.Windows.Shapes.Line>()
                .Where(l => l.Tag?.ToString() == "contour").ToList();
            foreach (var l in sagToRemove) SagittalCanvas.Children.Remove(l);

            double scw = SagittalCanvas.ActualWidth, sch = SagittalCanvas.ActualHeight;
            if (scw > 0 && sch > 0)
            {
                int dFrames2 = _doseVolume.GetLength(0);
                double sagImgAsp = (double)rows / dFrames2;
                double sagCanAsp = scw / sch;
                double srw, srh, sox, soy;
                if (sagImgAsp > sagCanAsp) { srw = scw; srh = scw / sagImgAsp; sox = 0; soy = (sch - srh) / 2; }
                else { srh = sch; srw = sch * sagImgAsp; sox = (scw - srw) / 2; soy = 0; }

                double zMin = _zPositions[0], zMax = _zPositions[dFrames2 - 1];
                double zRange = zMax - zMin;
                double gridPhysHY = _pixelSpacingY * rows;

                foreach (var structure in visibleStructures)
                {
                    foreach (var kvp in structure.SliceContours)
                    {
                        double z = kvp.Key;
                        double yMin = double.MaxValue, yMax = double.MinValue;
                        foreach (var poly in kvp.Value)
                            foreach (var pt in poly) { if (pt.Y < yMin) yMin = pt.Y; if (pt.Y > yMax) yMax = pt.Y; }

                        double fzPos = 1.0 - (z - zMin) / zRange;
                        double canvasY = soy + fzPos * srh;
                        double canvasX1 = sox + (yMin - _yPositions[0]) / gridPhysHY * srw;
                        double canvasX2 = sox + (yMax - _yPositions[0]) / gridPhysHY * srw;

                        var line = new System.Windows.Shapes.Line
                        {
                            X1 = canvasX1, X2 = canvasX2, Y1 = canvasY, Y2 = canvasY,
                            Stroke = new SolidColorBrush(structure.DisplayColor),
                            StrokeThickness = 1.0, Tag = "contour"
                        };
                        SagittalCanvas.Children.Add(line);
                    }
                }
            }
        }

        // ===== Structure Navigation =====

        private void GoToCenter_Click(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null || StructureCombo.SelectedItem is not StructureContour structure) return;

            // Navigate to structure centroid
            _isUpdatingSliders = true;
            XSlider.Value = FindNearestIndex(structure.CenterX, _xPositions!);
            YSlider.Value = FindNearestIndex(structure.CenterY, _yPositions!);
            ZSlider.Value = FindNearestIndex(structure.CenterZ, _zPositions!);
            _isUpdatingSliders = false;

            _currentX = (int)XSlider.Value;
            _currentY = (int)YSlider.Value;
            _currentZ = (int)ZSlider.Value;

            // Auto-set isocenter to structure center
            IsoXInput.Value = structure.CenterX;
            IsoYInput.Value = structure.CenterY;
            IsoZInput.Value = structure.CenterZ;

            UpdateAllViews();
            StatusText.Text = $"Navigated to {structure.Name} center ({structure.CenterX:F1}, {structure.CenterY:F1}, {structure.CenterZ:F1})";
        }

        private void GoToMaxDose_Click(object sender, RoutedEventArgs e)
        {
            if (_doseVolume == null) return;

            _isUpdatingSliders = true;
            ZSlider.Value = _maxDoseZ;
            YSlider.Value = _maxDoseY;
            XSlider.Value = _maxDoseX;
            _isUpdatingSliders = false;

            _currentX = (int)XSlider.Value;
            _currentY = (int)YSlider.Value;
            _currentZ = (int)ZSlider.Value;

            UpdateAllViews();
            StatusText.Text = $"Navigated to Maximum Dose location: ({XCoordInput.Value:F1}, {YCoordInput.Value:F1}, {ZCoordInput.Value:F1})";
        }

        private void StructureCombo_Changed(object sender, SelectionChangedEventArgs e) => DrawContours();
        private void ShowContours_Checked(object sender, RoutedEventArgs e) => DrawContours();
    }
}
