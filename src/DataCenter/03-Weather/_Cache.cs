using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._03_Weather
{
    [Serializable]
    internal class _Cache
    {
        public List<string> Station { get; set; }
        public List<long> Date { get; set; }
        public List<double> Precipitation { get; set; }
        public List<double> Snow { get; set; }
        public List<double> TemperatureMax { get; set; }
        public List<double> TemperatureMin { get; set; }

        public _Cache(_InternalData internalData)
        {
            Station = new List<string>();
            Date = new List<long>();
            Precipitation = new List<double>();
            Snow = new List<double>();
            TemperatureMax = new List<double>();
            TemperatureMin = new List<double>();

            foreach (KeyValuePair<string, List<_Event>> kv in internalData.Events)
            {
                // Add to list
                foreach (_Event e in kv.Value)
                {
                    Station.Add(kv.Key);
                    Date.Add(e.Date.Ticks);
                    Precipitation.Add(e.Precipitation);
                    Snow.Add(e.Snow);
                    TemperatureMax.Add(e.TemperatureMax);
                    TemperatureMin.Add(e.TemperatureMin);
                }

                // Delete list
                kv.Value.Clear();
            }
        }
    }
}
