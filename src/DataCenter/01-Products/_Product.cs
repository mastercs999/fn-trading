using DataCenter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._01_Products
{
    [Serializable]
    internal class _Product
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public double MarketCapitalizationInMillions { get; set; }
        public DateTime StartDate { get; set; }
    }
}
