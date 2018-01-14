using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._05_Google
{
    [Serializable]
    internal class _InternalData
    {
        public List<_Event> Events { get; set; }

        public _InternalData()
        {
            Events = new List<_Event>();
        }
    }
}
