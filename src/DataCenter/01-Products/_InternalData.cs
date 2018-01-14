using DataCenter.Data;
using DataCenter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._01_Products
{
    internal class _InternalData
    {
        public List<_Product> Products { get; set; }

        public _InternalData()
        {
            Products = new List<_Product>();
        }
    }
}
