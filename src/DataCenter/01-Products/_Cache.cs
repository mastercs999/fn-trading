using DataCenter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter._01_Products
{
    [Serializable]
    internal class _Cache
    {
        public List<string> Symbols { get; set; }
        public List<string> Names { get; set; }

        public _Cache(_InternalData internalData)
        {
            Symbols = new List<string>();
            Names = new List<string>();

            foreach (_Product p in internalData.Products)
            {
                Symbols.Add(p.Symbol);
                Names.Add(p.Name);
            }
        }
    }
}
