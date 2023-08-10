using System.Collections.Generic;
using Common.Models;

namespace HistoryStorage.Models
{
    public class HistoryConfigurationParameters
    {
        public bool SaveToStorage;

        public bool ForceReplace;

        public List<PriceConfiguration> PriceConfiguration;
    }
}
