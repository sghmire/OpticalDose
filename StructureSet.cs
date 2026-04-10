using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace OpticalDose
{
    /// <summary>
    /// Represents a single ROI (Region of Interest) from an RT Structure Set.
    /// Contours are stored as polygons grouped by their Z-slice position.
    /// </summary>
    public class StructureContour
    {
        public string Name { get; set; } = "";
        public int ROINumber { get; set; }
        public Color DisplayColor { get; set; } = Colors.Yellow;

        /// <summary>
        /// Contour polygons grouped by Z position (mm).
        /// Key = Z coordinate (rounded to 2 decimal places for matching).
        /// Value = List of polygons (each polygon is an array of (X,Y) points in DICOM LPS mm).
        /// </summary>
        public Dictionary<double, List<Point[]>> SliceContours { get; set; } = new();

        // Bounding box
        public double MinX { get; set; } = double.MaxValue;
        public double MaxX { get; set; } = double.MinValue;
        public double MinY { get; set; } = double.MaxValue;
        public double MaxY { get; set; } = double.MinValue;
        public double MinZ { get; set; } = double.MaxValue;
        public double MaxZ { get; set; } = double.MinValue;

        public double CenterX => (MinX + MaxX) / 2.0;
        public double CenterY => (MinY + MaxY) / 2.0;
        public double CenterZ => (MinZ + MaxZ) / 2.0;

        public override string ToString() => Name;
    }
}
