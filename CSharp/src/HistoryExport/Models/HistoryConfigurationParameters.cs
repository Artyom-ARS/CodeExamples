using System.Collections.Generic;
using Common.Models;

namespace HistoryExport.Models
{
    public class HistoryConfigurationParameters
    {
        public string Path;

        public bool LoadFromStorage;

        public List<PriceConfiguration> PriceConfiguration;
    }
}
