using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvnetNullSearchBot
{
    public class RegionSelector
    {
        public string StoreId { get; set; }
        public string Display { get; set; }
        public RegionSelector(string storeId, string display)
        {
            StoreId = storeId;
            Display = display;
        }
    }
}
