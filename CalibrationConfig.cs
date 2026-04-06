using System;

namespace FilmAnalysis
{
    /// <summary>
    /// Represents a complete calibration fit configuration, including multiple channels and optimization factors.
    /// </summary>
    public class CalibrationConfig
    {
        public string Name { get; set; } = "New Calibration";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Channel and Degree settings
        public string ChannelType { get; set; } = "Single"; // Single, Dual, Triple
        public string TargetChannel { get; set; } = "Red";  // Red, Green, Blue, Red/Blue, etc.
        public int PolyDegree { get; set; } = 3;

        // Coefficients (Highest power to lowest)
        public double[] RedCoefficients { get; set; }
        public double[] GreenCoefficients { get; set; }
        public double[] BlueCoefficients { get; set; }

        // Optimized stabilization factors (Matlab delta_opt)
        public double DeltaOpt { get; set; } = 1.0;

        // Statistical feedback
        public double RSquared { get; set; } = 0;

        // Raw data points (for potential re-fitting)
        public double[][] RawPoints { get; set; } // [][Dose, Red, Green, Blue]

        public bool IsValid => (RedCoefficients != null && RedCoefficients.Length > 0);
    }
}
