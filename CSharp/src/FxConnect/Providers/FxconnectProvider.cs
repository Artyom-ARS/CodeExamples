using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;
using Common.Models;
using FxConnect.Adapters;
using FxConnect.Models;
using fxcore2;

namespace FxConnect.Providers
{
    public class FxconnectProvider : IFxconnectProvider
    {
        public event EventHandler<DTOPriceUpdate> PriceUpdate;

        public event EventHandler<DTOOrderUpdate> OrderUpdate;

        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly ITableManagerProvider _tableManagerProvider;
        private readonly ITableManagerAdapter _tableManagerAdapter;
        private readonly IRequestProvider _requestProvider;
        private readonly IMarketDataAdapter _marketDataAdapter;
        private readonly IMarketUpdateProvider _marketUpdateProvider;
        private O2GSession _session;
        private O2GTableManager _tableManager;

        public FxconnectProvider(
            IAuthenticationProvider authenticationProvider,
            ITableManagerProvider tableManagerProvider,
            ITableManagerAdapter tableManagerAdapter,
            IRequestProvider requestProvider,
            IMarketDataAdapter marketDataAdapter,
            IMarketUpdateProvider marketUpdateProvider)
        {
            _authenticationProvider = authenticationProvider;
            _tableManagerProvider = tableManagerProvider;
            _tableManagerAdapter = tableManagerAdapter;
            _requestProvider = requestProvider;
            _marketDataAdapter = marketDataAdapter;
            _marketUpdateProvider = marketUpdateProvider;
        }

        public async Task<DTOAccount> Login()
        {
            _session = await _authenticationProvider.GetSession().ConfigureAwait(false);
            return await LoginWithTableManager().ConfigureAwait(false);
        }

        public async Task Logout()
        {
            await _authenticationProvider.Logout(_session).ConfigureAwait(false);
            _tableManager = null;
            _session = null;
        }

        public async Task<IList<ActiveOrder>> GetActiveOrders(string accountId)
        {
            IList<O2GTradeTableRow> activeOrders = await _tableManagerProvider.GetActiveOrders(_tableManager).ConfigureAwait(false);
            IList<ActiveOrder> dtoActiveOrders = await _tableManagerAdapter.GetActiveOrders(activeOrders).ConfigureAwait(false);
            var account = await _tableManagerProvider.GetAccount(_tableManager, accountId).ConfigureAwait(false);
            foreach (var order in dtoActiveOrders)
            {
                var baseUnitSize = await _marketDataAdapter.GetBaseUnit(_session, account, order.Instrument).ConfigureAwait(false);
                order.Lots /= baseUnitSize;
            }

            return dtoActiveOrders;
        }

        public async Task<bool> CloseOrder(string orderId, OrderBuySell buySell, string accountId, string instrument, int lots, string expertId)
        {
            var offer = await _tableManagerProvider.GetOffer(_tableManager, instrument).ConfigureAwait(false);
            var account = await _tableManagerProvider.GetAccount(_tableManager, accountId).ConfigureAwait(false);
            var baseUnitSize = await _marketDataAdapter.GetBaseUnit(_session, account, instrument).ConfigureAwait(false);
            var orderClosed = await _requestProvider.CloseOrder(_session, accountId, offer, orderId, lots * baseUnitSize, buySell, expertId).ConfigureAwait(false);
            return orderClosed;
        }

        public async Task<ActiveOrder> OpenOrder(string accountId, string instrument, int lots, OrderBuySell buySell, string token)
        {
            var offer = await _tableManagerProvider.GetOffer(_tableManager, instrument).ConfigureAwait(false);
            var account = await _tableManagerProvider.GetAccount(_tableManager, accountId).ConfigureAwait(false);
            var baseUnitSize = await _marketDataAdapter.GetBaseUnit(_session, account, instrument).ConfigureAwait(false);
            var order = new ActiveOrder()
            {
                Lots = lots,
                BuySell = buySell,
                Instrument = instrument,
                OpenPrice = (decimal)offer.Ask,
                Tag = token,
            };

            var orderId = await _requestProvider.OpenOrder(_session, accountId, offer, order.Lots * baseUnitSize, order.BuySell, token).ConfigureAwait(false);

            if (orderId == null)
            {
                return null;
            }

            order.OrderId = orderId;

            return order;
        }

        public async Task<bool> UpdateOrder(string orderId, OrderBuySell buySell, string accountId, string instrument, int lots, decimal stopLoss, decimal takeProfit, string expertId)
        {
            var offer = await _tableManagerProvider.GetOffer(_tableManager, instrument).ConfigureAwait(false);
            var account = await _tableManagerProvider.GetAccount(_tableManager, accountId).ConfigureAwait(false);
            var baseUnitSize = await _marketDataAdapter.GetBaseUnit(_session, account, instrument).ConfigureAwait(false);
            if (stopLoss > 0.0m)
            {
                var orderClosedStop = await _requestProvider.StopCloseOrder(_session, accountId, offer, orderId, lots * baseUnitSize, buySell, stopLoss, expertId).ConfigureAwait(false);
                return orderClosedStop;
            }

            return false;
        }

