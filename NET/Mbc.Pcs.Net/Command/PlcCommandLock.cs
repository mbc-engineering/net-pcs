//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Command
{
    internal static class PlcCommandLock
    {
        private static object _lock = new object();
        private static HashSet<string> _lockCommand = new HashSet<string>();

        public static IDisposable AcquireLock(string adsCommandFbPath, AmsAddress address, ExecutionBehavior behavior)
        {
            string uniqueCommandLock = $"{adsCommandFbPath}-on-{address.ToString()}";

            Monitor.Enter(_lock);

            try
            {
                while (!_lockCommand.Add(uniqueCommandLock))
                {
                    if(behavior == ExecutionBehavior.ThrowException)
                    {
                        throw new PlcCommandLockException(adsCommandFbPath, behavior, CommandResources.ERR_ExecutionBehaviorCommandLocked);
                    }

                    // Wait for Pulse and retest
                    Monitor.Wait(_lock);
                }
            }
            finally
            {
                Monitor.Exit(_lock);
            }

            return new AcquiredLock(uniqueCommandLock);
        }

        private class AcquiredLock : IDisposable
        {
            private readonly string _uniqueCommandLockName;

            internal AcquiredLock(string uniqueCommandLockName)
            {
                _uniqueCommandLockName = uniqueCommandLockName;
            }

            public void Dispose()
            {
                Monitor.Enter(_lock);
                try
                {
                    _lockCommand.Remove(_uniqueCommandLockName);
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
