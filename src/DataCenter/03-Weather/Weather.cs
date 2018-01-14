using DataCenter.Data;
using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using Types;

namespace DataCenter._03_Weather
{
    internal class Weather : DataSource
    {
        public Weather() : base() { }
        public Weather(bool reload) : base(reload) { }

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

            // Parse stations
            PlaceStations(internalData);

            // Clean
            Clean(dataContainer, internalData);

            // Parse data
            Parse(internalData);

            // Caching
            Cache(dataContainer, internalData);

            // End info
            Console.WriteLine();
        }

        private async Task Download()
        {
            string prefix = "1/5 Downloading 1/2...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Download
            try
            {
                // Target file
                string targetFile = Path.Combine(Folder, "Stations.csv");

                // Download if needed
                if (Reload || !File.Exists(targetFile))
                    await Downloader.DownloadFileAsync("http://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt", targetFile, progress =>
                    {
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, progress), ConsoleColor.Gray);
                    }, null);

                // Now other small data
                prefix = "1/5 Downloading 2/2...";
                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

                targetFile = Path.Combine(Folder, "Weather.tar.gz");
                if (Reload || !File.Exists(targetFile))
                    await Downloader.DownloadFileAsync("http://www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd_all.tar.gz", targetFile, progress =>
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
        private void PlaceStations(_InternalData internalData)
        {
            string prefix = "2/5 Placing stations...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Parsing
            try
            {
                // Only if needed
                if (Reload || !File.Exists(SerializedFile))
                {
                    // Load stations
                    string[] stations = File.ReadAllLines(Path.Combine(Folder, "Stations.csv"));

                    // Count when we update progress bar
                    int drawEvery = Utils.PercentIntervalByLength(stations.Length);

                    // Place each station
                    for (int i = 0; i < stations.Length; ++i)
                    {
                        // Columns
                        string[] columns = stations[i].Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

                        // Get station name
                        string name = columns[0];

                        // Get longitude and langitude
                        double lon = double.Parse(columns[2], CultureInfo.InvariantCulture);
                        double lat = double.Parse(columns[1], CultureInfo.InvariantCulture);

                        // Add station place
                        internalData.StationPlaces.Add(name, new _LonLat(lon, lat));

                        if (i % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)i / stations.Length * 100.0), ConsoleColor.Gray);
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
        private void Clean(DataContainer dataContainer, _InternalData internalData)
        {
            string prefix = "3/5 Cleaning...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Parsing
            try
            {
                // Only if needed
                if (Reload || !File.Exists(SerializedFile))
                {
                    // Get station count
                    int stations = File.ReadAllLines(Path.Combine(Folder, "Stations.csv")).Length;

                    // Count when we update progress bar
                    int drawEvery = Utils.PercentIntervalByLength(stations);

                    // Get minimal date
                    DateTime minimalDate = dataContainer.GetMinimalDate();
                    int minimalDateInt = int.Parse(minimalDate.ToString("yyyyMM"));

                    // Clean data
                    int currentCount = 0;
                    Compression.DecompressTgzStream(Path.Combine(Folder, "Weather.tar.gz"), (file, content) =>
                    {
                        // Examine only data files
                        if (Path.GetExtension(file) != ".dly")
                            return;

                        // Split to lines
                        List<string> lines = content.Split(new char[] { '\n' }).ToList();

                        // Remove blank lines
                        lines.RemoveAll(x => String.IsNullOrWhiteSpace(x));

                        // Remove lines of data we dont want
                        lines.RemoveAll(x => !internalData.Wanted.Contains(x.Substring(17, 4)));

                        // Remove all too old data
                        lines.RemoveAll(x => int.Parse(x.Substring(11, 6)) < minimalDateInt);

                        // Check if source is long enough (in years)
                        if (lines.Where(x => int.Parse(x.Substring(11, 4)) >= DateTime.Today.Year - 1).Count() == 0)
                            return;

                        // Remove all info if there is not enough of data (many unknown data)
                        foreach (string w in internalData.Wanted)
                        {
                            // Select only lines of current info
                            string[] ofInfo = lines.Where(x => x.Substring(17, 4) == w).ToArray();

                            // Check ratio of known and unknown values
                            int unknownValues = ofInfo.Select(x => Regex.Matches(x, "-9999").Count).Sum();
                            int knownValues = ofInfo.Length * 31 - unknownValues;

                            // If not much, remove this lines
                            if (unknownValues / (double)(knownValues + unknownValues) > 0.1)
                                lines.RemoveAll(x => x.Substring(17, 4) == w);
                        }

                        // No data left
                        if (lines.Count < 10)
                            return;

                        // Get station coords
                        _LonLat coords = internalData.StationPlaces[Path.GetFileNameWithoutExtension(file)];

                        // Place to world map
                        internalData.SaveToMap(String.Join("\n", lines), coords);

                        if (++currentCount % drawEvery == 0)
                            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)currentCount / stations * 100.0), ConsoleColor.Gray);
                    });
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
            string prefix = "4/5 Parsing...";
            Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 0), ConsoleColor.Gray);

            // Parsing
            try
            {
                // Only if needed
                if (Reload || !File.Exists(SerializedFile))
                {
                    // Total worl map points
                    int totalCount = internalData.WorldMap.Length * internalData.WorldMap[0].Length;

                    // Count when we update progress bar
                    int drawEvery = Utils.PercentIntervalByLength(totalCount);

                    for (int i = 0; i < internalData.WorldMap.Length; ++i)
                        for (int j = 0; j < internalData.WorldMap[i].Length; ++j)
                        {
                            // Get file content and null on world map (save memory)
                            string content = internalData.WorldMap[i][j];
                            internalData.WorldMap[i][j] = null;

                            // Place has to have a data
                            if (content != null)
                            {
                                // Split to lines
                                List<string> lines = content.Split(new char[] { '\n' }).ToList();

                                // Get station name
                                string station = lines[0].Substring(0, 11);

                                // Create event list
                                List<_Event> events = new List<_Event>();

                                // Add event list to internal data
                                internalData.Events.Add(station, events);

                                // Remove empty lines
                                lines.RemoveAll(x => String.IsNullOrWhiteSpace(x));

                                // Enumerate lines
                                for (int k = 0; k < lines.Count; ++k)
                                {
                                    // Find all lines with same date
                                    List<string> sameDateLines = new List<string>();
                                    sameDateLines.Add(lines[k]);
                                    for (int z = k + 1; z < lines.Count; ++z)
                                    {
                                        // Add if same date
                                        if (lines[z].Substring(11, 6) == sameDateLines[0].Substring(11, 6))
                                            sameDateLines.Add(lines[z]);
                                        else
                                        {
                                            k = z - 1;
                                            break;
                                        }

                                        // We read all lines
                                        if (z + 1 == lines.Count)
                                        {
                                            k = lines.Count;
                                            break;
                                        }
                                    }

                                    // Get string of info (SNOW, TMAX, ..)
                                    string[] infos = sameDateLines.Select(x => x.Substring(17, 4)).ToArray();

                                    // Get year and month
                                    int year = int.Parse(sameDateLines[0].Substring(11, 4));
                                    int month = int.Parse(sameDateLines[0].Substring(15, 2));

                                    // Process days
                                    int daysInMonth = DateTime.DaysInMonth(year, month);
                                    for (int day = 1; day <= daysInMonth; ++day)
                                    {
                                        // Create new event
                                        _Event e = new _Event();
                                        e.Date = new DateTime(year, month, day);

                                        // Process lines
                                        for (int z = 0; z < sameDateLines.Count; ++z)
                                        {
                                            // Parse value
                                            int value = int.Parse(sameDateLines[z].Substring(21 + (day - 1) * 8, 5));

                                            // Edit value
                                            double valueDouble = value == -9999 ? double.NaN : value;

                                            // Save value
                                            if (infos[z] == "PRCP")
                                                e.Precipitation = valueDouble;
                                            else if (infos[z] == "SNOW")
                                                e.Snow = valueDouble;
                                            else if (infos[z] == "TMAX")
                                                e.TemperatureMax = valueDouble;
                                            else if (infos[z] == "TMIN")
                                                e.TemperatureMin = valueDouble;
                                        }

                                        // Add to event list
                                        events.Add(e);
                                    }
                                }
                            }

                            int count = i * internalData.WorldMap[0].Length + j;
                            if (count % drawEvery == 0)
                                Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, (double)count / totalCount * 100.0), ConsoleColor.Gray);
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

                int drawEvery = Utils.PercentIntervalByLength(cache.Station.Count * 2);

                // Save to datacontainer
                for (int i = 0; i < cache.Station.Count; ++i)
                {
                    // Create weather data
                    _WeatherData wd = new _WeatherData()
                    {
                        Precipitation = cache.Precipitation[i],
                        Snow = cache.Snow[i],
                        TemperatureMax = cache.TemperatureMax[i],
                        TemperatureMin = cache.TemperatureMin[i]
                    };

                    // Station
                    string station = cache.Station[i];

                    // Get event
                    Event e = dataContainer.GetEvent(new DateTime(cache.Date[i]));

                    // Add to event
                    e.WeatherDatas.Add(station, wd);


                    if (i % drawEvery == 0)
                        Utils.DrawMessage(prefix, Utils.CreateProgressBar(Utils.ProgressBarLength, 50 + (double)i / cache.Station.Count * 50), ConsoleColor.Gray);
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
