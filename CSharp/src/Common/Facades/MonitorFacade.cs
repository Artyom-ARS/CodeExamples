using System.Threading;

namespace Common.Facades
{
    public class MonitorFacade : IMonitorFacade
    {
        public void Enter(object _lock)
        {
            Monitor.Enter(_lock);
        }

        public bool TryEnter(object _lock, int lockTimeout)
        {
            return Monitor.TryEnter(_lock, lockTimeout);
        }

        public void Exit(object _lock)
        {
            Monitor.Exit(_lock);
        }

        public bool TryExit(object _lock)
        {
            try
            {
                Monitor.Exit(_lock);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
