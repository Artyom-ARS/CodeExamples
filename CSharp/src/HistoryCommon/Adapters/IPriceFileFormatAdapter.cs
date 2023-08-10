using System.Collections.Generic;
using System.IO;
using HistoryCommon.Models;

namespace HistoryCommon.Adapters
{
    public interface IPriceFileFormatAdapter
    {
        List<PriceBarForStore> ReadPriceBars(BinaryReader reader);

        InstrumentForStore ReadInstrument(BinaryReader reader);

        int Write(InstrumentForStoreMarshalable data, BinaryWriter bw);

        int Write(PriceBarForStore data, BinaryWriter bw);

    }
}
