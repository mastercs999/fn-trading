using Accord.Statistics.Analysis;
using Accord.Math;
using DataCenter.Data;
using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;
using Accord.Statistics;

namespace DataCenter.Interface
{
    internal class RnnTable
    {
        private int[] DataSourcesStarts;
        private int[] DataSourcesCounts;

        private double[][] Input;
        private double[][] Output;
        private double[][] PriceOutput;

        public double[][] TrainX { get; set; }
        public double[][] TrainY { get; set; }
        public double[][] ValidX { get; set; }
        public double[][] ValidY { get; set; }
        public double[][] TestX { get; set; }
        public double[][] TestY { get; set; }
        public RnnConfig RnnConfig { get; set; }

        public void CreateRnnTable()
        {
            // Create config
            RnnConfig = new RnnConfig();

            // Load data
            double[][] data = LoadFullData(DataManager.Config.FullDataFile);

            List<double[]> newData = new List<double[]>();
            for (int i = 0; i < 5500; ++i)
                newData.Add(new double[] { Math.Sin(i / 50.0) });
            //data = newData.ToArray();

            // Fill NaNs
            FillNaNs(data);

            // Create input/output
            CreateInputOutput(data, DataManager.Config.Predict);

            // Split to train, valid, test
            SplitData();

            // Export YY a Z data (correct output and first order prediction)
            ExportYZData();

            // Normalize data
            NormalizeData(TrainX, ValidX, TestX, 0, DataManager.Config.NormalizationWindowSize);
            NormalizeData(TrainY, ValidY, TestY, 1, DataManager.Config.NormalizationWindowSize);

            //// PCA transformation
            //PcaTransformation(0);
            //PcaTransformation(1);

            // Normalize again
            //NormalizeData(TrainX, ValidX, TestX, 888);
            //NormalizeData(TrainY, ValidY, TestY, 1);

            // Clip to high values
            ClipData(10);
        }

