using DataCenter;
using DataCenter._01_Products;
using DataCenter._02_HistoricalPrices;
using DataCenter._03_Weather;
using DataCenter.Data;
using DataCenter.Helpers;
using DataCenter.Interface;
using Accord.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;
using DataCenter._04_Forex;
using DataCenter._05_Google;
using DataCenter._06_WikiTrends;
using DataCenter._07_Futures;
using DataCenter._08_Fundamentals;

namespace DataCenter
{
    public class DataManager
    {
        public static Config Config { get; set; }

        public DataManager(Config config)
        {
            // Save config
            Config = config;

            // Create data directory
            Directory.CreateDirectory(Config.DataDirectory);
        }

        public async Task PrepareData()
        {
            // Prepare data only if needed
            if (Config.Reload || !File.Exists(Config.FullDataFile))
            {
                // Data
                DataContainer dataContainer = new DataContainer();

                // Data list
                DataSource[] dataSources = new DataSource[] {
                    new Products(Config.Reload),
                    new HistoricalPrices(Config.Reload),
                    new Weather(Config.Reload),
                    new Forex(Config.Reload),
                    new Google(Config.Reload),
                    new WikiTrends(Config.Reload),
                    new Futures(Config.Reload),
                    new Fundamentals(Config.Reload)
                };

                // Go to data folder
                Directory.SetCurrentDirectory(Config.DataDirectory);

                // Prepare all data
                foreach (DataSource ds in dataSources)
                    await ds.Prepare(dataContainer);

                // Create datatable
                FullTable fullTable = new FullTable();
                fullTable.CreateFullTable(dataContainer);

                // Export data
                Directory.CreateDirectory(Config.TradingDataFolder);
                Exporter.ExportCSV(fullTable.DataTable, true, Config.FullDataFile, "Exporting full data");

                Console.WriteLine();
            }

            // Create RNN data
            if (Config.Reload ||
                !File.Exists(Config.RnnTrainXFile) ||
                !File.Exists(Config.RnnTrainYFile) ||
                !File.Exists(Config.RnnValidXFile) ||
                !File.Exists(Config.RnnValidYFile) ||
                !File.Exists(Config.RnnTestXFile) ||
                !File.Exists(Config.RnnTestYFile))
            {
                // Create datatable
                RnnTable rnnTable = new RnnTable();
                rnnTable.CreateRnnTable();

                // Export data
                Exporter.ExportRnnConfig(rnnTable.RnnConfig, Config.RnnConfigFile);
                Exporter.ExportCSV(rnnTable.TrainX, Config.RnnTrainXFile);
                Exporter.ExportCSV(rnnTable.TrainY, Config.RnnTrainYFile);
                Exporter.ExportCSV(rnnTable.ValidX, Config.RnnValidXFile);
                Exporter.ExportCSV(rnnTable.ValidY, Config.RnnValidYFile);
                Exporter.ExportCSV(rnnTable.TestX, Config.RnnTestXFile);
                Exporter.ExportCSV(rnnTable.TestY, Config.RnnTestYFile);

                Console.WriteLine();
            }
        }
        public void DecodePrediction()
        {
            if (!Config.Reload &&
                File.Exists(Config.FullPredictedXFile) &&
                File.Exists(Config.FullPredictedYFile))
                return;

            string prefix = "Decoding predictions...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Load config
            RnnConfig rnnConfig = (RnnConfig)Serializer.Deserialize(Config.RnnConfigFile);

            // Source files
            string[] sourceFiles = new string[] { 
                Config.RnnPredictedXFile, 
                Config.RnnPredictedYFile
            };

            // Destination files
            string[] destinationFiles = new string[] { 
                Config.FullPredictedXFile, 
                Config.FullPredictedYFile 
            };

            // Decode files
            for (int i = 0; i < sourceFiles.Length; ++i)
            {
                // Load source file
                double[][] data = File.ReadAllLines(sourceFiles[i]).Select(x => x.Split(new char[] { ';' }).Select(y => double.Parse(y)).ToArray()).ToArray();

                // Remove clipped values
                if (i == 1)
                    for (int k = 0; k < data.Length; ++k)
                        for (int m = 0; m < data[k].Length; ++m)
                        {
                            double? clipped = rnnConfig.GetClippedValue(k, m);
                            if (clipped != null)
                                data[k][m] = (double)clipped;
                        }

                //// Unnormalize after PCA
                //for (int k = 0; k < data.Length; ++k)
                //    for (int m = 0; m < data[k].Length; ++m)
                //        data[k][m] = rnnConfig.GetTransformed(1, k, m, data[k][m]);

                //// Decode from PCA
                //data = data.Dot(rnnConfig.PcaCoefficients[1]);

                // Unnormalize
                for (int k = 0; k < data.Length; ++k)
                    for (int m = 0; m < data[k].Length; ++m)
                        data[k][m] = rnnConfig.GetTransformed(0, k, m, data[k][m]);

                // Flush
                File.WriteAllLines(destinationFiles[i], data.Select(x => string.Join(";", x)));

                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)(i + 1) / sourceFiles.Length * 100.0), ConsoleColor.Gray);
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
    }
}
