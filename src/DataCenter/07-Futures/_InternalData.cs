using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._07_Futures
{
    [Serializable]
    internal class _InternalData
    {
        public List<_Event> Events { get; set; }

        public Dictionary<string, string> NameToInfoUrl { get; set; }
        public Dictionary<string, List<string>> NameToDataUrl { get; set; }

        public _InternalData()
        {
            Events = new List<_Event>();

            NameToInfoUrl = new Dictionary<string, string>();
            NameToDataUrl = new Dictionary<string, List<string>>();
        }
    }
}
