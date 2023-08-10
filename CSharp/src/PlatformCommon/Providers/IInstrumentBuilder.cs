using System;
using System.Threading.Tasks;
using Common.Models;

namespace PlatformCommon.Providers
{
    public interface IDataStorageProvider
    {
        Task<InstrumentForStore> BuildInstrumentFromFile(DateTime dtTo, string path,
            string instrument, string timeFrameName, DateTime dtFrom);

        Task<InstrumentForStore> BuildInstrumentFromStorage(DateTime dtTo, string path,
            string instrument, string timeFrameName, DateTime dtFrom);
    }
}
