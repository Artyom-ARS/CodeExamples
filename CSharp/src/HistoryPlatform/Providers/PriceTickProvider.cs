using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Adapters;
using Common.Constants;
using Common.Models;
using HistoryPlatform.Builders;

namespace HistoryPlatform.Providers
{
    public class PriceTickProvider : IPriceTickProvider
    {
        private readonly ITradeAdapter _tradeAdapter;
        private readonly IPriceTickBuilder _priceTickBuilder;

        public PriceTickProvider(ITradeAdapter tradeAdapter, IPriceTickBuilder priceTickBuilder)
        {
            _tradeAdapter = tradeAdapter;
            _priceTickBuilder = priceTickBuilder;
        }

        public async Task<List<PriceTick>> CreatePriceTicketList(InstrumentForStore dataSetForInstrument)
        {
            var instrument = (InstrumentForStoreBase)dataSetForInstrument;
            var priceTickList = new List<PriceTick>();
            var timeFrameShiftForPriceEmulation = await _tradeAdapter.TimeFrameShiftForPriceEmulation(instrument.TimeFrame).ConfigureAwait(false);
            foreach (var priceBar in dataSetForInstrument.PriceBarList)
            {
                var prices = await ConvertOhlcToArray(priceBar).ConfigureAwait(false);
                var previousPrice = prices[0];

                for (var i = 1; i < 4; i++)
                {
                    var price = prices[i];
                    var priceCount = Math.Truncate(Math.Abs((price - previousPrice) / (instrument.PointSize *
                                                             PlatformParameters.PointSizeRegulator)));
                    priceCount = priceCount > PlatformParameters.TimeSeparator ? PlatformParameters.TimeSeparator : priceCount;

                    var step = priceCount == 0
                        ? Math.Abs(price - previousPrice)
                        : Math.Ceiling((Math.Abs(price - previousPrice) / priceCount) / instrument.PointSize) *
                          instrument.PointSize;

                    if (step == 0)
                    {
                        step = 1;
                    }

                    var startShiftSeconds = i * timeFrameShiftForPriceEmulation;

                    if ((price - previousPrice) / (instrument.PointSize * PlatformParameters.PointSizeRegulator) >= 0)
                    {
                        await AddPriceTickAddDirection(priceTickList, instrument, previousPrice, price, step, priceBar, startShiftSeconds).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddPriceTickSubstractDirection(priceTickList, instrument, previousPrice, price, step, priceBar, i).ConfigureAwait(false);
                    }

                    previousPrice = price;
                }
            }

            return priceTickList.OrderBy(x => x.TickServerTime).ToList();
        }

        private async Task AddPriceTickSubstractDirection(
            List<PriceTick> priceTickList,
            InstrumentForStoreBase instrument,
            decimal previousPrice,
            decimal price,
            decimal step,
            PriceBarForStore priceBar,
            int startShiftSeconds)
        {
            var j = 0;
            for (var currentPrice = previousPrice; currentPrice >= price; j++, currentPrice = decimal.Subtract(currentPrice, step))
            {
                var tick = await CreatePriceTick(instrument, j, priceBar, startShiftSeconds, currentPrice).ConfigureAwait(false);
                if (tick == null)
                {
                    break;
                }

                priceTickList.Add(tick.Value);
            }
        }

        private async Task AddPriceTickAddDirection(
            List<PriceTick> priceTickList,
            InstrumentForStoreBase instrument,
            decimal previousPrice,
            decimal price,
            decimal step,
            PriceBarForStore priceBar,
            int startShiftSeconds)
        {
            var j = 0;
            for (var currentPrice = previousPrice; currentPrice <= price; j++, currentPrice = decimal.Add(currentPrice, step))
            {
                var tick = await CreatePriceTick(instrument, j, priceBar, startShiftSeconds, currentPrice).ConfigureAwait(false);
                if (tick == null)
                {
                    break;
                }

                priceTickList.Add(tick.Value);
            }
        }

        private async Task<PriceTick?> CreatePriceTick(
            InstrumentForStoreBase instrument,
            int currentTickIndex,
            PriceBarForStore priceBar,
            int startShiftSeconds,
            decimal price)
        {
            if (currentTickIndex > PlatformParameters.TimeSeparator)
            {
                return null;
            }

            var timeShiftSeconds = startShiftSeconds + currentTickIndex;
            var tick = await _priceTickBuilder.BuildPriceTick(
                priceBar,
                timeShiftSeconds,
                instrument,
                price).ConfigureAwait(false);
            return tick;
        }

        private async Task<List<decimal>> ConvertOhlcToArray(PriceBarForStore value)
        {
            var random = new Random();
            var rndValue = random.Next(1, 3);
            var prices = new List<decimal> { value.BidOpen };
            switch (rndValue)
            {
                case 1:
                    prices.Add(value.BidHigh);
                    prices.Add(value.BidLow);
                    break;
                case 2:
                    prices.Add(value.BidLow);
                    prices.Add(value.BidHigh);
                    break;
            }

            prices.Add(value.BidClose);
            return prices;
        }
    }
}
