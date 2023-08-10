using fxcore2;

namespace FxConnect.Listeners
{
    public interface IRequestListener
    {
        bool IsCompleted { get; }

        bool IsFailed { get; }

        O2GResponse Response { get; }

        void OnRequestCompleted(object sender, RequestCompletedEventArgs e);

        void OnRequestFailed(object sender, RequestFailedEventArgs e);

        bool WaitEvents();

        bool WaitEvents(int wait);
    }
}
