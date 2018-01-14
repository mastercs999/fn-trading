using DataCenter.Data;
using DataCenter.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Types;

namespace DataCenter._06_WikiTrends
{
    internal class WikiTrends : DataSource
    {
        public WikiTrends() : base() { }
        public WikiTrends(bool reload) : base(reload) { }

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
            Parse(internalData);

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
            int drawEvery = Utils.PercentIntervalByLength(products.Count + DataManager.Config.ImportantWords.Length);

            // Download
            try
            {
                for (int i = 0; i < products.Count + DataManager.Config.ImportantWords.Length; ++i)
                {
                    // Downloading info
                    string word, filePrefix;
                    if (i < products.Count)
                    {
                        Product p = products[i];

                        word = HttpUtility.HtmlDecode(p.Name.Remove(",", "(The)", "Inc.", "Corporation", "Company", "Corp."));
                        filePrefix = "Symbol-" + p.Symbol;
                    }
                    else
                    {
                        word = DataManager.Config.ImportantWords[i - products.Count].Capitalize();
                        filePrefix = "Word-" + word;
                    }

                    // Download stats
                    string query = WebUtility.UrlEncode(word);
                    string url = "http://www.wikipediatrends.com/csv.php?query[]=" + query;
                    string targetFile = Path.Combine(Folder, filePrefix + ".csv");
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(url, targetFile, null, null);

                    // Check if downloaded any data
                    if (File.ReadAllLines(targetFile).Length <= 2)
                    {
                        // Delete file with empty stats
                        File.Delete(targetFile);

                        // Download suggestions
                        url = "http://www.wikipediatrends.com/typeahead.php?q=" + query;
                        targetFile = Path.Combine(Folder, filePrefix + ".json");
                        if (Reload || !File.Exists(targetFile))
                            await Downloader.DownloadFileAsync(url, targetFile, null, null);

                        // Get first suggestion
                        JArray json = JArray.Parse(File.ReadAllText(targetFile));
                        if (json.Count == 0)
                            continue;
                        JObject first = json[0] as JObject;
                        string title = (string)first["title"];

                        // Download stats
                        url = "http://www.wikipediatrends.com/csv.php?query[]=" + WebUtility.UrlEncode(title);
                        targetFile = Path.Combine(Folder, filePrefix + ".csv");
                        if (Reload || !File.Exists(targetFile))
                            await Downloader.DownloadFileAsync(url, targetFile, null, null);
                    }

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / (products.Count + DataManager.Config.ImportantWords.Length) * 100.0), ConsoleColor.Gray);
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
        private void Parse(_InternalData internalData)
        {
            string prefix = "2/3 Parsing...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Files to parse
            string[] files = Directory.GetFiles(Folder).Where(x => x.Contains(".csv")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(files.Length);

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < files.Length; ++i)
                    {
                        // Load lines
                        string[] lines = File.ReadAllLines(files[i]).Skip(2).Where(x => !String.IsNullOrWhiteSpace(x)).ToArray();
                        
                        // Parse lines
                        int previousValue = -1;
                        foreach (string l in lines)
                        {
                            string[] parts = l.Split(new char[] { ',', '/', '"', '.' }).Where(x => !String.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

                            int year = int.Parse(parts[0]);
                            int month = int.Parse(parts[1]);
                            int day = int.Parse(parts[2]);

                            int value = int.Parse(parts[3]);

                            // Do not save first zeros, or save previous value
                            if (value == 0 && previousValue == -1)
                                continue;
                            else if (value == 0 && previousValue != -1)
                                value = previousValue;
                            previousValue = value;

                            internalData.Events.Add(new _Event()
                            {
                                Name = Path.GetFileNameWithoutExtension(files[i]).Split(new char[] { '-' })[1],
                                Date = new DateTime(year, month, day),
                                Value = value
                            });
                        }

                        // Update progress bar
                        if (i % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / files.Length * 100.0), ConsoleColor.Gray);
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

                int drawEvery = Utils.PercentIntervalByLength(cache.Name.Count * 2);

                // Save to datacontainer
                for (int i = 0; i < cache.Name.Count; ++i)
                {
                    // Create forex data
                    _WikiTrendsData wtd = new _WikiTrendsData()
                    {
                        Value = cache.Value[i]
                    };

                    // Add to data container
                    dataContainer.GetEvent(new DateTime(cache.Date[i])).WikiTrendsDatas.Add(cache.Name[i], wtd);

                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50 + (double)i / cache.Name.Count * 50), ConsoleColor.Gray);
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
