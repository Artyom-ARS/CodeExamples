using System.Threading.Tasks;
using fxcore2;

namespace FxConnect.Providers
{
    public interface IAuthenticationProvider
    {
        Task<O2GSession> GetSession();

        Task Logout(O2GSession session);

        Task<bool> TryLogin(O2GSession session);

        Task<bool> TryLoginWithTableManager(O2GSession session);
    }
}
