using System.Collections.Generic;

namespace HistoryCommon.Models
{
    public class InstrumentForStore : InstrumentForStoreMarshalable
    {
        public List<PriceBarForStore> PriceBarList;
    }
}
