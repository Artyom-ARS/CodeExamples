using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Models;

namespace Common.Adapters
{
    public interface IPriceFileFormatAdapter
    {
        Task<List<PriceBarForStore>> ReadPriceBars(BinaryReader reader);

        Task<InstrumentForStore> ReadInstrument(BinaryReader reader);

        Task<int> Write(InstrumentForStoreBase data, BinaryWriter bw);

        Task<int> Write(PriceBarForStore data, BinaryWriter bw);
    }
}
