using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter.Data
{
    public class Event
    {
        // Numbers data
        public Dictionary<string, _ProductData> ProductsDatas { get; set; }
        public Dictionary<string, _WeatherData> WeatherDatas { get; set; }
        public Dictionary<string, _ForexData> ForexDatas { get; set; }
        public Dictionary<string, _GoogleData> GoogleDatas { get; set; }
        public Dictionary<string, _WikiTrendsData> WikiTrendsDatas { get; set; }
        public Dictionary<string, _FuturesData> FuturesDatas { get; set; }
        public Dictionary<string, _FundamentalsData> FundamentalsData { get; set; }

        public Event(List<Product> products)
        {
            ProductsDatas = new Dictionary<string, _ProductData>();
            foreach (Product p in products)
                ProductsDatas.Add(p.Symbol, new _ProductData());

            WeatherDatas = new Dictionary<string, _WeatherData>();

            ForexDatas = new Dictionary<string, _ForexData>();

            GoogleDatas = new Dictionary<string, _GoogleData>();

            WikiTrendsDatas = new Dictionary<string, _WikiTrendsData>();

            FuturesDatas = new Dictionary<string, _FuturesData>();

            FundamentalsData = new Dictionary<string, _FundamentalsData>();
        }
    }
}
