using System;

namespace SharpML.Recurrent.Models
{
    [Serializable]
    public class Matrix
    {
        public int Rows;
        public int Cols;
        public double[] W;
        public double[] Dw;
        public double[] StepCache;
        public bool[] Dropped;

        public Matrix(int dim)
        {
            this.Rows = dim;
            this.Cols = 1;
            this.W = new double[Rows * Cols];
            this.Dw = new double[Rows * Cols];
            this.StepCache = new double[Rows * Cols];
            this.Dropped = new bool[Rows * Cols];
        }

        public Matrix(int rows, int cols)
        {
            this.Rows = rows;
            this.Cols = cols;
            this.W = new double[rows * cols];
            this.Dw = new double[rows * cols];
            this.StepCache = new double[rows * cols];
            this.Dropped = new bool[rows * cols];
        }

        public Matrix(double[] vector)
        {
            this.Rows = vector.Length;
            this.Cols = 1;
            this.W = vector;
            this.Dw = new double[vector.Length];
            this.StepCache = new double[vector.Length];
            this.Dropped = new bool[vector.Length];
        }


        public override string ToString()
        {
            String result = "";
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    result += String.Format("{0:N5}", GetW(r, c)) + "\t";
                }
                result += "\n";
            }
            return result;
        }

        public Matrix Clone()
        {
            Matrix result = new Matrix(Rows, Cols);
            for (int i = 0; i < W.Length; i++)
            {
                result.W[i] = W[i];
                result.Dw[i] = Dw[i];
                result.StepCache[i] = StepCache[i];
                result.Dropped[i] = Dropped[i];
            }
            return result;
        }

        public void ResetDw()
        {
            for (int i = 0; i < Dw.Length; i++)
            {
                Dw[i] = 0;
            }
        }

        public void ResetStepCache()
        {
            for (int i = 0; i < StepCache.Length; i++)
            {
                StepCache[i] = 0;
            }
        }

        public static Matrix Transpose(Matrix m)
        {
            Matrix result = new Matrix(m.Cols, m.Rows);
            for (int r = 0; r < m.Rows; r++)
            {
                for (int c = 0; c < m.Cols; c++)
                {
                    result.SetW(c, r, m.GetW(r, c));
                }
            }
            return result;
        }

        public static Matrix Random(int rows, int cols, double initParamsStdDev, Random rng)
        {
            Accord.Math.Random.GaussianGenerator gen = new Accord.Math.Random.GaussianGenerator(0, (float)initParamsStdDev, rng.Next());

            Matrix result = new Matrix(rows, cols);
            for (int i = 0; i < result.W.Length; i++)
            {
                result.W[i] = gen.Next();
            }
            return result;
        }

        public static Matrix Ident(int dim)
        {
            Matrix result = new Matrix(dim, dim);
            for (int i = 0; i < dim; i++)
            {
                result.SetW(i, i, 1.0);
            }
            return result;
        }

        public static Matrix Uniform(int rows, int cols, double s)
        {
            Matrix result = new Matrix(rows, cols);
            for (int i = 0; i < result.W.Length; i++)
            {
                result.W[i] = s;
            }
            return result;
        }

        public static Matrix Ones(int rows, int cols)
        {
            return Uniform(rows, cols, 1.0);
        }

        public static Matrix NegativeOnes(int rows, int cols)
        {
            return Uniform(rows, cols, -1.0);
        }

       

        private int GetByIndex(int row, int col)
        {
            int ix = Cols * row + col;
            return ix;
        }

        private double GetW(int row, int col)
        {
            return W[GetByIndex(row, col)];
        }

        private void SetW(int row, int col, double val)
        {
            W[GetByIndex(row, col)] = val;
        }

        public void SetDropped(int row, int col, bool dropped)
        {
            Dropped[GetByIndex(row, col)] = dropped;
        }
    }
}
