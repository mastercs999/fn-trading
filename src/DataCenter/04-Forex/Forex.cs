using DataCenter.Data;
using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Types;
using System.Globalization;

namespace DataCenter._04_Forex
{
    internal class Forex : DataSource
    {
        public Forex() : base() { }
        public Forex(bool reload) : base(reload) { }

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

            // Find usd pairs
            await FindUsdPairs(internalData);

            // Download historical prices
            await DownloadUsdPairs(internalData);

            // Parse
            ParsePairs(internalData);

            // Clean
            Clean(internalData);

            // SeDeSerialize
            Cache(dataContainer, internalData);

            // End info
            Console.WriteLine();
        }

        private async Task FindUsdPairs(_InternalData internalData)
        {
            string prefix = "1/5 Finding USD pairs...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Download
            try
            {
                string allUrl = "https://www.quandl.com/collections/usa/usa-currency-exchange-rate";
                string targetFile = Path.Combine(Folder, "All.html");

                // Download if needed
                if (Reload || !File.Exists(targetFile))
                    await Downloader.DownloadFileAsync(allUrl, targetFile, null, null);

                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50), ConsoleColor.Gray);

                // Get links to USD pairs
                string fileContent = File.ReadAllText(targetFile);
                string[] linksString = fileContent.AllSubstrings("/CURRFX/", "/CURRFX/".Length + 6);
                linksString = linksString.Where((x, i) => i % 2 == 0).ToArray();

                // Save urls
                internalData.Pairs = linksString.Select(x => x.Substring(x.LastIndexOf("/") + 1)).Distinct().ToList();
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
        private async Task DownloadUsdPairs(_InternalData internalData)
        {
            string prefix = "2/5 Downloading pairs...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(internalData.Pairs.Count);

            // Download
            try
            {
                for (int i = 0; i < internalData.Pairs.Count; ++i)
                {
                    string url = "https://www.quandl.com/api/v3/datasets/CURRFX/" + internalData.Pairs[i] + ".csv?api_key=" + DataManager.Config.QuandlApiKey;
                    string targetFile = Path.Combine(Folder, internalData.Pairs[i] + ".csv");

                    // Download if needed
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(url, targetFile, null, null);

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.Pairs.Count * 100.0), ConsoleColor.Gray);
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
        private void ParsePairs(_InternalData internalData)
        {
            string prefix = "3/5 Parsing...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(internalData.Pairs.Count);

            // Parsing
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                {
                    for (int i = 0; i < internalData.Pairs.Count; ++i)
                    {
                        // Pair name
                        string pairName = internalData.Pairs[i];
                        string fileName = Path.Combine(Folder, pairName + ".csv");

                        // Load lines
                        string[] lines = File.ReadAllLines(fileName);

                        // Process each line
                        for (int k = 1; k < lines.Length; ++k)
                        {
                            // Split line
                            string[] columns = lines[k].Split(new char[] { ',' });

                            // Parse date
                            DateTime date = DateTime.ParseExact(columns[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            
                            // Too old data
                            if (date < DataManager.Config.DataFrom)
                                continue;

                            // Parse price
                            double price = double.Parse(columns[1], CultureInfo.InvariantCulture);

                            // Set price for date
                            internalData.Events.Add(new _Event()
                            {
                                Pair = pairName,
                                Date = date,
                                Price = price
                            });
                        }

                        // Update progress bar
                        if (i % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.Pairs.Count * 100.0), ConsoleColor.Gray);
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
        private void Clean(_InternalData internalData)
        {
            string prefix = "4/5 Cleaning...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(internalData.Pairs.Count);

            // Get max data length
            int maxDataLength = 0;
            foreach (string pair in internalData.Pairs)
                maxDataLength = Math.Max(maxDataLength, internalData.Events.Where(x => x.Pair == pair).Count());

            // Download
            try
            {
                for (int i = 0; i < internalData.Pairs.Count; ++i)
                {
                    // Get events of pair
                    List<_Event> pairsEvents = internalData.Events.Where(x => x.Pair == internalData.Pairs[i]).OrderBy(x => x.Date).ToList();

                    // Remove pair if not many data
                    if (pairsEvents.Count < 0.6 * maxDataLength)
                        internalData.Events.RemoveAll(x => x.Pair == internalData.Pairs[i]);

                    // Remove if not many changes
                    int zeros = 0;
                    for (int k = 1; k < pairsEvents.Count; ++k)
                        if (Math.Abs(pairsEvents[k].Price - pairsEvents[k - 1].Price) < 0.0000000001)
                            ++zeros;
                    if (zeros > 0.15 * maxDataLength)
                        internalData.Events.RemoveAll(x => x.Pair == internalData.Pairs[i]);

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.Pairs.Count * 100.0), ConsoleColor.Gray);
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

                int drawEvery = Utils.PercentIntervalByLength(cache.Pair.Count * 2);

                // Save to datacontainer
                for (int i = 0; i < cache.Pair.Count; ++i)
                {
                    // Create forex data
                    _ForexData fd = new _ForexData()
                    {
                        Price = cache.Price[i]
                    };

                    // Add to data container
                    dataContainer.GetEvent(new DateTime(cache.Date[i])).ForexDatas.Add(cache.Pair[i], fd);

                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50 + (double)i / cache.Pair.Count * 50), ConsoleColor.Gray);
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
