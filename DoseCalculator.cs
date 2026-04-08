using System;

namespace FilmAnalysis
{
    /// <summary>
    /// Static dose calculation from calibration curves.
    /// </summary>
    public static class DoseCalculator
    {
        public static double CalculateSinglePixelDose(
            double[,] redChannel, double[,] greenChannel, double[,] blueChannel,
            int x, int y, string mode, CalibrationConfig config, double delta)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(mode)) throw new ArgumentException("Mode is required", nameof(mode));

            double rValue = redChannel != null ? redChannel[y, x] : 0;
            double gValue = greenChannel != null ? greenChannel[y, x] : 0;
            double bValue = blueChannel != null ? blueChannel[y, x] : 0;

            double rOD = -Math.Log10(Math.Max(rValue, 1) / 65535.0);
            double gOD = -Math.Log10(Math.Max(gValue, 1) / 65535.0);
            double bOD = -Math.Log10(Math.Max(bValue, 1) / 65535.0);

            if (mode.Contains("Single") && mode.Contains("Red"))
            {
                return Math.Max(0, FittingMath.PolyVal(config.FirstFit!, rOD));
            }
            else if (mode.Contains("Single") && mode.Contains("Green"))
            {
                return Math.Max(0, FittingMath.PolyVal(config.FirstFit!, gOD));
            }
            else if (mode.Contains("Single") && mode.Contains("Blue"))
            {
                return Math.Max(0, FittingMath.PolyVal(config.FirstFit!, bOD));
            }
            else if (mode.Contains("Dual") && mode.Contains("Red"))
            {
                double ratio = rOD / (bOD + 2.22e-16);
                double firstPass = FittingMath.PolyVal(config.FirstFit!, ratio);
                return Math.Max(0, FittingMath.PolyVal(config.SecondFit!, firstPass));
            }
            else if (mode.Contains("Dual") && mode.Contains("Green"))
            {
                double ratio = gOD / (bOD + 2.22e-16);
                double firstPass = FittingMath.PolyVal(config.FirstFit!, ratio);
                return Math.Max(0, FittingMath.PolyVal(config.SecondFit!, firstPass));
            }
            else if (mode.Contains("Triple"))
            {
                double doseR = FittingMath.PolyVal(config.FirstFit!, rOD * delta);
                double doseG = FittingMath.PolyVal(config.SecondFit!, gOD * delta);
                double doseB = FittingMath.PolyVal(config.ThirdFit!, bOD * delta);

                return Math.Max(0, (doseR + doseG + doseB) / 3.0);
            }

            throw new InvalidOperationException($"Unsupported mode '{mode}'.");
        }
    }
}
