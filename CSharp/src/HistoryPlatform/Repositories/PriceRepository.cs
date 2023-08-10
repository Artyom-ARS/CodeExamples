using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Models;
using HistoryPlatform.Models;

namespace HistoryPlatform.Repositories
{
    public class PriceRepository : IPriceRepository
    {
        private Warehouse Warehouse { get; set; }

        public PriceRepository()
        {
            Warehouse = new Warehouse();
        }

        public async Task<InstrumentForStore> Get(string instrument, TimeFrame timeFrame, DateTime dateFrom, DateTime dateTo)
        {
            var dataFromInstance = await Get(instrument, timeFrame).ConfigureAwait(false);
            if (dataFromInstance.PriceBarList.Count == 0)
            {
                return dataFromInstance;
            }

            var resultFilter = dataFromInstance.PriceBarList.Where(x => x.Time.Date >= dateFrom && x.Time.Date <= dateTo).OrderBy(x => x.Time.Date).ToList();
            dataFromInstance.PriceBarList = resultFilter;

            return dataFromInstance;
        }

        public async Task Add(InstrumentForStore value)
        {
            Warehouse.Add(await GetKey(value.Instrument, value.TimeFrame.ToLowerInvariant()).ConfigureAwait(false), value);
        }

        public async Task<InstrumentForStore> Get(string instrument, TimeFrame timeFrame, DateTime dateTo, int depth)
        {
            var dataFromInstance = await Get(instrument, timeFrame).ConfigureAwait(false);
            if (dataFromInstance.PriceBarList.Count == 0)
            {
                return dataFromInstance;
            }

            var resultFilter = dataFromInstance.PriceBarList.Where(x => x.Time.Date < dateTo).OrderBy(x => x.Time.Date).Take(depth + 1).ToList();
            dataFromInstance.PriceBarList = resultFilter;

            return dataFromInstance;
        }

        public async Task<InstrumentForStore> GetFlatInstrument(string instrument, TimeFrame timeFrameName)
        {
            var keyForDictionary = await GetKey(instrument, timeFrameName.ToString()).ConfigureAwait(false);
            if (Warehouse.ContainsKey(keyForDictionary))
            {
                return Warehouse[keyForDictionary];
            }

            return null;
        }

        private async Task<string> GetKey(string instrument, string timeFrame)
        {
            return instrument + "_" + timeFrame;
        }

        private async Task<InstrumentForStore> Get(string instrument, TimeFrame timeFrame)
        {
            var dataSetFromDictionary = await GetFlatInstrument(instrument, timeFrame).ConfigureAwait(false);

            if (dataSetFromDictionary == null)
            {
                return new InstrumentForStore
                {
                    PriceBarList = new List<PriceBarForStore>(),
                };
            }

            return dataSetFromDictionary;
        }
    }
}
