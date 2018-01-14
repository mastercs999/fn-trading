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
    internal class FullTable
    {
        public DataTable DataTable { get; set; }

        public void CreateFullTable(DataContainer dataContainer)
        {
            // Create full table
            CreateFullTableFromData(dataContainer);

            // Clean full table
            RemoveUselessLines(dataContainer);
            CleanFullTable(dataContainer);
        }
        
        private void CreateFullTableFromData(DataContainer dataContainer)
        {
            string prefix = "Creating full data...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Create datatable
            DataTable = new DataTable();

            // First column is date
            DataTable.Columns.Add("Date", typeof(DateTime));

            // Another columns are products
            List<string> products = GetAllProducts(dataContainer);
            foreach (string p in products)
                DataTable.Columns.Add("2_" + p, typeof(double));

            // Weather
            List<string> stations = GetAllStations(dataContainer);
            foreach (string s in stations)
            {
                DataTable.Columns.Add("3_" + s + "-PRCP", typeof(double));
                DataTable.Columns.Add("3_" + s + "-SNOW", typeof(double));
                DataTable.Columns.Add("3_" + s + "-TMAX", typeof(double));
                DataTable.Columns.Add("3_" + s + "-TMIN", typeof(double));
            }

            // Forex
            List<string> pairs = GetAllPairs(dataContainer);
            foreach (string p in pairs)
                DataTable.Columns.Add("4_" + p, typeof(double));

            // Google
            List<string> words = GetAllWords(dataContainer);
            foreach (string w in words)
                DataTable.Columns.Add("5_" + w, typeof(double));

            // WikiTrends
            List<string> wikiWords = GetAllWikiWords(dataContainer);
            foreach (string w in wikiWords)
                DataTable.Columns.Add("6_" + w, typeof(double));

            // Futures
            List<string> futuresNames = GetAllFutures(dataContainer);
            foreach (string w in futuresNames)
                DataTable.Columns.Add("7_" + w, typeof(double));

            // Fundamentals
            List<string> fundamentalsNames = GetAllFundamentals(dataContainer);
            foreach (string w in fundamentalsNames)
                DataTable.Columns.Add("8_" + w, typeof(double));

            DataTable.BeginLoadData();

            // Pocet radku
            int rowLength = DataTable.Columns.Count;

            // Adding rows
            int drawEvery = Utils.PercentIntervalByLength(dataContainer.Events.Count);
            int i = 0;
            foreach (KeyValuePair<DateTime, Event> kv in dataContainer.Events)
            {
                // Date
                DateTime date = kv.Key;

                // Event
                Event e = kv.Value;

                // Row data
                List<object> row = new List<object>(rowLength);

                // Add date
                row.Add(date);

                // Add product prices
                foreach (string p in products)
                    row.Add(e.ProductsDatas[p].Price);

                // Add weather data
                foreach (string s in stations)
                {
                    _WeatherData weather = null;
                    if (!e.WeatherDatas.TryGetValue(s, out weather))
                        weather = new _WeatherData();

                    row.Add(weather.Precipitation);
                    row.Add(weather.Snow);
                    row.Add(weather.TemperatureMax);
                    row.Add(weather.TemperatureMin);
                }

                // Add forex data
                foreach (string s in pairs)
                {
                    _ForexData pair = null;
                    if (!e.ForexDatas.TryGetValue(s, out pair))
                        pair = new _ForexData();

                    row.Add(pair.Price);
                }

                // Add google data
                foreach (string w in words)
                {
                    _GoogleData word = null;
                    if (!e.GoogleDatas.TryGetValue(w, out word))
                        word = new _GoogleData();

                    row.Add(word.Value);
                }

                // Add WikiTrends data
                foreach (string w in wikiWords)
                {
                    _WikiTrendsData word = null;
                    if (!e.WikiTrendsDatas.TryGetValue(w, out word))
                        word = new _WikiTrendsData();

                    row.Add(word.Value);
                }

                // Add Futures data
                foreach (string w in futuresNames)
                {
                    _FuturesData fd = null;
                    if (!e.FuturesDatas.TryGetValue(w, out fd))
                        fd = new _FuturesData();

                    row.Add(fd.Price);
                }

                // Add Fundamentals data
                foreach (string w in fundamentalsNames)
                {
                    _FundamentalsData fd = null;
                    if (!e.FundamentalsData.TryGetValue(w, out fd))
                        fd = new _FundamentalsData();

                    row.Add(fd.Value);
                }

                // Add row to datatable
                DataTable.Rows.Add(row.ToArray());

                // Update progress bar
                if (i++ % drawEvery == 0)
                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / dataContainer.Events.Count * 100.0), ConsoleColor.Gray);
            }

            DataTable.EndLoadData();

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void RemoveUselessLines(DataContainer dataContainer)
        {
            string prefix = "Removing useless lines...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Drawing data
            int drawEvery = Utils.PercentIntervalByLength(DataTable.Rows.Count);

            // Remove useless rows
            int rowCount = DataTable.Rows.Count;
            for (int i = 0, row = 0; i < rowCount; ++i, ++row)
            {
                if (i % drawEvery == 0)
                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / rowCount * 100.0), ConsoleColor.Gray);

                // Delete old data
                if (((DateTime)DataTable.Rows[row][0]) < DataManager.Config.DataFrom)
                {
                    DataTable.Rows.RemoveAt(row--);
                    continue;
                }

                // Remove row if it is weekend
                DateTime date = (DateTime)DataTable.Rows[row][0];
                if ((date.DayOfWeek == DayOfWeek.Saturday) || (date.DayOfWeek == DayOfWeek.Sunday))
                {
                    DataTable.Rows.RemoveAt(row--);
                    continue;
                }

                // Delete row if the day was not traded
                bool wasPrice = false;
                for (int col = 1; col < 1 + dataContainer.Products.Count; ++col)
                    if (!double.IsNaN((double)DataTable.Rows[row][col]))
                    {
                        wasPrice = true;
                        break;
                    }
                if (!wasPrice)
                {
                    DataTable.Rows.RemoveAt(row--);
                    continue;
                }
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void CleanFullTable(DataContainer dataContainer)
        {
            string prefix = "Cleaning full data...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Drawing data
            int columnsToProcess = DataTable.Columns.Count - dataContainer.Products.Count - 1;
            int drawEvery = Utils.PercentIntervalByLength(columnsToProcess);

            // Remove columns with same values (except products and date)
            for (int col = DataTable.Columns.Count - 1; col >= dataContainer.Products.Count + 1; --col)
            {
                // Find if all values in training data are same
                bool notSameValue = false;
                double defaultValue = (double)DataTable.Rows[0][col];
                for (int row = 0; row < (int)Math.Round(DataTable.Rows.Count * DataManager.Config.ValidFrom); ++row)
                {
                    if ((double.IsNaN(defaultValue) && !double.IsNaN((double)DataTable.Rows[row][col]) && (double)DataTable.Rows[row][col] != 0) ||
                        (Math.Abs((double)DataTable.Rows[row][col] - defaultValue) > 0.00000000001))
                    {
                        notSameValue = true;
                        break;
                    }
                }

                // Only same values, remove column
                if (!notSameValue)
                    DataTable.Columns.RemoveAt(col);

                int progress = columnsToProcess - (col - dataContainer.Products.Count - 1);
                if (progress % drawEvery == 0)
                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)progress / columnsToProcess * 100.0), ConsoleColor.Gray);
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }

        private List<string> GetAllProducts(DataContainer dataContainer)
        {
            return dataContainer.Products.Select(x => x.Symbol).ToList();
        }
        private List<string> GetAllStations(DataContainer dataContainer)
        {
            HashSet<string> stations = new HashSet<string>();

            foreach (Event e in dataContainer.Events.Values)
                stations.UnionWith(e.WeatherDatas.Keys);

            return stations.ToList();
        }
        private List<string> GetAllPairs(DataContainer dataContainer)
        {
            HashSet<string> pairs = new HashSet<string>();

            foreach (Event e in dataContainer.Events.Values)
                pairs.UnionWith(e.ForexDatas.Keys);

            return pairs.ToList();
        }
        private List<string> GetAllWords(DataContainer dataContainer)
        {
            HashSet<string> words = new HashSet<string>();

            foreach (Event e in dataContainer.Events.Values)
                words.UnionWith(e.GoogleDatas.Keys);

            return words.ToList();
        }
        private List<string> GetAllWikiWords(DataContainer dataContainer)
        {
            HashSet<string> words = new HashSet<string>();

            foreach (Event e in dataContainer.Events.Values)
                words.UnionWith(e.WikiTrendsDatas.Keys);

            return words.ToList();
        }
        private List<string> GetAllFutures(DataContainer dataContainer)
        {
            HashSet<string> futures = new HashSet<string>();

            foreach (Event e in dataContainer.Events.Values)
                futures.UnionWith(e.FuturesDatas.Keys);

            return futures.ToList();
        }
        private List<string> GetAllFundamentals(DataContainer dataContainer)
        {
            HashSet<string> fundamentals = new HashSet<string>();

            foreach (Event e in dataContainer.Events.Values)
                fundamentals.UnionWith(e.FundamentalsData.Keys);

            return fundamentals.ToList();
        }
    }
}
