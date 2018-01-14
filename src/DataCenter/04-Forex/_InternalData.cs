using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._04_Forex
{
    [Serializable]
    internal class _InternalData
    {
        public List<string> Pairs { get; set; }
        public List<_Event> Events { get; set; }

        public _InternalData()
        {
            Pairs = new List<string>();
            Events = new List<_Event>();
        }
    }
}
