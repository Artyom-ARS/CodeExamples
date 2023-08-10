using System;
using System.Threading.Tasks;
using Common.Facades;
using Common.Logging;
using FxConnect.Facades;
using FxConnect.Listeners;
using fxcore2;

namespace FxConnect.Providers
{
    public class AuthenticationProvider : IAuthenticationProvider
    {
        private readonly IParametersProvider _parametersProvider;
        private readonly IConsole _console;
        private readonly ISessionStatusListener _sessionStatusListener;
        private readonly ISessionFacade _sessionFacade;

        public AuthenticationProvider(IParametersProvider parametersProvider, IConsole console, ISessionFacade sessionFacade, ISessionStatusListener sessionStatusListener)
        {
            _parametersProvider = parametersProvider;
            _console = console;
            _sessionFacade = sessionFacade;
            _sessionStatusListener = sessionStatusListener;
        }

        public async Task<bool> TryLogin(O2GSession session)
        {
            var loginParams = _parametersProvider.LoginParameters;
            try
            {
                await _sessionFacade.SubscribeSessionStatus(session, _sessionStatusListener).ConfigureAwait(false);
                _sessionStatusListener.Reset();

                await _sessionFacade.Login(session, loginParams.Login, loginParams.Password, loginParams.Url, loginParams.Connection).ConfigureAwait(false);

                while (!_sessionStatusListener.Connected && !_sessionStatusListener.Disconnected && !_sessionStatusListener.Error)
                {
                    _sessionStatusListener.WaitEvents();
                }

                return _sessionStatusListener.Connected;
            }
            catch (Exception e)
            {
                _console.WriteLine($"Exception: {e}");
                Logger.Log.Error($"Failed to login. Login {loginParams.Login}. Exception: {e}");
            }

            return false;
        }

        public async Task<O2GSession> GetSession()
        {
            var session = await _sessionFacade.CreateSession().ConfigureAwait(false);
            return session;
        }

        public async Task Logout(O2GSession session)
        {
            if (session == null)
            {
                return;
            }

            _sessionStatusListener.Reset();
            await _sessionFacade.Logout(session).ConfigureAwait(false);

            while (!_sessionStatusListener.Connected && !_sessionStatusListener.Disconnected && !_sessionStatusListener.Error)
            {
                _sessionStatusListener.WaitEvents();
            }

            await _sessionFacade.UnsubscribeSessionStatus(session, _sessionStatusListener).ConfigureAwait(false);
            await _sessionFacade.Dispose(session).ConfigureAwait(false);
        }

        public async Task<bool> TryLoginWithTableManager(O2GSession session)
        {
            await _sessionFacade.UseTableManager(session, O2GTableManagerMode.Yes, null).ConfigureAwait(false);
            return await TryLogin(session).ConfigureAwait(false);
        }
    }
}
