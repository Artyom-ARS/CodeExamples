using System.Threading.Tasks;
using fxcore2;

namespace FxConnect.Clients
{
    public interface IFxConnectClient
    {
        Task EndRequest(O2GSession session);

        Task<O2GResponse> SendRequest(O2GSession session, O2GRequest request);

        Task SendRequestWithoutResponse(O2GSession session, O2GRequest request);

        Task StartRequest(O2GSession session);
    }
}
