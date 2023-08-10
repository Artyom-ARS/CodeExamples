using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Constants;
using FxConnect.Listeners;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public class MarketUpdateProvider : IMarketUpdateProvider
    {
        public event EventHandler<DTOPriceUpdate> PriceUpdate;

        public event EventHandler<O2GOrderRow> OrderUpdate;

        private readonly MarketUpdateListener _marketUpdateListener;

        private O2GSession _session;

        public MarketUpdateProvider()
        {
            _marketUpdateListener = new MarketUpdateListener();
        }

        public async Task StartWatchingPriceUpdates(O2GSession session)
        {
            _session = session;

            _marketUpdateListener.UpdatePrice += OnMarketUpdate;
            _session.TablesUpdates += _marketUpdateListener.OnUpdate;
        }

        private async void OnMarketUpdate(object sender, TablesUpdatesEventArgs e)
        {
            var response = e.Response;
            var (priceRows, orderRows) = HandlePriceUpdate(response);
            foreach (var priceRow in priceRows)
            {
                PriceUpdate(this, priceRow);
            }

            foreach (var orderRow in orderRows)
            {
                if (orderRow.Status == PlatformParameters.OrderFinalStatus
                    || ((orderRow.Type == Constants.Orders.Limit || orderRow.Type == Constants.Orders.Stop) && (orderRow.Status == PlatformParameters.OrderUpdateStatus || orderRow.Status == PlatformParameters.OrderCloseStatus)))
                {
                    OrderUpdate(this, orderRow);
                }
            }
        }

        private (List<DTOPriceUpdate>, List<O2GOrderRow>) HandlePriceUpdate(O2GResponse response)
        {
            var priceRows = new List<DTOPriceUpdate>();
            var orderRows = new List<O2GOrderRow>();
            var responseFactory = _session.getResponseReaderFactory();

            if (responseFactory == null)
            {
                return (priceRows, orderRows);
            }

            var tablesUpdatesReader = responseFactory.createTablesUpdatesReader(response);
            if (tablesUpdatesReader.Count == 0)
            {
                return (priceRows, orderRows);
            }

            for (var i = 0; i < tablesUpdatesReader.Count; i++)
            {
                var type = tablesUpdatesReader.getUpdateType(i);
                var table = tablesUpdatesReader.getUpdateTable(i);

                // todo: move GetPriceUpdate to MarketDataAdapter
                var priceRow = GetPriceUpdate(type, table, tablesUpdatesReader, i);

                if (priceRow != null)
                {
                    priceRows.Add(priceRow);
                }

                var orderRow = GetOrderUpdate(table, tablesUpdatesReader, i);

                if (orderRow == null)
                {
                    continue;
                }

                orderRows.Add(orderRow);
            }

            return (priceRows, orderRows);
        }

        private DTOPriceUpdate GetPriceUpdate(
            O2GTableUpdateType type,
            O2GTableType table,
            O2GTablesUpdatesReader tablesUpdatesReader,
            int i)
        {
            if (type != O2GTableUpdateType.Update || table != O2GTableType.Offers)
            {
                return null;
            }

            var price = tablesUpdatesReader.getOfferRow(i);
            if (string.IsNullOrEmpty(price.Instrument))
            {
                return null;
            }

            var priceRow = new DTOPriceUpdate
            {
                Instrument = price.Instrument,
                Ask = price.Ask,
                Bid = price.Bid,
                ServerTime = tablesUpdatesReader.ServerTime,
            };
            return priceRow;
        }

        private O2GOrderRow GetOrderUpdate(
            O2GTableType table,
            O2GTablesUpdatesReader tablesUpdatesReader,
            int i)
        {
            if (table != O2GTableType.Orders)
            {
                return null;
            }

            var order = tablesUpdatesReader.getOrderRow(i);

            return order;
        }
    }
}
