using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._04_Forex
{
    [Serializable]
    internal class _Cache
    {
        public List<string> Pair { get; set; }
        public List<long> Date { get; set; }
        public List<double> Price { get; set; }

        public _Cache(_InternalData internalData)
        {
            Pair = new List<string>();
            Date = new List<long>();
            Price = new List<double>();

            for (int i = 0; i < internalData.Events.Count; ++i)
            {
                Pair.Add(internalData.Events[i].Pair);
                Date.Add(internalData.Events[i].Date.Ticks);
                Price.Add(internalData.Events[i].Price);
            }
        }
    }
}
