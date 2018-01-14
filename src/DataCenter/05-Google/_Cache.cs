using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._05_Google
{
    [Serializable]
    internal class _Cache
    {
        public List<string> Name { get; set; }
        public List<long> Date { get; set; }
        public List<double> Value { get; set; }

        public _Cache(_InternalData internalData)
        {
            Name = new List<string>();
            Date = new List<long>();
            Value = new List<double>();

            for (int i = 0; i < internalData.Events.Count; ++i)
            {
                Name.Add(internalData.Events[i].Name);
                Date.Add(internalData.Events[i].Date.Ticks);
                Value.Add(internalData.Events[i].Value);
            }
        }
    }
}
