using DataCenter.Data;
using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;

namespace DataCenter._02_HistoricalPrices
{
    internal class HistoricalPrices : DataSource
    {
        public HistoricalPrices() : base() { }
        public HistoricalPrices(bool reload) : base(reload) { }

        public override async Task Prepare(DataContainer dataContainer)
        {
            // Delete old data
            if (Reload)
                Directory.Delete(Folder, true);

            // Create folder
            Directory.CreateDirectory(Folder);

            // Inernal data
            _InternalData internalData = new _InternalData();

            // Draw header
            base.DrawHeader();

            // Download
            await Download(dataContainer.Products);

            // Parse
            Parse(dataContainer.Products, internalData);

            // SeDeSerialize
            Cache(dataContainer, internalData);

            // End info
            Console.WriteLine();
        }

        private async Task Download(List<Product> products)
        {
            string prefix = "1/3 Downloading...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(products.Count);

            // Basic url
            string url = "http://real-chart.finance.yahoo.com/table.csv?s=___&d=0&e=0&f=2100&g=d&a=0&b=0&c=1900&ignore=.csv";

            // Download
            try
            {
                for (int i = 0; i < products.Count; ++i)
                {
                    // Product
                    Product p = products[i];

                    // Prepare url
                    string productUrl = url.Replace("___", p.Symbol);

                    // Target file
                    string targetFile = Path.Combine(Folder, p.Symbol + ".csv");

                    // Download if needed
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(productUrl, targetFile, null, null);

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / products.Count * 100.0), ConsoleColor.Gray);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Utils.DrawMessage("", ex.Message, ConsoleColor.Red);
                Console.WriteLine();

                System.Environment.Exit(1);
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void Parse(List<Product> products, _InternalData internalData)
        {
            string prefix = "2/3 Parsing...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(products.Count);

            // Parsing
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                {
                    for (int i = 0; i < products.Count; ++i)
                    {
                        // Product
                        Product p = products[i];

                        // Load lines
                        string[] lines = File.ReadAllLines(Path.Combine(Folder, p.Symbol) + ".csv");

                        // Check first line validity
                        if (lines[0] != "Date,Open,High,Low,Close,Volume,Adj Close")
                            throw new Exception("Invalid header for " + p.Symbol + ".csv");

                        // Process each line
                        for (int k = 1; k < lines.Length; ++k)
                        {
                            // Split line
                            string[] columns = lines[k].Split(new char[] { ',' });

                            // Parse date
                            DateTime date = DateTime.ParseExact(columns[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);

                            // Parse price - adjusted close
                            double price = double.Parse(columns[6], CultureInfo.InvariantCulture);

                            // Set price for date
                            internalData.Events.Add(new _Event()
                            {
                                Symbol = p.Symbol,
                                Date = date,
                                Price = price
                            });
                        }

                        // Update progress bar
                        if (i % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / products.Count * 100.0), ConsoleColor.Gray);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Utils.DrawMessage("", ex.Message, ConsoleColor.Red);
                Console.WriteLine();

                System.Environment.Exit(1);
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
        private void Cache(DataContainer dataContainer, _InternalData internalData)
        {
            string prefix = "3/3 Caching...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            try
            {
                // Serialize or deserialize
                _Cache cache = null;
                if (Reload || !File.Exists(SerializedFile))
                {
                    cache = new _Cache(internalData);

                    Serializer.Serialize(SerializedFile, cache);
                }
                else
                    cache = (_Cache)Serializer.Deserialize(SerializedFile);

                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50), ConsoleColor.Gray);

                int drawEvery = Utils.PercentIntervalByLength(cache.Symbol.Count * 2);

                // Save to datacontainer
                for (int i = 0; i < cache.Symbol.Count; ++i)
                {
                    // Create event
                    _Event e = new _Event()
                    {
                        Symbol = cache.Symbol[i],
                        Date = new DateTime(cache.Date[i]),
                        Price = cache.Price[i]
                    };

                    // Add to data container
                    dataContainer.GetEvent(e.Date).ProductsDatas[e.Symbol].Price = e.Price;

                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50 + (double)i / cache.Symbol.Count * 50), ConsoleColor.Gray);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Utils.DrawMessage("", ex.Message, ConsoleColor.Red);
                Console.WriteLine();

                System.Environment.Exit(1);
            }

            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 100), ConsoleColor.Green);
            Console.WriteLine();
        }
    }
}
