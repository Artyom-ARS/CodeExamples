using System.Threading;
using Common.Facades;
using fxcore2;

namespace FxConnect.Listeners
{
    public class SessionStatusListener : ISessionStatusListener
    {
        private readonly EventWaitHandle _syncSessionEvent;
        private readonly IConsole _console;

        public SessionStatusListener(IConsole console)
        {
            Reset();
            _syncSessionEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
            _console = console;
        }

        public bool Connected { get; private set; }

        public bool Disconnected { get; private set; }

        public bool Error { get; private set; }

        public void Reset()
        {
            Connected = false;
            Disconnected = false;
            Error = false;
        }

        public bool WaitEvents()
        {
            return _syncSessionEvent.WaitOne(30000);
        }

        public void onSessionStatusChanged(O2GSessionStatusCode status)
        {
            _console.WriteLine("Status: " + status.ToString());
            switch (status)
            {
                case O2GSessionStatusCode.Connected:
                    Connected = true;
                    Disconnected = false;
                    _syncSessionEvent.Set();
                    break;
                case O2GSessionStatusCode.Disconnected:
                    Connected = false;
                    Disconnected = true;
                    _syncSessionEvent.Set();
                    break;
                default:
                    _syncSessionEvent.Set();
                    break;
            }
        }

        public void onLoginFailed(string error)
        {
            _console.WriteLine("LoginWithTableManager error: " + error);
            Error = true;
        }
    }
}
