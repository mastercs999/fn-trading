using CsQuery;
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
using Types;

namespace DataCenter._07_Futures
{
    internal class Futures : DataSource
    {
        public Futures() : base() { }
        public Futures(bool reload) : base(reload) { }

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

            // Download main futures page
            await DownloadPage();

            // Download futures list
            await DownloadLists();

            // Parse
            ParseNames(internalData);

            // Download info pages
            await DownloadInfo(internalData);

            // Extract datasets address
            ParseDatasetsUrls(internalData);

            // Download searches
            await DownloadSearches(internalData);

            // Download datasets
            await DownloadDatasets(internalData);

            // Choose datasets
            ChooseBestDataset();

            // Parse datasets
            ParseDatasets(internalData);

            // Clean data
            Clean(internalData);

            // SeDeSerialize
            Cache(dataContainer, internalData);

            // End info
            Console.WriteLine();
        }

        private async Task DownloadPage()
        {
            string prefix = "1/11 Downloading main...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Directory
            Directory.CreateDirectory(Path.Combine(Folder, "Main"));

            // Download
            try
            {
                // Download stats
                string url = "https://www.quandl.com/collections/futures";
                string targetFile = Path.Combine(Folder, "Main", "Futures.html");
                if (Reload || !File.Exists(targetFile))
                    await Downloader.DownloadFileAsync(url, targetFile, progress =>
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
        private async Task DownloadLists()
        {
            string prefix = "2/11 Downloading lists...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            try
            {
                // Load file
                string content = File.ReadAllText(Path.Combine(Folder, "Main", "Futures.html"));

                // Parse html
                CQ dom = new CQ(content);

                // Get links to futures
                string[] links = dom["#content > noscript > p > i > b > a"].Select(x => x.Attributes["href"]).Distinct().ToArray();

                int drawEvery = Utils.PercentIntervalByLength(links.Length);

                // Directory
                Directory.CreateDirectory(Path.Combine(Folder, "Lists"));

                // Download pages with complete lists
                for (int i = 0; i < links.Length; ++i)
                {
                    string url = "https://www.quandl.com/collections" + links[i];
                    string targetFile = Path.Combine(Folder, "Lists", Path.GetFileName(url) + ".html");
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(url, targetFile, null, null);

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / links.Length * 100.0), ConsoleColor.Gray);
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
        private void ParseNames(_InternalData internalData)
        {
            string prefix = "3/11 Parsing names...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Files to parse
            string[] files = Directory.GetFiles(Path.Combine(Folder, "Lists")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(files.Length);

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < files.Length; ++i)
                    {
                        // Load file
                        string content = File.ReadAllText(files[i]);

                        // Parse name and url
                        CQ dom = new CQ(content);
                        List<IDomObject> rows = dom["#content > noscript > table.sheet-table > tbody > tr td:first-child a"].ToList();
                        foreach (IDomObject r in rows)
                        {
                            string name = r.TextContent;
                            string link = r.Attributes["href"];

                            internalData.NameToInfoUrl[name] = link;
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
        private async Task DownloadInfo(_InternalData internalData)
        {
            string prefix = "4/11 Downloading info...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(internalData.NameToInfoUrl.Count);

            Directory.CreateDirectory(Path.Combine(Folder, "Info"));

            try
            {
                // Download pages with complete lists
                int i = 0;
                foreach(KeyValuePair<string, string> kv in internalData.NameToInfoUrl)
                {
                    string name = kv.Key;

                    string url = "https://www.quandl.com" + kv.Value;
                    string targetFile = Path.Combine(Folder, "Info", name.Replace("/", " ").Replace("\\", " ") + ".html");
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(url, targetFile, null, null);

                    // Update progress bar
                    if (i++ % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.NameToInfoUrl.Count * 100.0), ConsoleColor.Gray);
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
        private void ParseDatasetsUrls(_InternalData internalData)
        {
            string prefix = "5/11 Parsing DS urls...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Files to parse
            string[] files = Directory.GetFiles(Path.Combine(Folder, "Info")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(files.Length);

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < files.Length; ++i)
                    {
                        // Load file
                        string content = File.ReadAllText(files[i]);

                        // Parse name and url
                        CQ dom = new CQ(content);
                        List<string> urls = dom["#content > noscript a"].Select(x => x.Attributes["href"]).Where(x => x.StartsWith("/CHRIS/")).Distinct().ToList();

                        // Save
                        internalData.NameToDataUrl.Add(Path.GetFileNameWithoutExtension(files[i]), urls);

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
        private async Task DownloadSearches(_InternalData internalData)
        {
            string prefix = "6/11 Downloading search...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(internalData.NameToDataUrl.Count);

            Directory.CreateDirectory(Path.Combine(Folder, "Searches"));

            try
            {
                // Download pages with complete lists
                int i = 0;
                foreach (KeyValuePair<string, List<string>> kv in internalData.NameToDataUrl)
                {
                    string name = kv.Key;

                    // Create directory
                    Directory.CreateDirectory(Path.Combine(Folder, "Searches", name));

                    foreach (string access in kv.Value)
                    {
                        // Check url
                        string url = "https://www.quandl.com/api/v3/datasets.csv?database_code=CHRIS&query=" + WebUtility.UrlEncode(Path.GetFileName(access)) + "&api_key=" + DataManager.Config.QuandlApiKey;
                        string targetFile = Path.Combine(Folder, "Searches", name, Path.GetFileName(access) + ".csv");
                        if (Reload || !File.Exists(targetFile))
                            await Downloader.DownloadFileAsync(url, targetFile, null, null);
                    }

                    // Update progress bar
                    if (i++ % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.NameToInfoUrl.Count * 100.0), ConsoleColor.Gray);
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
        private async Task DownloadDatasets(_InternalData internalData)
        {
            string prefix = "7/11 Downloading DS...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(internalData.NameToDataUrl.Count);

            Directory.CreateDirectory(Path.Combine(Folder, "Datasets"));

            try
            {
                // Download pages with complete lists
                int i = 0;
                foreach (KeyValuePair<string, List<string>> kv in internalData.NameToDataUrl)
                {
                    string name = kv.Key;

                    // Create directory
                    Directory.CreateDirectory(Path.Combine(Folder, "Datasets", name));

                    foreach (string access in kv.Value)
                    {
                        // Check if dataset exists
                        string searchFile = Path.Combine(Folder, "Searches", name, Path.GetFileName(access) + ".csv");
                        if (File.ReadAllLines(searchFile).Skip(1).Select(x => x.Split(new char[] { ',' })[1].Trim()).Where(x => x == Path.GetFileName(access)).Count() == 0)
                            continue;

                        string url = "https://www.quandl.com/api/v3/datasets" + access + ".csv?api_key=" + DataManager.Config.QuandlApiKey;
                        string targetFile = Path.Combine(Folder, "Datasets", name, Path.GetFileName(access) + ".csv");
                        if (Reload || !File.Exists(targetFile))
                            await Downloader.DownloadFileAsync(url, targetFile, null, null);
                    }

                    // Update progress bar
                    if (i++ % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.NameToInfoUrl.Count * 100.0), ConsoleColor.Gray);
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
        private void ChooseBestDataset()
        {
            string prefix = "8/11 Choosing best DS...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Directories
            string[] directories = Directory.GetDirectories(Path.Combine(Folder, "Datasets")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(directories.Length);

            // Output directory
            Directory.CreateDirectory(Path.Combine(Folder, "Final datasets"));

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < directories.Length; ++i)
                    {
                        // Get files
                        string[] files = Directory.GetFiles(directories[i]);
                        if (files.Length == 0)
                            continue;

                        // Get file sizes
                        Dictionary<string, long> sizes = files.ToDictionary(x => x, x => (new FileInfo(x)).Length);

                        // Choose file with most size
                        string largestFile = sizes.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;

                        // Copy to data folder
                        string targetFile = Path.Combine(Folder, "Final datasets", Path.GetFileName(largestFile));
                        File.Copy(largestFile, targetFile, true);

                        // Update progress bar
                        if (i % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / directories.Length * 100.0), ConsoleColor.Gray);
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
        private void ParseDatasets(_InternalData internalData)
        {
            string prefix = "9/11 Parsing DS...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // All files to parse
            string[] files = Directory.GetFiles(Path.Combine(Folder, "Final datasets")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(files.Length);

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < files.Length; ++i)
                    {
                        // Info
                        string code = Path.GetFileNameWithoutExtension(files[i]);
                        string name = internalData.NameToDataUrl.Where(x => x.Value.Where(y => y.Contains(code)).Count() > 0).FirstOrDefault().Key;

                        // Load file
                        string[] lines = File.ReadAllLines(files[i]);

                        // Ignore files with not many data
                        if (lines.Length < 500)
                            continue;

                        // Find column with price
                        string[] headers = lines[0].Split(new char[] { ',' }).Select(x => x.ToLower()).ToArray();
                        int index = -1;
                        if (headers.Where(x => x.Contains("close")).Count() > 0)
                            index = headers.Select((v, ind) => new { column = v, index = ind }).First(x => x.column.Contains("close")).index;
                        else if (headers.Where(x => x.Contains("last")).Count() > 0)
                            index = headers.Select((v, ind) => new { column = v, index = ind }).First(x => x.column.Contains("last")).index;
                        else if (headers.Where(x => x.Contains("settle")).Count() > 0)
                            index = headers.Select((v, ind) => new { column = v, index = ind }).First(x => x.column.Contains("settle")).index;
                        else
                            continue;

                        // Parse data
                        for (int k = 1; k < lines.Length; ++k)
                        {
                            string[] parts = lines[k].Split(new char[] { ',' });

                            // Date
                            string[] dateParts = parts[0].Split(new char[] { '-' });
                            int year = int.Parse(dateParts[0]);
                            int month = int.Parse(dateParts[1]);
                            int day = int.Parse(dateParts[2]);
                            DateTime date = new DateTime(year, month, day);

                            // Value
                            if (String.IsNullOrWhiteSpace(parts[index]))
                                continue;
                            double value = double.Parse(parts[index], CultureInfo.InvariantCulture);

                            // Create event
                            internalData.Events.Add(new _Event()
                            {
                                Name = name,
                                Date = date,
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
        private void Clean(_InternalData internalData)
        {
            string prefix = "10/11 Cleaning...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Get names
            string[] names = internalData.Events.Select(x => x.Name).Distinct().ToArray();

            int drawEvery = Utils.PercentIntervalByLength(names.Length);

            // Get max data length
            int maxDataLength = 0;
            foreach (string n in names)
                maxDataLength = Math.Max(maxDataLength, internalData.Events.Where(x => x.Name == n).Count());

            // Download
            try
            {
                for (int i = 0; i < names.Length; ++i)
                {
                    // Get events of futures
                    List<_Event> futuresEvents = internalData.Events.Where(x => x.Name == names[i]).OrderBy(x => x.Date).ToList();

                    // Remove futures if not many data
                    if (futuresEvents.Count < 0.5 * maxDataLength)
                        internalData.Events.RemoveAll(x => x.Name == names[i]);

                    // Update progress bar
                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / names.Length * 100.0), ConsoleColor.Gray);
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
            string prefix = "11/11 Caching...";
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
                    _FuturesData fd = new _FuturesData()
                    {
                        Price = cache.Value[i]
                    };

                    // Add to data container
                    dataContainer.GetEvent(new DateTime(cache.Date[i])).FuturesDatas.Add(cache.Name[i], fd);

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
