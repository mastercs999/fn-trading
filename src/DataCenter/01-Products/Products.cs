using DataCenter;
using DataCenter.Helpers;
using Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DataCenter.Data;
using System.Threading;
using System.Text.RegularExpressions;

namespace DataCenter._01_Products
{
    internal class Products : DataSource
    {
        public Products() : base() { }
        public Products(bool reload) : base(reload) { }

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
            await Download();

            // Parse
            Parse(internalData);

            // Find starting dates
            await FindStartDate(internalData);

            // Filter
            Filter(internalData);

            // SeDeSerialize
            Cache(dataContainer, internalData);

            // End info
            Console.WriteLine();
        }

        private async Task Download()
        {
            string prefix = "1/5 Downloading...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Download
            try
            {
                // Target file
                string targetFile = Path.Combine(Folder, "Products.csv");

                // Download if needed
                if (Reload || !File.Exists(targetFile))
                    await Downloader.DownloadFileAsync("http://www.nasdaq.com/screening/companies-by-name.aspx?letter=0&exchange=nyse&render=download", targetFile, progress =>
                    {
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, progress), ConsoleColor.Gray);
                    }, null);
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
        private void Parse(_InternalData internalData)
        {
            string prefix = "2/5 Parsing...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            try
            {
                // Parse only if we have not parsed
                if (Reload || !File.Exists(SerializedFile))
                {
                    // Load data
                    string[] lines = File.ReadAllLines(Path.Combine(Folder, "Products.csv"));

                    // Count when we update progress bar
                    int drawEvery = Utils.PercentIntervalByLength(lines.Length);

                    // Check header
                    if (lines[0] != "\"Symbol\",\"Name\",\"LastSale\",\"MarketCap\",\"IPOyear\",\"Sector\",\"industry\",\"Summary Quote\",")
                        throw new Exception("Unknown file header");

                    // Parse product names
                    for (int i = 1; i < lines.Length; ++i)
                    {
                        // Get columns
                        string[] columns = lines[i].Split("\",\"");

                        // Extract symbol
                        string tickerSymbol = columns[0];
                        tickerSymbol = tickerSymbol.Substring(1).Trim();

                        // Extract name
                        string name = columns[1];

                        // Extract market capitalization
                        string mcStr = columns[3];
                        double mc = -1;
                        if (mcStr != "n/a")
                        {
                            if (mcStr[0] != '$')
                                throw new Exception("Market capitalization - missin $ symbol (" + i + "," + lines[i].Split(new char[] { ',' })[3] + ")");
                            mcStr = mcStr.Substring(1, mcStr.Length - 1);
                            if (mcStr.Last() != 'B' && mcStr.Last() != 'M' && !Char.IsDigit(mcStr.Last()))
                                throw new Exception("Market capitalization - not millions, not billions and not normal number (" + i + "," + lines[i].Split(new char[] { ',' })[3] + ")");
                            
                            int multiple = 1;
                            if (mcStr.Last() == 'B')
                            {
                                multiple = 1000000000;
                                mcStr = mcStr.Substring(0, mcStr.Length - 1);
                            }
                            else if (mcStr.Last() == 'M')
                            {
                                multiple = 1000000;
                                mcStr = mcStr.Substring(0, mcStr.Length - 1);
                            }

                            mc = double.Parse(mcStr, CultureInfo.InvariantCulture);
                            mc *= multiple;
                        }

                        // Only valid products
                        if ((new Regex("^[a-zA-Z0-9]*$")).IsMatch(tickerSymbol))
                        {
                            // Create new products
                            _Product p = new _Product();
                            p.Symbol = tickerSymbol;
                            p.Name = name;
                            p.MarketCapitalizationInMillions = mc;

                            // Add to list
                            internalData.Products.Add(p);
                        }

                        // Update progress bar
                        if (i % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / lines.Length * 100.0), ConsoleColor.Gray);
                    }

                    // Check some properties
                    if (internalData.Products.Count < 1000)
                        throw new Exception("There are just " + internalData.Products.Count + " products");
                    int yy = internalData.Products.Select(x => x.Symbol).Where(x => x == "IBM" || x == "JPM" || x == "SNHN").Count();
                    if (internalData.Products.Select(x => x.Symbol).Where(x => x == "IBM" || x == "JPM" || x == "SNHN").Count() != 3)
                        throw new Exception("Some products are missing");
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
        private async Task FindStartDate(_InternalData internalData)
        {
            string prefix = "3/5 Finding first date...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(internalData.Products.Count);

            // Basic url
            string url = "http://real-chart.finance.yahoo.com/table.csv?s=___&d=0&e=0&f=2100&g=d&a=0&b=0&c=1900&ignore=.csv";

            // Destination folder
            string targetFolder = Path.Combine(Folder, "ProductsHistory");

            // Create folder for product
            Directory.CreateDirectory(targetFolder);

            // Download
            try
            {
                for (int i = 0; i < internalData.Products.Count; ++i)
                {
                    // Product
                    _Product p = internalData.Products[i];

                    // Prepare url
                    string productUrl = url.Replace("___", p.Symbol);

                    // Target file
                    string targetFile = Path.Combine(targetFolder, p.Symbol + ".csv");

                    // Download if needed
                    if (Reload || !File.Exists(targetFile))
                        try
                        {
                            await Downloader.DownloadFileAsync(productUrl, targetFile, null, null);
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "The remote server returned an error: (404) Not Found.")
                            {
                                internalData.Products.RemoveAt(i);
                                --i;
                                if (File.Exists(targetFile))
                                    File.Delete(targetFile);
                            }
                            else
                                throw ex;
                        }

                    if (File.Exists(targetFile))
                    {
                        // Parse only if we have not parsed
                        if (Reload || !File.Exists(SerializedFile))
                        {
                            // Store start date
                            string[] lines = File.ReadAllLines(targetFile);
                            string date = lines.Last().Split(new char[] { ',' })[0];

                            p.StartDate = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        }
                    }

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.Products.Count * 100.0), ConsoleColor.Gray);
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
        private void Filter(_InternalData internalData)
        {
            string prefix = "4/5 Filtering...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            try
            {
                // Filter only if we have not parsed
                if (Reload || !File.Exists(SerializedFile))
                {
                    // Order by history length
                    internalData.Products = internalData.Products.OrderBy(x => x.StartDate.Ticks).ToList();

                    Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50), ConsoleColor.Green);

                    // Take only N best
                    internalData.Products = internalData.Products.Take(DataManager.Config.BestNCompanies).ToList();
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
            string prefix = "5/5 Caching...";
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

                // Apply data do global data
                for (int i = 0; i < cache.Symbols.Count; ++i)
                {
                    Product p = new Product()
                    {
                        Symbol = cache.Symbols[i],
                        Name = cache.Names[i]
                    };

                    dataContainer.Products.Add(p);
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
