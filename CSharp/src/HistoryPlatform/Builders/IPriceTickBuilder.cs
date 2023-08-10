using System.Threading.Tasks;
using Common.Models;

namespace HistoryPlatform.Builders
{
    public interface IPriceTickBuilder
    {
        Task<PriceTick> BuildPriceTick(PriceBarForStore priceBar, int timeShiftSeconds, InstrumentForStoreBase instrument, decimal price);
    }
}
