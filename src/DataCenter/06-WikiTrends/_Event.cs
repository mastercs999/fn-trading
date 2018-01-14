using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._06_WikiTrends
{
    [Serializable]
    internal class _Event
    {
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }
}
