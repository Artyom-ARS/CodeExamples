using System.Collections.Generic;
using Common.Models;

namespace HistoryPlatform.Models
{
    public class HistoryConfigurationParameters
    {
        public string Path;

        public string PathClosedOrders;

        public bool LoadFromStorage;

        public List<PriceConfiguration> PriceConfiguration;
    }
}
