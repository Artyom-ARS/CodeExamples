using System;
using System.Threading.Tasks;
using Common.Models;

namespace HistoryPlatform.Builders
{
    public class PriceTickBuilder : IPriceTickBuilder
    {
        public async Task<PriceTick> BuildPriceTick(
            PriceBarForStore priceBar,
            int timeShiftSeconds,
            InstrumentForStoreBase instrument,
            decimal price)
        {
            var priceTick = new PriceTick
            {
                TickServerTime = priceBar.Time.AddSeconds(timeShiftSeconds),
                Bid = Math.Round(price, instrument.Digits + 1),
                Ask = Math.Round(price + priceBar.Spread, instrument.Digits + 1),
                Instrument = instrument.Instrument,
                PointSize = instrument.PointSize,
            };
            return priceTick;
        }
    }
}
