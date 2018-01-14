using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter.Data
{
    public class _WeatherData
    {
        public double Precipitation { get; set; }
        public double Snow { get; set; }
        public double TemperatureMax { get; set; }
        public double TemperatureMin { get; set; }

        public _WeatherData()
        {
            Precipitation = double.NaN;
            Snow = double.NaN;
            TemperatureMax = double.NaN;
            TemperatureMin = double.NaN;
        }
    }
}
