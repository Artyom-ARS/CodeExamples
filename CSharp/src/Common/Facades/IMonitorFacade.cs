namespace Common.Facades
{
    public interface IMonitorFacade
    {
        void Enter(object _lock);

        void Exit(object _lock);

        bool TryEnter(object _lock, int lockTimeout);

        bool TryExit(object _lock);
    }
}
