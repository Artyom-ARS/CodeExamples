using System;
using System.Threading.Tasks;
using Common;
using Common.Facades;
using FxConnect.Listeners;
using fxcore2;

namespace FxConnect.Clients
{
    public class FxConnectClient : IFxConnectClient
    {
        private readonly string _lock;
        private readonly IFactory _factory;
        private readonly IMonitorFacade _monitorFacade;
        private IRequestListener _requestListener;

        public FxConnectClient(IFactory factory, IMonitorFacade monitorFacade)
        {
            _lock = $"FxConnectClient-{DateTime.Now.Ticks}";
            _factory = factory;
            _monitorFacade = monitorFacade;
        }

        public async Task<O2GResponse> SendRequest(O2GSession session, O2GRequest request)
        {
            session.sendRequest(request);

            if (!_requestListener.WaitEvents())
            {
                throw new Exception("Response waiting timeout expired");
            }

            if (_requestListener.IsFailed)
            {
                throw new Exception("Request failed");
            }

            var response = _requestListener.Response;
            return response;
        }

        public async Task SendRequestWithoutResponse(O2GSession session, O2GRequest request)
        {
            session.sendRequest(request);
        }

        public async Task StartRequest(O2GSession session)
        {
            _monitorFacade.Enter(_lock);
            _requestListener = _factory.Get<IRequestListener, RequestListener>();
            session.RequestCompleted += _requestListener.OnRequestCompleted;
            session.RequestFailed += _requestListener.OnRequestFailed;
        }

        public async Task EndRequest(O2GSession session)
        {
            session.RequestCompleted -= _requestListener.OnRequestCompleted;
            session.RequestFailed -= _requestListener.OnRequestFailed;
            _requestListener = null;
            _monitorFacade.Exit(_lock);
        }
    }
}
