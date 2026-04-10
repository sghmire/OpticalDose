using System;

namespace OpticalDose
{
    /// <summary>
    /// Pure static color map routines for dose heatmap rendering.
    /// </summary>
    public static class ColorMaps
    {
        public static (byte R, byte G, byte B) GetColorFromMap(double v, string mapName)
        {
            v = Math.Clamp(v, 0, 1);
            return mapName switch
            {
                "Hot" => GetHotColor(v),
                "Viridis" => GetViridisColor(v),
                "Gray" => GetGrayColor(v),
                _ => GetJetColor(v)
            };
        }

        public static (byte R, byte G, byte B) GetJetColor(double v)
        {
            double r = 0, g = 0, b = 0;
            if (v < 0.25) { r = 0; g = 4 * v; b = 1; }
            else if (v < 0.5) { r = 0; g = 1; b = 1 + 4 * (0.25 - v); }
            else if (v < 0.75) { r = 4 * (v - 0.5); g = 1; b = 0; }
            else { r = 1; g = 1 + 4 * (0.75 - v); b = 0; }
            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        public static (byte R, byte G, byte B) GetHotColor(double v)
        {
            double r, g, b;
            if (v < 0.33) { r = 3 * v; g = 0; b = 0; }
            else if (v < 0.66) { r = 1; g = 3 * (v - 0.33); b = 0; }
            else { r = 1; g = 1; b = 3 * (v - 0.66); }
            return ((byte)(Math.Clamp(r, 0, 1) * 255), (byte)(Math.Clamp(g, 0, 1) * 255), (byte)(Math.Clamp(b, 0, 1) * 255));
        }

        public static (byte R, byte G, byte B) GetGrayColor(double v)
        {
            byte val = (byte)(v * 255);
            return (val, val, val);
        }

        public static (byte R, byte G, byte B) GetViridisColor(double v)
        {
            // Simple 3-point approximation: Purple -> Green -> Yellow
            double r, g, b;
            if (v < 0.5) {
                double t = v * 2;
                r = 0.26 + 0.1 * t; g = 0.0 + 0.6 * t; b = 0.33 + 0.1 * t;
            } else {
                double t = (v - 0.5) * 2;
                r = 0.36 + 0.6 * t; g = 0.6 + 0.3 * t; b = 0.43 - 0.3 * t;
            }
            return ((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
