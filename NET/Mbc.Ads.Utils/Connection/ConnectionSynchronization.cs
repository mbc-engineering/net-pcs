//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Castle.DynamicProxy;
using System;
using System.Linq;
using System.Threading;
using TwinCAT.Ads;

namespace Mbc.Ads.Utils.Connection
{
    /// <summary>
    /// Synchronize all access to the <see cref="IAdsConnection"/> interface.
    /// </summary>
    public class ConnectionSynchronization : IInterceptor
    {
        private readonly object _connectionLock = new object();

        /// <summary>
        /// Creates a <see cref="IAdsConnection"/> proxy object, whose members are
        /// synchronized.
        /// </summary>
        /// <param name="connection">The connection to syncronize.</param>
        /// <returns>A synchronized connection instance.</returns>
        public static IAdsConnection MakeSynchronized(IAdsConnection connection)
        {
            var proxyGenerator = new ProxyGenerator();
            return proxyGenerator.CreateInterfaceProxyWithTargetInterface<IAdsConnection>(connection, new ConnectionSynchronization());
        }

        public static ConnectionSynchronization GetSynchronizationInstance(IAdsConnection connection)
        {
            var proxyTargetAccessor = connection as IProxyTargetAccessor;
            if (proxyTargetAccessor == null || !proxyTargetAccessor.GetInterceptors().Any(x => x is ConnectionSynchronization))
            {
                throw new ArgumentException("Object is not synchronized.");
            }

            return proxyTargetAccessor.GetInterceptors().OfType<ConnectionSynchronization>().First();
        }

        private ConnectionSynchronization()
        {
        }

        /// <summary>
        /// Gets a boolean value indicating whether the lock of the underlying connection
        /// is entered.
        /// </summary>
        public bool IsLocked => Monitor.IsEntered(_connectionLock);

        void IInterceptor.Intercept(IInvocation invocation)
        {
            lock (_connectionLock)
            {
                invocation.Proceed();
            }
        }
    }
}
