using System;
using System.Threading;
using Common.Logging;
using fxcore2;

namespace FxConnect.Listeners
{
    public class RequestListener : IRequestListener
    {
        public bool IsCompleted { get; private set; }

        public bool IsFailed { get; private set; }

        public O2GResponse Response { get; private set; }

        private readonly EventWaitHandle _syncResponseEvent;

        public RequestListener()
        {
            Response = null;
            IsCompleted = false;
            IsFailed = false;
            _syncResponseEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
        }

        public bool WaitEvents()
        {
            return WaitEvents(30000);
        }

        public bool WaitEvents(int wait)
        {
            var waitEvents = _syncResponseEvent.WaitOne(wait);
            return waitEvents;
        }

        public void OnRequestCompleted(object sender, RequestCompletedEventArgs e)
        {
            Response = e.Response;
            IsCompleted = true;
            _syncResponseEvent.Set();
        }

        public void OnRequestFailed(object sender, RequestFailedEventArgs e)
        {
            var error = e.Error;
            Console.WriteLine("Request failed: " + error);
            var message = $"Request failed with reason: {error}";
            Logger.Log.Error(message);
            IsFailed = true;
            _syncResponseEvent.Set();
        }
    }
}
