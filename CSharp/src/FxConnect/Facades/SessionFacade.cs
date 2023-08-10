using System.Threading.Tasks;
using FxConnect.Listeners;
using fxcore2;

namespace FxConnect.Facades
{
    public class SessionFacade : ISessionFacade
    {
        public async Task SubscribeSessionStatus(O2GSession session, IO2GSessionStatus listener)
        {
            session.subscribeSessionStatus(listener);
        }

        public async Task<bool> Login(O2GSession session, string user, string password, string url, string connection)
        {
            var result = session.login(user, password, url, connection);
            return result;
        }

        public async Task Logout(O2GSession session)
        {
            session.logout();
        }

        public void UnsubscribeSessionStatus(O2GSession session, IO2GSessionStatus listener)
        {
            session.unsubscribeSessionStatus(listener);
        }

        public async Task Dispose(O2GSession session)
        {
            session.Dispose();
        }

        public async Task<O2GSession> CreateSession()
        {
            var session = O2GTransport.createSession();
            return session;
        }

        public async Task<O2GTableManager> GetTableManager(O2GSession session)
        {
            return session.getTableManager();
        }

        public async Task UnsubscribeSessionStatus(O2GSession session, ISessionStatusListener sessionStatusListener)
        {
            session.unsubscribeSessionStatus(sessionStatusListener);
        }

        public async Task UseTableManager(O2GSession session, O2GTableManagerMode mode, IO2GTableManagerListener listener)
        {
            session.useTableManager(mode, listener);
        }
    }
}
