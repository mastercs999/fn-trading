using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._03_Weather
{
    internal class _Event
    {
        public DateTime Date { get; set; }
        public double Precipitation { get; set; }
        public double Snow { get; set; }
        public double TemperatureMax { get; set; }
        public double TemperatureMin { get; set; }

        public _Event()
        {
            Precipitation = double.NaN;
            Snow = double.NaN;
            TemperatureMax = double.NaN;
            TemperatureMin = double.NaN;
        }
    }
}
