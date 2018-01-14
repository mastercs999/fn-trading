using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._03_Weather
{
    internal class _InternalData
    {
        public string[] Wanted = new string[] { "PRCP", "SNOW", "TMAX", "TMIN" };
        public Dictionary<string, _LonLat> StationPlaces { get; set; }
        public string[][] WorldMap { get; set; }
        public Dictionary<string, List<_Event>> Events { get; set; }

        public _InternalData()
        {
            StationPlaces = new Dictionary<string, _LonLat>();
            WorldMap = new string[361][];
            for (int i = 0; i < WorldMap.Length; ++i)
                WorldMap[i] = new string[181];
            Events = new Dictionary<string, List<_Event>>();
        }
        public void SaveToMap(string content, _LonLat coords)
        {
            // Get coords by area
            _LonLat byArea = RoundCoordsByArea(coords);

            // Align to zero
            double lon = byArea.Longitude + 180;
            double lat = byArea.Latitude + 90;

            // Round it
            int lonInt = (int)Math.Round(lon);
            int latInt = (int)Math.Round(lat);

            // Save to map
            if (WorldMap[lonInt][latInt] == null || WorldMap[lonInt][latInt].Length < content.Length)
                WorldMap[lonInt][latInt] = content;
        }

        private _LonLat RoundCoordsByArea(_LonLat coords)
        {
            // Default resoltuion
            int roundLonTo = 180;
            int roundLatTo = 90;

            // North America
            //if (coords.Longitude > -120 && coords.Longitude < -60 &&
            //    coords.Latitude > 10 && coords.Latitude < 70)
            //{
            //    roundLonTo = 10;
            //    roundLatTo = 5;
            //}
            //// North east America
            if (coords.Longitude > -90 && coords.Longitude < -60 &&
                coords.Latitude > 30 && coords.Latitude < 55)
            {
                roundLonTo = 5;
                roundLatTo = 3;
            }



            //// Europe
            //if (coords.Longitude > -15 && coords.Longitude < 20 &&
            //    coords.Latitude > 37 && coords.Latitude < 55)
            //{
            //    roundLonTo = 15;
            //    roundLatTo = 9;
            //}
            //// Russia / Asia
            //else if (coords.Longitude > 25 && coords.Longitude < 135 &&
            //    coords.Latitude > 10 && coords.Latitude < 70)
            //{
            //    roundLonTo = 50;
            //    roundLatTo = 30;
            //}
            //// Japan
            //else if (coords.Longitude > 130 && coords.Longitude < 148 &&
            //    coords.Latitude > 30 && coords.Latitude < 40)
            //{
            //    roundLonTo = 9;
            //    roundLatTo = 5;
            //}
            //// Australia
            //else if (coords.Longitude > 110 && coords.Longitude < 150 &&
            //    coords.Latitude > -40 && coords.Latitude < -10)
            //{
            //    roundLonTo = 20;
            //    roundLatTo = 15;
            //}
            //// Africa
            //else if (coords.Longitude > -15 && coords.Longitude < 45 &&
            //    coords.Latitude > -30 && coords.Latitude < 30)
            //{
            //    roundLonTo = 30;
            //    roundLatTo = 30;
            //}
            //// North west America
            //else if (coords.Longitude > -90 && coords.Longitude < -60 &&
            //    coords.Latitude > 30 && coords.Latitude < 55)
            //{
            //    roundLonTo = 15;
            //    roundLatTo = 15;
            //}
            //// North east America
            //else if (coords.Longitude > -120 && coords.Longitude < -90 &&
            //    coords.Latitude > 10 && coords.Latitude < 70)
            //{
            //    roundLonTo = 15;
            //    roundLatTo = 30;
            //}
            //// South America
            //else if (coords.Longitude > -75 && coords.Longitude < -45 &&
            //    coords.Latitude > -50 && coords.Latitude < 10)
            //{
            //    roundLonTo = 15;
            //    roundLatTo = 20;
            //}
            //// Antartida
            //else if (coords.Longitude > -180 && coords.Longitude < 180 &&
            //    coords.Latitude > -90 && coords.Latitude < -70)
            //{
            //    roundLonTo = 180;
            //    roundLatTo = 20;
            //}

            return new _LonLat(LocalUtils.Round(coords.Longitude, roundLonTo), LocalUtils.Round(coords.Latitude, roundLatTo));
        }
    }
}
