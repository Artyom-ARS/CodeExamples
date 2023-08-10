using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common.Logging;

namespace OnlinePlatform.Providers
{
    public class LockProvider : ILockProvider
    {
        private const int LockTimeout = 5000;
        private readonly ConcurrentDictionary<string, DateTime> _lockList = new ConcurrentDictionary<string, DateTime>();

        public async Task<bool> Lock(string expertId)
        {
            var now = DateTime.UtcNow;
            var current = _lockList.GetOrAdd(expertId, now.AddMilliseconds(-1));
            if (now <= current)
            {
                return false;
            }
            _lockList[expertId] = now.AddMilliseconds(LockTimeout);
            Logger.Log.Debug($"Lock {expertId}");
            return true;
        }

        public async Task UnLock(string expertId)
        {
            _lockList.AddOrUpdate(expertId, DateTime.MinValue, (key, value) => DateTime.MinValue);
        }
    }
}
