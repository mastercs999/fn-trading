using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._03_Weather
{
    internal class _LonLat
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }

        public _LonLat(double lon, double lat)
        {
            Longitude = lon;
            Latitude = lat;
        }
    }
}
