using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace TradingCenter
{
    public class TradingSystem
    {
        private Config Config;

        public TradingSystem(Config config)
        {
            Config = config;
        }

        public void Trade()
        {
            //if (!Config.Reload && File.Exists(Config.TradesFile))
            //    return;

            string prefix = "Trading...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Load needed data
            double[][] predicted = Utils.LoadCsv(Config.FullPredictedXFile);
            double[][] correct = Utils.LoadCsv(Config.FullPredictedYYFile);
            double[][] predictedFirstOrder = Utils.LoadCsv(Config.FullPredictedZFile);
            double[][] sharesPrices = LoadPrices(Config.FullDataFile);

            // Find train and valid length
            int trainLength = File.ReadAllLines(Config.RnnTrainYFile).Length;
            int validLength = File.ReadAllLines(Config.RnnValidYFile).Length;

            int drawEvery = Utils.PercentIntervalByLength(predicted.Length);

            // Spocitame vahy akcie
            double[] weights = StocksWeights(correct.Take(trainLength).ToArray());

            // Nahodny generator
            Random currentRandom = new Random(Config.Random.Next());

            // Result file
            StreamWriter sw = new StreamWriter(Config.TradesFile);

            // First line 
            // Balance, BuyBalance, FirstOrder, RandomBalance, Border
            List<double[]> lines = new List<double[]>();
            lines.Add(new double[] { 0, 0, 0, 0, double.NaN });

            // Count percent predictions
            int predictionCount = 0;
            int predictionRight = 0;
            double trainPredictionSuccess = 0;

            int buyPredictionCount = 0;
            int buyPredictionRight = 0;
            double buyTrainPredictionSuccess = 0;

            int firstOrderCount = 0;
            int firstOrderRight = 0;
            double firstOrderSuccess = 0;

            int randomCount = 0;
            int randomRight = 0;
            double randomSuccess = 0;


            // Make trades
            for (int i = 0; i < correct.Length; ++i)
            {
                double[] prevLine = lines.Last();
                double[] line = new double[] { prevLine[0], prevLine[1], prevLine[2], prevLine[3], double.NaN };

                if (i == trainLength)
                {
                    trainPredictionSuccess = (double)predictionRight / predictionCount * 100;
                    predictionCount = 0;
                    predictionRight = 0;

                    buyTrainPredictionSuccess = (double)buyPredictionRight / buyPredictionCount * 100;
                    buyPredictionCount = 0;
                    buyPredictionRight = 0;

                    firstOrderSuccess = (double)firstOrderRight / firstOrderCount * 100;
                    firstOrderCount = 0;
                    firstOrderRight = 0;

                    randomSuccess = (double)randomRight / randomCount * 100;
                    randomCount = 0;
                    randomRight = 0;

                    line[4] = 0;
                }
                else if (i > trainLength && i < trainLength + 20)
                    line[4] = 0;
                else
                    line[4] = double.NaN;

                //if (i == trainLength + validLength)
                //    break;

                for (int k = 0; k < correct[i].Length; ++k)
                {
                    double pred = predicted[i][k];
                    double corr = correct[i][k];

                    double threshold = 0.75;

                    // Strategy
                    if (pred > threshold)
                    {
                        if (corr > 0)
                        {
                            line[0] += Math.Abs(corr * weights[k]);

                            ++predictionRight;
                        }
                        else
                            line[0] += -Math.Abs(corr * weights[k]);

                        ++predictionCount;
                    }

                    // Buy strategy
                    line[1] += corr * weights[k];

                    ++buyPredictionCount;
                    if (corr >= 0)
                        ++buyPredictionRight;

                    // First order strategy
                    if (predictedFirstOrder[i][k] > threshold)
                    {
                        if (corr > 0)
                        {
                            line[2] += Math.Abs(corr * weights[k]);

                            ++firstOrderRight;
                        }
                        else
                            line[2] -= Math.Abs(corr * weights[k]);

                        ++firstOrderCount;
                    }

                    // Random strategy
                    if (currentRandom.Next(0, 2) == 0)
                    {
                        line[3] += corr * weights[k];

                        ++randomCount;
                        if (corr >= 0)
                            ++randomRight;
                    }
                }

                // Add results as one line
                lines.Add(line);

                if (i % drawEvery == 0)
                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / predicted.Length * 100.0), ConsoleColor.Gray);
            }

            // Set border to max size
            double borderValue = lines.Select(x => x.Max()).Max();
            foreach (double[] d in lines)
                if (!double.IsNaN(d[4]))
                    d[4] = borderValue;

            // Write trades
            foreach (double[] d in lines)
                WriteLine(sw, d);
            sw.Close();

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Correct predictions LSTM [train]: " + Math.Round(trainPredictionSuccess, 2) + "%");
            Console.WriteLine("Correct predictions LSTM [test ]: " + Math.Round((double)predictionRight / predictionCount * 100, 2) + "%");
            Console.WriteLine();
            Console.WriteLine("Correct predictions BUY  [train]: " + Math.Round(buyTrainPredictionSuccess, 2) + "%");
            Console.WriteLine("Correct predictions BUY  [test ]: " + Math.Round((double)buyPredictionRight / buyPredictionCount * 100, 2) + "%");
            Console.WriteLine();
            Console.WriteLine("Correct predictions FO   [train]: " + Math.Round(firstOrderSuccess, 2) + "%");
            Console.WriteLine("Correct predictions FO   [test ]: " + Math.Round((double)firstOrderRight / firstOrderCount * 100, 2) + "%");
            Console.WriteLine();
            Console.WriteLine("Correct predictions RND  [train]: " + Math.Round(randomSuccess, 2) + "%");
            Console.WriteLine("Correct predictions RND  [test ]: " + Math.Round((double)randomRight / randomCount * 100, 2) + "%");
            Console.WriteLine();
            Console.WriteLine();
        }

        private void WriteLine(StreamWriter sw, double[] data)
        {
            string str = "";
            foreach (double d in data)
                if (double.IsNaN(d))
                    str += ";";
                else
                    str += d + ";";

            sw.WriteLine(str.Substring(0, str.Length - 1));
        }
        private double[] StocksWeights(double[][] data)
        {
            double[] weights = new double[data[0].Length];
            double[] moveSize = new double[data[0].Length];

            for (int i = 0; i < moveSize.Length; ++i)
                moveSize[i] = data.Select(x => Math.Abs(x[i])).Average();

            for (int i = 0; i < weights.Length; ++i)
                weights[i] = moveSize.Max() / moveSize[i];

            return weights;
        }
        private double[][] LoadPrices(string filename)
        {
            // Load data
            string[] lines = File.ReadAllLines(filename);
            double[][] data = lines.Skip(1).Select(x => x.Split(new char[] { ';' }).Skip(1).Take(Config.BestNCompanies).Select(y => y.Trim() == "NaN" ? double.NaN : double.Parse(y)).ToArray()).ToArray();

            // Fill NaN hodnoty with last known value
            for (int col = 0; col < data[0].Length; ++col)
            {
                for (int row = 1; row < data.Length; ++row)
                    if (double.IsNaN(data[row][col]))
                        data[row][col] = data[row - 1][col];
                for (int row = data.Length - 2; row >= 0; --row)
                    if (double.IsNaN(data[row][col]))
                        data[row][col] = data[row + 1][col];
            }

            // We start from 0 + Predict (see RnnTable-CreateInputAndOutput)
            return data.Skip(Config.Predict).ToArray();
        }
    }
}
