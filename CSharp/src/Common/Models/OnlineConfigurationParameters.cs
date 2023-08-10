using System.Collections.Generic;

namespace Common.Models
{
    public class OnlineConfigurationParameters
    {
        public List<string> Instruments;
        public int PrintStatusSuspenseSeconds;
        public int PlatformStartHandleUpdatesDelaySeconds;
        public Dictionary<string, PriceUpdateSuspenseParameters> PriceUpdateSuspense;
    }
}
