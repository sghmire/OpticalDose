using System;
using System.Collections.Generic;
using System.Linq;

namespace FilmAnalysis
{
    public static class FittingMath
    {
        /// <summary>
        /// Performs a polynomial fit (Least Squares) of the specified degree.
        /// Returns coefficients from highest power to lowest (e.g., [a, b, c] for ax^2 + bx + c).
        /// </summary>
        public static double[] PolyFit(double[] x, double[] y, int degree)
        {
            if (x.Length != y.Length || x.Length <= degree)
                throw new ArgumentException("Insufficient data points for the requested degree.");

            int n = x.Length;
            int m = degree + 1;

            // Design matrix A (Vandermonde matrix)
            double[,] A = new double[n, m];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    A[i, j] = Math.Pow(x[i], degree - j);
                }
            }

            // Normal Equations: (A^T * A) * c = A^T * y
            double[,] At = Transpose(A);
            double[,] AtA = Multiply(At, A);
            double[] AtY = Multiply(At, y);

            return SolveLinearSystem(AtA, AtY);
        }

        public static double PolyVal(double[] coefficients, double x)
        {
            double result = 0;
            int degree = coefficients.Length - 1;
            for (int i = 0; i < coefficients.Length; i++)
            {
                result += coefficients[i] * Math.Pow(x, degree - i);
            }
            return result;
        }

        public static double CalculateRSquared(double[] x, double[] y, double[] coefficients)
        {
            double yMean = y.Average();
            double ssTot = y.Sum(v => Math.Pow(v - yMean, 2));
            double ssRes = 0;

            for (int i = 0; i < x.Length; i++)
            {
                double yFit = PolyVal(coefficients, x[i]);
                ssRes += Math.Pow(y[i] - yFit, 2);
            }

            return ssTot > 0 ? 1 - (ssRes / ssTot) : 0;
        }

        /// <summary>
        /// Optimizes the 'delta' factor for Triple Channel dosimetry (Matlab fmincon equivalent).
        /// Uses a Golden Section Search to find the optimal delta in [0.8, 1.2].
        /// </summary>
        public static double OptimizeTripleChannelDelta(
            double[] redNorm, double[] greenNorm, double[] blueNorm,
            double[] redFit, double[] greenFit, double[] blueFit)
        {
            Func<double, double> objective = (delta) =>
            {
                double sum = 0;
                for (int i = 0; i < redNorm.Length; i++)
                {
                    double rDose = PolyVal(redFit, redNorm[i] * delta);
                    double gDose = PolyVal(greenFit, greenNorm[i] * delta);
                    double bDose = PolyVal(blueFit, blueNorm[i] * delta);

                    sum += Math.Pow(rDose - gDose, 2) + 
                           Math.Pow(rDose - bDose, 2) + 
                           Math.Pow(gDose - bDose, 2);
                }
                return sum;
            };

            // Golden Section Search
            double a = 0.8, b = 1.2;
            double invPhi = (Math.Sqrt(5) - 1) / 2;
            double x1 = b - invPhi * (b - a);
            double x2 = a + invPhi * (b - a);
            double f1 = objective(x1);
            double f2 = objective(x2);

            for (int i = 0; i < 50; i++) // 50 iterations for high precision
            {
                if (f1 < f2)
                {
                    b = x2;
                    x2 = x1;
                    f2 = f1;
                    x1 = b - invPhi * (b - a);
                    f1 = objective(x1);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    f1 = f2;
                    x2 = a + invPhi * (b - a);
                    f2 = objective(x2);
                }
            }

            return (a + b) / 2;
        }

        #region Matrix Math Helpers

        private static double[,] Transpose(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] result = new double[cols, rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = matrix[i, j];
            return result;
        }

        private static double[,] Multiply(double[,] A, double[,] B)
        {
            int aRows = A.GetLength(0);
            int aCols = A.GetLength(1);
            int bCols = B.GetLength(1);
            double[,] result = new double[aRows, bCols];
            for (int i = 0; i < aRows; i++)
                for (int j = 0; j < bCols; j++)
                    for (int k = 0; k < aCols; k++)
                        result[i, j] += A[i, k] * B[k, j];
            return result;
        }

        private static double[] Multiply(double[,] A, double[] v)
        {
            int rows = A.GetLength(0);
            int cols = A.GetLength(1);
            double[] result = new double[rows];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[i] += A[i, j] * v[j];
            return result;
        }

        private static double[] SolveLinearSystem(double[,] M, double[] b)
        {
            // Gaussian Elimination with partial pivoting
            int n = b.Length;
            for (int i = 0; i < n; i++)
            {
                int pivot = i;
                for (int j = i + 1; j < n; j++)
                    if (Math.Abs(M[j, i]) > Math.Abs(M[pivot, i])) pivot = j;

                for (int k = i; k < n; k++)
                {
                    double tmp = M[i, k]; M[i, k] = M[pivot, k]; M[pivot, k] = tmp;
                }
                double tmpB = b[i]; b[i] = b[pivot]; b[pivot] = tmpB;

                for (int j = i + 1; j < n; j++)
                {
                    double factor = M[j, i] / M[i, i];
                    for (int k = i; k < n; k++) M[j, k] -= factor * M[i, k];
                    b[j] -= factor * b[i];
                }
            }

            double[] res = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++) sum += M[i, j] * res[j];
                res[i] = (b[i] - sum) / M[i, i];
            }
            return res;
        }

        #endregion
    }
}
