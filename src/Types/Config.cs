using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Types
{
    public class Config
    {
        public bool Reload { get; set; }
        public string DataDirectory { get; set; }
        public string QuandlApiKey { get; set; }
        public string GoogleCookies { get; set; }
        public string[] ImportantWords { get; set; }
        public int BestNCompanies { get; set; }
        public int NormalizationWindowSize { get; set; }
        public DateTime DataFrom { get; set; }
        public double ValidFrom { get; set; }
        public double TestFrom { get; set; }
        public int Predict { get; set; }
        public Random Random { get; set; }

        public string TradingDataFolder { get { return "TradingData"; } }
        public string FullDataFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "1 - FullData.csv"); } }
        public string RnnConfigFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnConfig.bin"); } }
        public string RnnTrainXFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnTrainX.csv"); } }
        public string RnnTrainYFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnTrainY.csv"); } }
        public string RnnValidXFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnValidX.csv"); } }
        public string RnnValidYFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnValidY.csv"); } }
        public string RnnTestXFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnTestX.csv"); } }
        public string RnnTestYFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "2 - RnnTestY.csv"); } }
        public string RnnPredictedXFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "3 - RnnPredictedX.csv"); } }
        public string RnnPredictedYFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "3 - RnnPredictedY.csv"); } }
        public string FullPredictedXFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "4 - FullPredictedX.csv"); } }
        public string FullPredictedYFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "4 - FullPredictedY.csv"); } }
        public string FullPredictedYYFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "4 - FullPredictedYY.csv"); } }
        public string FullPredictedZFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "4 - FullPredictedZ.csv"); } }
        public string TradesFile { get { return Path.Combine(DataDirectory, TradingDataFolder, "5 - Trades.csv"); } }
    }
}
