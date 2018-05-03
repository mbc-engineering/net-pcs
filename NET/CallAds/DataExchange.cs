using System;
using System.Threading;

namespace AtomizerUI.link
{
    class DataExchange
    {
        private bool _signaled;
        private object _data;

        public void Set(object data)
        {
            lock (this)
            {
                _data = data;
                _signaled = true;
                Monitor.PulseAll(this);
            }
        }

        public object GetOrWait(TimeSpan timeout)
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
