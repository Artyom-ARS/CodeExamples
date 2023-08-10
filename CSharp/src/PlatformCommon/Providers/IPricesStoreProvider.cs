using System.Threading.Tasks;
using Common.Models;

namespace PlatformCommon.Providers
{
    public interface IPricesStoreProvider
    {
        Task<InstrumentForStore> LoadPricesFromFile(string filePath);

        Task<InstrumentForStore> LoadPricesFromStorage(string filePath, string filePathDisk);
    }
}
