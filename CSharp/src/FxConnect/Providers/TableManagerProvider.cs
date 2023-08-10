using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Facades;
using Common.Logging;
using FxConnect.Facades;
using fxcore2;

namespace FxConnect.Providers
{
    public class TableManagerProvider : ITableManagerProvider
    {
        private readonly IConsole _console;
        private readonly ISessionFacade _sessionFacade;
        private readonly ITableManagerFacade _tableManagerFacade;

        public TableManagerProvider(IConsole console, ISessionFacade sessionFacade, ITableManagerFacade tableManagerFacade)
        {
            _console = console;
            _sessionFacade = sessionFacade;
            _tableManagerFacade = tableManagerFacade;
        }

        public async Task<O2GTableManager> GetTableManager(O2GSession session)
        {
            try
            {
                var tableManager = await _sessionFacade.GetTableManager(session).ConfigureAwait(false);
                var managerStatus = _tableManagerFacade.GetStatus(tableManager);

                while (managerStatus == O2GTableManagerStatus.TablesLoading)
                {
                    Thread.Sleep(50);
                    managerStatus = _tableManagerFacade.GetStatus(tableManager);
                }

                if (managerStatus == O2GTableManagerStatus.TablesLoadFailed)
                {
                    throw new Exception("Cannot refresh all tables of table manager");
                }

                return tableManager;
            }
            catch (Exception e)
            {
                _console.WriteLine("Exception: {0}", e);
                Logger.Log.Error($"Failed to get table manager. Exception: {e}");
            }

            return null;
        }

        public async Task<O2GAccountRow> GetAccount(O2GTableManager tableManager)
        {
            var accountsTable = _tableManagerFacade.GetTable<O2GAccountsTable>(tableManager, O2GTableType.Accounts);
            if (accountsTable.Count == 0)
            {
                return null;
            }

            var accountRow = accountsTable.getRow(0);
            return accountRow;
        }

        public async Task<IList<O2GTradeTableRow>> GetActiveOrders(O2GTableManager tableManager)
        {
            var tradesTable = (O2GTradesTable)tableManager.getTable(O2GTableType.Trades);
            var orders = new List<O2GTradeTableRow>();
            for (var i = 0; i < tradesTable.Count; i++)
            {
                var trade = tradesTable.getRow(i);
                orders.Add(trade);
            }

            return orders;
        }

        public async Task<O2GOfferTableRow> GetOffer(O2GTableManager tableManager, string instrument)
        {
            var offersTable = _tableManagerFacade.GetTable<O2GOffersTable>(tableManager, O2GTableType.Offers);
            for (int i = 0; i < offersTable.Count; i++)
            {
                var offer = offersTable.getRow(i);
                if (offer.Instrument.Equals(instrument))
                {
                    return offer;
                }
            }

            return null;
        }

        public async Task<O2GAccountRow> GetAccount(O2GTableManager tableManager, string accountId)
        {
            var accountsTable = _tableManagerFacade.GetTable<O2GAccountsTable>(tableManager, O2GTableType.Accounts);
            if (accountsTable.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < accountsTable.Count; i++)
            {
                var accountRow = accountsTable.getRow(i);
                if (accountRow.AccountID.Equals(accountId))
                {
                    return accountRow;
                }
            }

            return null;
        }

        public async Task<O2GOfferTableRow> GetOfferById(O2GTableManager tableManager, string offerID)
        {
            var offersTable = _tableManagerFacade.GetTable<O2GOffersTable>(tableManager, O2GTableType.Offers);
            for (int i = 0; i < offersTable.Count; i++)
            {
                var offer = offersTable.getRow(i);
                if (offer.OfferID.Equals(offerID))
                {
                    return offer;
                }
            }

            return null;
        }

        public async Task<IList<O2GOfferTableRow>> GetSubscribedOffers(O2GTableManager tableManager)
        {
            var offers = new List<O2GOfferTableRow>();
            var offersTable = _tableManagerFacade.GetTable<O2GOffersTable>(tableManager, O2GTableType.Offers);
            for (int i = 0; i < offersTable.Count; i++)
            {
                var offer = offersTable.getRow(i);
                if (offer.SubscriptionStatus == Constants.SubscriptionStatuses.Tradable)
                {
                    offers.Add(offer);
                }
            }

            return offers;
        }
    }
}