        public async Task StartWatchingPriceUpdates()
        {
            _marketUpdateProvider.PriceUpdate += HandlePriceUpdates;
            _marketUpdateProvider.OrderUpdate += HandleOrderUpdates;
            await _marketUpdateProvider.StartWatchingPriceUpdates(_session).ConfigureAwait(false);
        }

        public async Task Subscribe(List<string> instruments)
        {
            var offers = await _tableManagerProvider.GetSubscribedOffers(_tableManager).ConfigureAwait(false);
            var needToSubscribeInstruments = new List<string>(instruments);
            var needToUnSubscribeOffers = new List<string>();
            foreach (var offer in offers)
            {
                if (instruments.Contains(offer.Instrument))
                {
                    needToSubscribeInstruments.Remove(offer.Instrument);
                }
                else
                {
                    needToUnSubscribeOffers.Add(offer.OfferID);
                }
            }

            var needToSubscribeOffers = new List<string>();
            foreach (var instrument in needToSubscribeInstruments)
            {
                var offer = await _tableManagerProvider.GetOffer(_tableManager, instrument).ConfigureAwait(false);
                needToSubscribeOffers.Add(offer.OfferID);
            }

            await _requestProvider.Subscribe(_session, needToUnSubscribeOffers, needToSubscribeOffers).ConfigureAwait(false);
        }

        public async Task<IList<PriceBarForStore>> RequestHistoryPrices(string instrument, string tf, int depth)
        {
            var prices = await _requestProvider.GetHistoryPrices(_session, instrument, tf, depth).ConfigureAwait(false);
            var priceBars = await _marketDataAdapter.GetPriceBarList(prices).ConfigureAwait(false);
            if (priceBars.Count == 0)
            {
                return priceBars;
            }

            var firstBar = priceBars.First();
            firstBar.BidClose = firstBar.BidOpen;
            firstBar.BidLow = firstBar.BidOpen;
            firstBar.BidHigh = firstBar.BidOpen;
            firstBar.Volume = 0;
            return priceBars;
        }

        public async Task<(InstrumentForStoreBase, IList<PriceBarForStore>)> RequestHistoryPrices(
            string instrument, string timeFrameName, DateTime dtFrom, DateTime dtTo)
        {
            var offer = await _tableManagerProvider.GetOffer(_tableManager, instrument).ConfigureAwait(false);
            var instrumentForStore = await _tableManagerAdapter.GetInstrument(offer).ConfigureAwait(false);
            var prices = await _requestProvider.GetHistoryPrices(_session, instrument, timeFrameName, dtFrom, dtTo).ConfigureAwait(false);
            var pricesForStore = await _marketDataAdapter.GetPriceBarList(prices).ConfigureAwait(false);
            return (instrumentForStore, pricesForStore);
        }

        public async Task<InstrumentForStoreBase> GetInstrument(string instrument)
        {
            var offer = await _tableManagerProvider.GetOffer(_tableManager, instrument).ConfigureAwait(false);
            if (offer == null)
            {
                return null;
            }

            var instrumentForStore = await _tableManagerAdapter.GetInstrument(offer).ConfigureAwait(false);
            return instrumentForStore;
        }

        private async void HandleOrderUpdates(object sender, O2GOrderRow e)
        {
            var result = await _marketDataAdapter.GetOrderUpdate(e).ConfigureAwait(false);
            var offer = await _tableManagerProvider.GetOfferById(_tableManager, e.OfferID).ConfigureAwait(false);
            result.Instrument = offer.Instrument;
            var account = await _tableManagerProvider.GetAccount(_tableManager, e.AccountID).ConfigureAwait(false);
            var baseUnitSize = await _marketDataAdapter.GetBaseUnit(_session, account, result.Instrument).ConfigureAwait(false);
            result.Amount = result.Amount / baseUnitSize;
            OrderUpdate(this, result);
        }

        private async void HandlePriceUpdates(object sender, DTOPriceUpdate e)
        {
            PriceUpdate(this, e);
        }

        private async Task<DTOAccount> LoginWithTableManager()
        {
            if (!await _authenticationProvider.TryLoginWithTableManager(_session).ConfigureAwait(false))
            {
                return null;
            }

            var tableManager = await _tableManagerProvider.GetTableManager(_session).ConfigureAwait(false);
            if (tableManager == null)
            {
                return null;
            }

            _tableManager = tableManager;

            var account = await _tableManagerProvider.GetAccount(_tableManager).ConfigureAwait(false);
            if (account == null)
            {
                return null;
            }

            var dtoAccount = await _tableManagerAdapter.GetAccount(account).ConfigureAwait(false);

            return dtoAccount;
        }
    }
}
