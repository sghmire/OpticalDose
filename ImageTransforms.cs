using System;

namespace FilmAnalysis
{
    /// <summary>
    /// Pure static image transformation routines (rotate, flip, crop, clone).
    /// </summary>
    public static class ImageTransforms
    {
        public static double[,] CloneArray(double[,] src)
        {
            if (src == null) return null;
            return (double[,])src.Clone();
        }

        public static double[,] Rotate2D(double[,] src, int oldH, int oldW, bool isCW)
        {
            var dst = new double[oldW, oldH];
            for (int row = 0; row < oldH; row++)
                for (int col = 0; col < oldW; col++)
                {
                    int nr, nc;
                    if (isCW) { nr = col; nc = oldH - 1 - row; }
                    else { nr = oldW - 1 - col; nc = row; }
                    dst[nr, nc] = src[row, col];
                }
            return dst;
        }

        public static void FlipH(double[,] data, int h, int w)
        {
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w / 2; col++)
                {
                    int m = w - 1 - col;
                    (data[row, col], data[row, m]) = (data[row, m], data[row, col]);
                }
        }

        public static void FlipV(double[,] data, int h, int w)
        {
            for (int row = 0; row < h / 2; row++)
            {
                int m = h - 1 - row;
                for (int col = 0; col < w; col++)
                    (data[row, col], data[m, col]) = (data[m, col], data[row, col]);
            }
        }

        public static double[,] CropArray(double[,] src, int x, int y, int w, int h)
        {
            var dst = new double[h, w];
            for (int row = 0; row < h; row++)
                for (int col = 0; col < w; col++)
                    dst[row, col] = src[y + row, x + col];
            return dst;
        }
    }
}
