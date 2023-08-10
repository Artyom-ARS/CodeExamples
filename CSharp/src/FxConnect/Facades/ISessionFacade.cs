using System.Threading.Tasks;
using FxConnect.Listeners;
using fxcore2;

namespace FxConnect.Facades
{
    public interface ISessionFacade
    {
        Task<O2GSession> CreateSession();

        Task<bool> Login(O2GSession session, string user, string password, string url, string connection);

        Task SubscribeSessionStatus(O2GSession session, IO2GSessionStatus listener);

        Task Logout(O2GSession session);

        Task UnsubscribeSessionStatus(O2GSession session, ISessionStatusListener sessionStatusListener);

        Task Dispose(O2GSession session);

        Task UseTableManager(O2GSession session, O2GTableManagerMode mode, IO2GTableManagerListener listener);

        Task<O2GTableManager> GetTableManager(O2GSession session);

        void UnsubscribeSessionStatus(O2GSession session, IO2GSessionStatus listener);
    }
}
