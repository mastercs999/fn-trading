using DataCenter;
using RnnCenter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingCenter;
using Types;

namespace Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }
        static async Task MainAsync()
        {
            // Config
            Config config = new Config()
            {
                Reload = false,
                DataDirectory = @"..\..\..\..\data\",
                QuandlApiKey = "",
                GoogleCookies = "",
                ImportantWords = new string[] { "debt", "color", "stocks", "economics", "inflation", "restaurant", "portfolio", "metals", "housing", "dow jones", "revenue", "sell", "bonds", "risk", "car", "credit", "markets", "return", "unemployment", "leverage", "chance", "nasdaq", "money", "society", "war", "religion", "cancer", "growth", "investment", "hedge", "marriage", "transaction", "cash", "economy", "derivatives", "headlines", "profit", "loss", "office", "forex", "finance", "fed", "banking", "stock market", "fine", "crisis", "happy", "gains", "invest", "house" },
                BestNCompanies = 100,
                NormalizationWindowSize = int.MaxValue,
                DataFrom = new DateTime(1995, 1, 1),
                ValidFrom = 0.7,
                TestFrom = 0.85,
                Predict = 30,
                Random = new Random()
            };

            // Prepare data
            DataManager dataManager = new DataManager(config);
            await dataManager.PrepareData();

            // Make prediction
            RnnManager rnnManager = new RnnManager(config);
            rnnManager.Predict();

            // Decode prediction
            dataManager.DecodePrediction();

            // Make trades
            TradingSystem tradingSystem = new TradingSystem(config);
            tradingSystem.Trade();
        }
    }
}
