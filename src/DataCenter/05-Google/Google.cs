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

namespace DataCenter._05_Google
{
    internal class Google : DataSource
    {
        public Google() : base() { }
        public Google(bool reload) : base(reload) { }

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

            // Search
            await DownloadSearchs(dataContainer.Products);

            // Parse
            Parse(internalData);

            // SeDeSerialize
            Cache(dataContainer, internalData);

            // End info
            Console.WriteLine();
        }

        private async Task DownloadSearchs(List<Product> products)
        {
            string prefix = "1/3 Searching...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(products.Count + DataManager.Config.ImportantWords.Length);

            // Download
            try
            {
                for (int i = 0; i < products.Count + DataManager.Config.ImportantWords.Length; ++i)
                {
                    // Downloading info
                    string query, targetFile;
                    if (i < products.Count)
                    {
                        Product p = products[i];

                        query = WebUtility.UrlEncode(HttpUtility.HtmlDecode(p.Name.Remove(",", "(The)", "Inc.", "Corporation", "Company", "Corp.")));
                        targetFile = Path.Combine(Folder, "Symbol-" + p.Symbol + ".json");
                    }
                    else
                    {
                        string word = DataManager.Config.ImportantWords[i - products.Count];

                        query = WebUtility.UrlEncode(word);
                        targetFile = Path.Combine(Folder, "Word-" + word + ".json");
                    }

                    // Download target range
                    string url = "http://www.google.com/trends/fetchComponent?q=" + query + "&cid=TIMESERIES_GRAPH_0&export=3";
                    if (Reload || !File.Exists(targetFile))
                        await Downloader.DownloadFileAsync(url, targetFile, null, DataManager.Config.GoogleCookies);

                    // Check if downloaded properly
                    string content = File.ReadAllText(targetFile);
                    if (content.Contains("http://www.google.com/trends/errorPage"))
                    {
                        File.Delete(targetFile);

                        Thread.Sleep(60 * 1000);
                        --i;
                        continue;
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
            string[] files = Directory.GetFiles(Folder).Where(x => x.Contains(".json")).ToArray();

            // Count when we update progress bar
            int drawEvery = Utils.PercentIntervalByLength(files.Length);

            // Download
            try
            {
                if (Reload || !File.Exists(SerializedFile))
                    for (int i = 0; i < files.Length; ++i)
                    {
                        // Parse only no errors data
                        string content = File.ReadAllText(files[i]);
                        content = content.Substring(content.IndexOf('{'));
                        content = content.Substring(0, content.LastIndexOf('}') + 1);
                        JObject json = JObject.Parse(content);
                        if (((string)json["status"]) == "ok")
                        {
                            JArray rows = (JArray)json["table"]["rows"];
                            foreach (JObject row in rows)
                            {
                                // Parse date
                                string dateStr = ((JConstructor)row["c"][0]["v"]).ToString();
                                string[] parts = dateStr.Split(new char[] { '\n' }).Select(x => x.Trim().Replace(",", "")).ToArray();
                                int year = int.Parse(parts[1]);
                                int month = int.Parse(parts[2]);
                                if (month == 0)
                                {
                                    month = 12;
                                    --year;
                                }
                                int day = int.Parse(parts[3]);
                                DateTime date = new DateTime(year, month, day).AddMonths(1);
                                month = date.Month;

                                // Parse value
                                int value = int.Parse((string)row["c"][1]["f"]);

                                // Save event to every day of month
                                while (date.Month == month)
                                {
                                    internalData.Events.Add(new _Event()
                                    {
                                        Name = Path.GetFileNameWithoutExtension(files[i]).Split(new char[] { '-' })[1],
                                        Date = date,
                                        Value = value
                                    });

                                    date = date.AddDays(1);
                                }
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
                    _GoogleData gd = new _GoogleData()
                    {
                        Value = cache.Value[i]
                    };

                    // Add to data container
                    dataContainer.GetEvent(new DateTime(cache.Date[i])).GoogleDatas.Add(cache.Name[i], gd);

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
