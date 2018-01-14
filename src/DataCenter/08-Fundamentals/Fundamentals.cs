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

namespace DataCenter._08_Fundamentals
{
    internal class Fundamentals : DataSource
    {
        public Fundamentals() : base() { }
        public Fundamentals(bool reload) : base(reload) { }

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

            // Download main fundamental page
            await DownloadPage();

            // Download fundamentals list
            await DownloadLists();

            // Parse data url
            ParseUrls(internalData);

            // Download datasets
            await DownloadDatasets(internalData);

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
            string prefix = "1/7 Downloading main...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Directory
            Directory.CreateDirectory(Path.Combine(Folder, "Main"));

            // Download
            try
            {
                // Download stats
                string url = "https://www.quandl.com/collections/usa";
                string targetFile = Path.Combine(Folder, "Main", "Overview.html");
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
            string prefix = "2/7 Downloading lists...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            try
            {
                // Load file
                string content = File.ReadAllText(Path.Combine(Folder, "Main", "Overview.html"));

                // Parse html
                CQ dom = new CQ(content);

                // Get links to intereseting data
                string[] links = dom["#content > noscript > p > i"].Where(x => x.TextContent.Contains("Detailed collection")).Select(x => x.FirstElementChild.Attributes["href"]).Distinct().ToArray();

                int drawEvery = Utils.PercentIntervalByLength(links.Length);

                // Directory
                Directory.CreateDirectory(Path.Combine(Folder, "Lists"));

                // Download pages with complete lists
                for (int i = 0; i < links.Length; ++i)
                {
                    string url = "https://www.quandl.com" + links[i];
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
        private void ParseUrls(_InternalData internalData)
        {
            string prefix = "3/7 Parsing urls...";
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
                        List<IDomObject> rows = dom["#content > noscript > table.sheet-table > tbody > tr:nth-child(2) > td > a.value-neutral"].ToList();
                        foreach (IDomObject r in rows)
                        {
                            string name = r.TextContent;
                            string link = r.Attributes["href"];

                            internalData.NameToDataUrl[name] = link;
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
        private async Task DownloadDatasets(_InternalData internalData)
        {
            string prefix = "4/7 Downloading DS...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            int drawEvery = Utils.PercentIntervalByLength(internalData.NameToDataUrl.Count);

            // Directory
            Directory.CreateDirectory(Path.Combine(Folder, "Datasets"));

            try
            {
                // Download pages with complete lists
                int i = 0;
                foreach (KeyValuePair<string, string> kv in internalData.NameToDataUrl)
                {
                    string url = "https://www.quandl.com/api/v3/datasets" + kv.Value + ".csv?api_key=" + DataManager.Config.QuandlApiKey;
                    string targetFile = Path.Combine(Folder, "Datasets", kv.Key.RemoveIllegalChars().Trim() + ".csv");
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(url, targetFile, null, null);

                    // Update progress bar
                    if (i++ % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / internalData.NameToDataUrl.Count * 100.0), ConsoleColor.Gray);
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
            string prefix = "5/7 Parsing DS...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // All files to parse
            string[] files = Directory.GetFiles(Path.Combine(Folder, "Datasets")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(files.Length);

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < files.Length; ++i)
                    {
                        // Info
                        string name = Path.GetFileNameWithoutExtension(files[i]);

                        // Load file
                        string[] lines = File.ReadAllLines(files[i]);

                        // Short files will not be parsed
                        if (lines.Length < 5)
                            continue;

                        // Find average date difference
                        List<DateTime> dates = new List<DateTime>();
                        for (int k = 1; k < lines.Length; ++k)
                        {
                            string[] parts = lines[k].Split(new char[] { ',' });
                            string[] dateParts = parts[0].Split(new char[] { '-' });
                            int year = int.Parse(dateParts[0]);
                            int month = int.Parse(dateParts[1]);
                            int day = int.Parse(dateParts[2]);

                            dates.Add(new DateTime(year, month, day));
                        }
                        double sum = 0;
                        int count = 0;
                        for (int k = 1; k < dates.Count; ++k)
                        {
                            sum += Math.Abs((dates[k] - dates[k - 1]).TotalDays);
                            ++count;
                        }
                        double avgDiff = sum / count;

                        // Find number, how many times we will copy value back
                        int copyCount = 1;
                        if (avgDiff > 300 && avgDiff < 400)
                            copyCount = 365;
                        else if (avgDiff > 60 && avgDiff < 120)
                            copyCount = 93;
                        else if (avgDiff > 20 && avgDiff < 40)
                            copyCount = 31;
                        else if (avgDiff > 4 && avgDiff < 14)
                            copyCount = 7;

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
                            if (String.IsNullOrWhiteSpace(parts[1]))
                                continue;
                            double value = double.Parse(parts[1], CultureInfo.InvariantCulture);

                            // Create event
                            for (int m = 0; m < copyCount; ++m)
                            {
                                // Remove if same event exists
                                for (int p = internalData.Events.Count - 1; p >= Math.Max(internalData.Events.Count - 10, 0); --p)
                                    if (internalData.Events[p].Name == name && internalData.Events[p].Date.Date == date.Date)
                                        internalData.Events.RemoveAt(p);

                                internalData.Events.Add(new _Event()
                                {
                                    Name = name,
                                    Date = date,
                                    Value = value
                                });

                                date = date.AddDays(-1);
                            }
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
            string prefix = "6/7 Cleaning...";
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
                if (false)
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
            string prefix = "7/7 Caching...";
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
                    // Create fundamentals data
                    _FundamentalsData fd = new _FundamentalsData()
                    {
                        Value = cache.Value[i]
                    };

                    // Add to data container
                    dataContainer.GetEvent(new DateTime(cache.Date[i])).FundamentalsData.Add(cache.Name[i], fd);

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
