using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._04_Forex
{
    [Serializable]
    internal class _Event
    {
        public string Pair { get; set; }
        public DateTime Date { get; set; }
        public double Price { get; set; }
    }
}
