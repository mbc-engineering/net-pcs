using System;
using System.Threading;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Helper class to transfer data from one thread to another.
    /// </summary>
    internal class DataExchange<T>
    {
        private bool _signaled;
        private T _data;

        public T Data { get; }

        public void Set(T data)
        {
            lock (this)
            {
                _data = data;
                _signaled = true;
                Monitor.PulseAll(this);
            }
        }

        public T GetOrWait(TimeSpan timeout)
        {
            lock (this)
            {
                if (!_signaled)
                    if(!Monitor.Wait(this, timeout))
                        throw new TimeoutException();
                _signaled = false;
                return _data;
            }
        }
    }
}