        private double[][] LoadFullData(string path)
        {
            string prefix = "Loading full data...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Load lines
            string[] lines = File.ReadAllLines(path);

            // Get data sources info
            string[] headers = lines[0].Split(new char[] { ';' }).Skip(1).Select(x => x.Trim().Substring(0, 1)).ToArray();
            int[] headersNum = headers.Select(x => int.Parse(x)).ToArray();
            int[] numbers = headers.Select(x => int.Parse(x)).Distinct().ToArray();
            DataSourcesStarts = new int[numbers.Length];
            DataSourcesCounts = new int[numbers.Length];
            for (int k = 0; k < numbers.Length; ++k)
            {
                int num = numbers[k];

                DataSourcesStarts[k] = headersNum.Select((v, i) => new { number = v, index = i }).First(x => x.number == num).index;
                DataSourcesCounts[k] = headersNum.Where(x => x == num).Count();
            }

            // Create array
            double[][] data = lines.Skip(1).Select(x => x.Split(new char[] { ';' }).Skip(1).Select(y => y.Trim() == "NaN" ? double.NaN : double.Parse(y)).ToArray()).ToArray();

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();

            return data;
        }
        private void FillNaNs(double[][] data)
        {
            string prefix = "Filling NaNs...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(data[0].Length);

            Random currentRandom = new Random(DataManager.Config.Random.Next());

            // Loop through all columns
            int row = 0;
            for (int col = 0; col < data[0].Length; ++col)
            {
                // Najdeme si statisticke vlastnosti
                double[] d = data.Select(x => x[col]).ToArray();
                double[] diff = new double[d.Length - 1];
                for (int i = 1; i < d.Length; ++i)
                    diff[i - 1] = d[i] - d[i - 1];
                diff = diff.Where(x => !double.IsNaN(x)).ToArray();
                double mean = diff.Mean();
                double stddev = diff.StandardDeviation(mean);
                if (diff.Length <= 1)
                {
                    mean = 0;
                    stddev = 1;
                }

                // Nahodny generator
                Accord.Math.Random.GaussianGenerator gen = new Accord.Math.Random.GaussianGenerator((float)mean, (float)stddev, currentRandom.Next());

                // Doplnime zacatek
                for (row = 0; row < data.Length; ++row)
                    if (!double.IsNaN(data[row][col]))
                    {
                        for (row = row - 1; row >= 0; --row)
                            data[row][col] = data[row + 1][col] + gen.Next();
                        break;
                    }

                // Doplnime konec
                for (row = data.Length - 1; row >= 0; --row)
                    if (!double.IsNaN(data[row][col]))
                    {
                        for (row = row + 1; row < data.Length; ++row)
                            data[row][col] = data[row - 1][col] + gen.Next();
                        break;
                    }

                // Doplnime mezery
                for (row = 0; row < data.Length; ++row)
                {
                    if (double.IsNaN(data[row][col]))
                    {
                        int start = row - 1;
                        for (row = row + 1; row < data.Length; ++row)
                            if (!double.IsNaN(data[row][col]))
                            {
                                int end = row;

                                // Doplnime od start do end
                                for (int i = start + 1; i < end; ++i)
                                {
                                    // Najdeme hodnotu pri aproximaci
                                    double aprox = data[start][col] + (data[end][col] - data[start][col]) / (end - start) * (i - start);

                                    data[i][col] = aprox + gen.Next();
                                }

                                break;
                            }
                    }
                }

                // Pro jistotu rozkopirovani
                for (row = 1; row < data.Length; ++row)
                    if (double.IsNaN(data[row][col]))
                        data[row][col] = data[row - 1][col];
                for (row = data.Length - 2; row >= 0; --row)
                    if (double.IsNaN(data[row][col]))
                        data[row][col] = data[row + 1][col];

                if (col % drawEvery == 0)
                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)col / data[0].Length * 100.0), ConsoleColor.Gray);
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void CreateInputOutput(double[][] data, int predict)
        {
            string prefix = "Creating input/output...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(data.Length);

            // Input and output
            List<double[]> input = new List<double[]>();
            List<double[]> output = new List<double[]>();
            List<double[]> priceOutput = new List<double[]>();
            int back = 1;
            int memory = DataManager.Config.Predict;
            int memoryHelp = DataManager.Config.Predict;
            for (int row = back * Math.Max(memory, memoryHelp); row < data.Length - predict; ++row)
            {
                List<double> xx = new List<double>();
                for (int i = 0; i < back; ++i)
                {
                    int from = row - (i + 1) * memory;
                    int to = row - i * memory;
                    double[] vector = data[to].Subtract(data[from]);

                    from = row - (i + 1) * memoryHelp;
                    to = row - i * memoryHelp;
                    for (int k = DataSourcesStarts[1]; k < DataSourcesStarts[1] + DataSourcesCounts[1]; ++k)
                    {
                        vector[k] = 0;
                        for (int m = from; m <= to; ++m)
                            vector[k] += data[m][k];
                    }

                    xx.AddRange(vector);
                }
                double[] x = xx.ToArray();
                double[] y = data[row + predict].Subtract(data[row]);

                x = GetDataSourcesData(x, 0, 1, 2, 5, 6);
                y = GetDataSourcesData(y, 0);

                priceOutput.Add((double[])y.Clone());

                for (int k = 0; k < y.Length; ++k)
                    y[k] = y[k] >= 0 ? 1 : 0;

                input.Add(x);
                output.Add(y);

                if (row % drawEvery == 0)
                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)row / data.Length * 100.0), ConsoleColor.Gray);
            }

            Input = input.ToArray();
            Output = output.ToArray();
            PriceOutput = priceOutput.ToArray();

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();

        }
        private void SplitData()
        {
            string prefix = "Splitting data...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Count borders
            int validateFrom = (int)Math.Round(Input.Length * DataManager.Config.ValidFrom);
            int testFrom = (int)Math.Round(Input.Length * DataManager.Config.TestFrom);

            // Create arrays
            TrainX = Input.Take(validateFrom).Select(s => (double[])s.Clone()).ToArray();
            TrainY = Output.Take(validateFrom).Select(s => (double[])s.Clone()).ToArray();
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 33), ConsoleColor.Gray);

            ValidX = Input.Skip(validateFrom).Take(testFrom - validateFrom).Select(s => (double[])s.Clone()).ToArray();
            ValidY = Output.Skip(validateFrom).Take(testFrom - validateFrom).Select(s => (double[])s.Clone()).ToArray();
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 66), ConsoleColor.Gray);

            TestX = Input.Select(s => (double[])s.Clone()).ToArray();
            TestY = Output.Select(s => (double[])s.Clone()).ToArray();
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 99), ConsoleColor.Gray);

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void ExportYZData()
        {
            string prefix = "Exporting YZ data...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            File.WriteAllLines(DataManager.Config.FullPredictedYYFile, PriceOutput.Select(x => String.Join(";", x)));

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50), ConsoleColor.Gray);

            File.WriteAllLines(DataManager.Config.FullPredictedZFile, TestX.Select(x => String.Join(";", x.Take(DataSourcesCounts[0]))));

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void NormalizeData(double[][] train, double[][] valid, double[][] test, int order)
        {
            NormalizeData(train, valid, test, order, int.MaxValue);
        }
        private void NormalizeData(double[][] train, double[][] valid, double[][] test, int order, int window)
        {
            string prefix = "Normalizing 1/2...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);
            int drawEvery = Utils.PercentIntervalByLength(test.Length);

            if (order == 0)
            {

                // Begin config
                RnnConfig.BeginStat(order, test.Length, test[0].Length, window);

                // Create stats for data
                for (int from = 0; from < test.Length; from += window)
                {
                    Stat[] stats = null;
                    if (from >= window)
                        stats = GetStats(test, from - window, window);
                    else if (window < train.Length)
                        stats = GetStats(test, 0, window);
                    else
                        stats = GetStats(test, 0, train.Length);

                    foreach (Stat st in stats)
                        if (st.Deviance == 0)
                            st.Deviance = 1;

                    // Set stats
                    for (int col = 0; col < test[0].Length; ++col)
                        RnnConfig.SetStat(order, from, col, stats[col]);

                    if (from % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)from / test.Length * 100.0), ConsoleColor.Gray);
                }
                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            }
            else
                order = 0;





            prefix = "Normalizing 2/2...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);
            drawEvery = Utils.PercentIntervalByLength(test.Length);

            // Normalize data
            for (int row = 0; row < test.Length; ++row)
                for (int col = 0; col < test[0].Length; ++col)
                {
                    Stat stat = RnnConfig.GetStat(order, row, col);

                    if (row < train.Length)
                        train[row][col] = Utils.NormalizeValue(train[row][col], 0, 1, stat.Mean, stat.Deviance);
                    if (row >= train.Length && row < train.Length + valid.Length)
                        valid[row - train.Length][col] = Utils.NormalizeValue(valid[row - train.Length][col], 0, 1, stat.Mean, stat.Deviance);
                    test[row][col] = Utils.NormalizeValue(test[row][col], 0, 1, stat.Mean, stat.Deviance);

                    if (row % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)row / test.Length * 100.0), ConsoleColor.Gray);
                }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void PcaTransformation(int xy)
        {
            string prefix = "PCA...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            double[][] train, valid, test;
            if (xy == 0)
            {
                train = TrainX;
                valid = ValidX;
                test = TestX;
            }
            else
            {
                train = TrainY;
                valid = ValidY;
                test = TestY;
            }

            // Perform PCA
            double[] w = null;
            double[,] u = null;
            double[,] vt = new double[0, 0];
            double[,] cov = train.Transpose().Dot(train).ToMatrix().Divide(train.Length);
            alglib.svd.rmatrixsvd(cov, cov.GetLength(0), cov.GetLength(1), 0, 1, 2, ref w, ref u, ref vt);
            vt = vt.Transpose();
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50), ConsoleColor.Gray);
            
            // Reduce coefficients
            double[,] vtt = new double[vt.GetLength(0), (int)Math.Round(vt.GetLength(1) * 1.0)];
            for (int i = 0; i < vtt.GetLength(0); ++i)
                for (int k = 0; k < vtt.GetLength(1); ++k)
                    vtt[i, k] = vt[i, k];
            vt = vtt;

            // Get new data
            train = train.Dot(vt);
            valid = valid.Dot(vt);
            test = test.Dot(vt);

            // Save transposed for decoding
            RnnConfig.PcaCoefficients[xy] = vt.Transpose();

            if (xy == 0)
            {
                TrainX = train;
                ValidX = valid;
                TestX = test;
            }
            else
            {
                TrainY = train;
                ValidY = valid;
                TestY = test;
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void ClipData(double border)
        {
            // All data
            double[][][] allData = new double[][][]
            {
                TrainX,
                ValidX,
                TestX,
                TrainY,
                ValidY,
                TestY
            };

            // Border values
            double min = -Math.Abs(border);
            double max = Math.Abs(border);

            // Clip values
            for (int i = 0; i < allData.Length; ++i)
            {
                string prefix = "Clipping " + (i + 1) + "/" + allData.Length +"...";
                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

                double[][] data = allData[i];
                int drawEvery = Utils.PercentIntervalByLength(data.Length);
                for (int m = 0; m < data.Length; ++m)
                {
                    for (int n = 0; n < data[m].Length; ++n)
                    {
                        if (data[m][n] > max)
                        {
                            if (data == TestY)
                                RnnConfig.SetClippedValue(m, n, data[m][n]);
                            data[m][n] = max;
                        }
                        if (data[m][n] < min)
                        {
                            if (data == TestY)
                                RnnConfig.SetClippedValue(m, n, data[m][n]);
                            data[m][n] = min;
                        }
                    }

                    if (m % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)m / data.Length * 100.0), ConsoleColor.Gray);
                }

                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            }

            Console.WriteLine();
        }

        private Stat[] GetStats(double[][] data, int from, int count)
        {
            from = Math.Max(from, 0);

            Stat[] stats = new Stat[data[0].Length];
            
            for (int i = 0; i < data[0].Length; ++i)
            {
                double[] part = data.Skip(from).Take(count).Select(x => x[i]).ToArray();

                stats[i] = new Stat();
                stats[i].Mean = part.Mean();
                stats[i].Deviance = part.StandardDeviation(stats[i].Mean);
            }

            return stats;
        }
        private double[] GetDataSourcesData(double[] allSources, params int[] sources)
        {
            List<double> results = new List<double>();
            
            foreach (int src in sources)
            {
                int from = DataSourcesStarts[src];
                int count = DataSourcesCounts[src];

                results.AddRange(allSources.Skip(from).Take(count));
            }

            return results.ToArray();
        }
    }
}
