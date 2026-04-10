using System;

namespace OpticalDose
{
    public class CalibrationConfig
    {
        public string Name { get; set; } = "New Calibration";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Channel and Degree settings
        public string Channel { get; set; } = "Single: Red"; 
        public int Degree { get; set; } = 3;

        // Coefficients (Generic names for Single/Dual/Triple flexibility)
        public double[]? FirstFit { get; set; }
        public double[]? SecondFit { get; set; }
        public double[]? ThirdFit { get; set; }

        public double DeltaOpt { get; set; } = 1.0;
        public double RSquared { get; set; } = 0;

        public bool IsValid => (FirstFit != null && FirstFit.Length > 0);
    }
}
