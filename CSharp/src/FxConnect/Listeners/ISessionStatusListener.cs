using fxcore2;

namespace FxConnect.Listeners
{
    public interface ISessionStatusListener : IO2GSessionStatus
    {
        bool Connected { get; }

        bool Disconnected { get; }

        bool Error { get; }

        void Reset();

        bool WaitEvents();
    }
}
