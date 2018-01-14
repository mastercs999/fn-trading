using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCenter.Data
{
    public class DataContainer
    {
        // Products
        public List<Product> Products { get; set; }

        // Data
        public SortedDictionary<DateTime, Event> Events { get; set; }

        public DataContainer()
        {
            Products = new List<Product>();
            Events = new SortedDictionary<DateTime, Event>();
        }

        public Event GetEvent(DateTime date)
        {
            // Get event for current date or insert new one
            Event e = null;
            if (!Events.TryGetValue(date, out e))
            {
                // Create new event
                e = new Event(Products);

                // Add event to dicionary
                Events.Add(date, e);
            }

            return e;
        }

        private DateTime? MinimalDate = null;
        public DateTime GetMinimalDate()
        {
            if (MinimalDate != null)
                return (DateTime)MinimalDate;

            DateTime minimal = Events.Keys.ElementAt(0);
            foreach (DateTime date in Events.Keys)
            {
                // Has event some price?
                foreach (_ProductData pd in Events[date].ProductsDatas.Values)
                    if (!double.IsNaN(pd.Price))
                    {
                        MinimalDate = date;
                        return date;
                    }
            }

            return new DateTime(0);
        }
    }
}
