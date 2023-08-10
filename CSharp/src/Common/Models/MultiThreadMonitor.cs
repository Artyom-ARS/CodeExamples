using System;

namespace Common.Models
{
    public class MultiThreadMonitor
    {
        public bool Locked { get; private set; }

        public DateTime LockTime { get; private set; }

        private readonly object _object = new object();

        public bool TryEnter(int lockTimeout)
        {
            var now = DateTime.UtcNow;
            if (Locked && now < LockTime.AddMilliseconds(lockTimeout))
            {
                return false;
            }

            lock (_object)
            {
                now = DateTime.UtcNow;
                if (Locked && now < LockTime.AddMilliseconds(lockTimeout))
                {
                    return false;
                }

                LockTime = now;
                Locked = true;
            }

            return true;
        }

        public bool TryExit()
        {
            if (!Locked)
            {
                return false;
            }

            lock (_object)
            {
                if (!Locked)
                {
                    return false;
                }

                Locked = false;
            }

            return true;
        }
    }
}
