using System;
using System.Threading.Tasks;

namespace OpticalDose
{
    /// <summary>
    /// Pure static image filtering and interpolation routines.
    /// </summary>
    public static class ImageFilters
    {
        // --- Median Filter (medfilt2 equivalent) ---
        public static double[,] MedianFilter2D(double[,] input, int kernelSize)
        {
            int h = input.GetLength(0), w = input.GetLength(1);
            var output = new double[h, w];
            int half = kernelSize / 2;
            var buffer = new double[kernelSize * kernelSize];

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    int count = 0;
                    for (int ky = -half; ky <= half; ky++)
                    {
                        int ry = Math.Clamp(row + ky, 0, h - 1);
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int cx = Math.Clamp(col + kx, 0, w - 1);
                            buffer[count++] = input[ry, cx];
                        }
                    }
                    Array.Sort(buffer, 0, count);
                    output[row, col] = buffer[count / 2];
                }
            }
            return output;
        }

        // --- Box/Average Filter (smoothdata2 movmean equivalent) ---
        public static double[,] BoxFilter2D(double[,] input, int kernelSize)
        {
            int h = input.GetLength(0), w = input.GetLength(1);
            var output = new double[h, w];
            int half = kernelSize / 2;

            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    double sum = 0; int count = 0;
                    for (int ky = -half; ky <= half; ky++)
                    {
                        int ry = Math.Clamp(row + ky, 0, h - 1);
                        for (int kx = -half; kx <= half; kx++)
                        {
                            int cx = Math.Clamp(col + kx, 0, w - 1);
                            sum += input[ry, cx];
                            count++;
                        }
                    }
                    output[row, col] = sum / count;
                }
            }
            return output;
        }

        // --- Gaussian Filter (imgaussfilt equivalent) ---
        public static double[,] GaussianFilter2D(double[,] input, double sigma)
        {
            int kernelRadius = (int)Math.Ceiling(sigma * 3);
            int kernelSize = kernelRadius * 2 + 1;

            // Build Gaussian kernel
            double[] kernel1D = new double[kernelSize];
            double kernelSum = 0;
            for (int i = 0; i < kernelSize; i++)
            {
                double x = i - kernelRadius;
                kernel1D[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
                kernelSum += kernel1D[i];
            }
            for (int i = 0; i < kernelSize; i++) kernel1D[i] /= kernelSum;

            int h = input.GetLength(0), w = input.GetLength(1);

            // Separable: horizontal pass
            var temp = new double[h, w];
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    double sum = 0;
                    for (int k = -kernelRadius; k <= kernelRadius; k++)
                    {
                        int cx = Math.Clamp(col + k, 0, w - 1);
                        sum += input[row, cx] * kernel1D[k + kernelRadius];
                    }
                    temp[row, col] = sum;
                }
            }

            // Vertical pass
            var output = new double[h, w];
            for (int col = 0; col < w; col++)
            {
                for (int row = 0; row < h; row++)
                {
                    double sum = 0;
                    for (int k = -kernelRadius; k <= kernelRadius; k++)
                    {
                        int ry = Math.Clamp(row + k, 0, h - 1);
                        sum += temp[ry, col] * kernel1D[k + kernelRadius];
                    }
                    output[row, col] = sum;
                }
            }
            return output;
        }

        // --- Noise Filter (NaN/Inf/threshold removal) ---
        public static void ApplyNoiseFilter(double[,] data, double threshold)
        {
            int h = data.GetLength(0), w = data.GetLength(1);
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    double v = data[row, col];
                    if (double.IsNaN(v) || double.IsInfinity(v) || Math.Abs(v) > threshold)
                        data[row, col] = 1;
                }
            }
        }

        // --- 2D Interpolation (interp2 equivalent) ---
        public static double[,] Interpolate2D(double[,] input, int newW, int newH, string method)
        {
            int oldH = input.GetLength(0), oldW = input.GetLength(1);
            var output = new double[newH, newW];

            // Handle degenerate 1-pixel dimensions to avoid divide-by-zero/NaN coordinates.
            if (newH == 1 && newW == 1)
            {
                output[0, 0] = input[Math.Min(oldH - 1, 0), Math.Min(oldW - 1, 0)];
                return output;
            }

            // Using direct linear mapping to preserve the physical reference point (0,0) 
            // used in the origin calculation: pos = (index - N/2) * spacing.
            double rowScale = (double)oldH / newH;
            double colScale = (double)oldW / newW;

            Parallel.For(0, newH, newRow =>
            {
                for (int newCol = 0; newCol < newW; newCol++)
                {
                    // Map output pixel to input coordinates
                    double srcRow = newRow * rowScale;
                    double srcCol = newCol * colScale;

                    if (method == "Nearest")
                    {
                        int r = (int)Math.Round(srcRow);
                        int c = (int)Math.Round(srcCol);
                        r = Math.Clamp(r, 0, oldH - 1);
                        c = Math.Clamp(c, 0, oldW - 1);
                        output[newRow, newCol] = input[r, c];
                    }
                    else if (method == "Linear")
                    {
                        output[newRow, newCol] = BilinearSample(input, srcRow, srcCol, oldH, oldW);
                    }
                    else // Cubic
                    {
                        output[newRow, newCol] = BicubicSample(input, srcRow, srcCol, oldH, oldW);
                    }
                }
            });

            return output;
        }

        public static double BilinearSample(double[,] data, double row, double col, int h, int w)
        {
            int r0 = (int)Math.Floor(row);
            int r1 = r0 + 1;
            int c0 = (int)Math.Floor(col);
            int c1 = c0 + 1;

            double fr = row - r0;
            double fc = col - c0;

            r0 = Math.Clamp(r0, 0, h - 1);
            r1 = Math.Clamp(r1, 0, h - 1);
            c0 = Math.Clamp(c0, 0, w - 1);
            c1 = Math.Clamp(c1, 0, w - 1);

            double v00 = data[r0, c0];
            double v01 = data[r0, c1];
            double v10 = data[r1, c0];
            double v11 = data[r1, c1];

            return v00 * (1 - fr) * (1 - fc) +
                   v01 * (1 - fr) * fc +
                   v10 * fr * (1 - fc) +
                   v11 * fr * fc;
        }

        public static double BicubicSample(double[,] data, double row, double col, int h, int w)
        {
            int r0 = (int)Math.Floor(row);
            int c0 = (int)Math.Floor(col);
            double fr = row - r0, fc = col - c0;

            double sum = 0;
            for (int m = -1; m <= 2; m++)
            {
                double wr = CubicWeight(fr - m);
                for (int n = -1; n <= 2; n++)
                {
                    double wc = CubicWeight(fc - n);
                    int ri = Math.Clamp(r0 + m, 0, h - 1);
                    int ci = Math.Clamp(c0 + n, 0, w - 1);
                    sum += data[ri, ci] * wr * wc;
                }
            }
            return sum;
        }

        public static double CubicWeight(double x)
        {
            x = Math.Abs(x);
            if (x <= 1) return 1.5 * x * x * x - 2.5 * x * x + 1;
            if (x < 2) return -0.5 * x * x * x + 2.5 * x * x - 4 * x + 2;
            return 0;
        }
    }
}
