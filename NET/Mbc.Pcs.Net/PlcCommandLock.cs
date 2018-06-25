using System;
using System.Collections.Generic;
using System.Threading;

namespace Mbc.Pcs.Net
{
    internal static class PlcCommandLock
    {
        private static object _lock = new object();
        private static HashSet<string> _lockCommand = new HashSet<string>();

        public static IDisposable AcquireLock(string commandName)
        {
            Monitor.Enter(_lock);
            try
            {
                while (!_lockCommand.Add(commandName))
                {
                    Monitor.Wait(_lock);
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }

            return new AcquiredLock(commandName);
        }

        private class AcquiredLock : IDisposable
        {
            private string _commandName;

            internal AcquiredLock(string commandName)
            {
                _commandName = commandName;
            }

            public void Dispose()
            {
                Monitor.Enter(_lock);
                try
                {
                    _lockCommand.Remove(_commandName);
                    Monitor.PulseAll(_lock);
                }
                finally
                {
                    Monitor.Exit(_lock);
                }
            }
        }
    }
}
