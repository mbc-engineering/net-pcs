//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Common.Interface;
using Microsoft.Extensions.Logging;
using System;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Connection
{
    /// <summary>
    /// Verwaltet eine <see cref="IAdsConnection"/> für andere Services.
    /// </summary>
    public class PlcAdsConnectionService : IPlcAdsConnectionService, IServiceStartable, IDisposable
    {
        private readonly object _apiLock = new object();
        private readonly PlcAdsConnectionProvider _plcConnection;
        private bool _connected;
        private IAdsConnection _connection;

        private event EventHandler<PlcConnectionChangeArgs> ConnectionStateChangedInternal;

        public event EventHandler<PlcConnectionChangeArgs> ConnectionStateChanged
        {
            add
            {
                lock (_apiLock)
                {
                    ConnectionStateChangedInternal += value;
                    // Initialevent nur bei bestehnder Verbindung senden
                    if (_connected)
                    {
                        value.Invoke(this, new PlcConnectionChangeArgs(_connected, _connection));
                    }
                }
            }
            remove
            {
                lock (_apiLock)
                {
                    ConnectionStateChangedInternal -= value;
                }
            }
        }

        public PlcAdsConnectionService(string plcAdsHost, int plcAdsPort, bool validateConnectedState = true, ILogger adsLogger = null)
        {
            _plcConnection = new PlcAdsConnectionProvider(plcAdsHost, plcAdsPort, validateConnectedState, adsLogger);
            _plcConnection.ConnectionStateChanged += OnConnectionStateChanged;
        }

        public bool IsConnected
        {
            get
            {
                lock (_apiLock)
                {
                    return _connected;
                }
            }
        }

        public IAdsConnection Connection
        {
            get
            {
                lock (_apiLock)
                {
                    return _plcConnection.GetConnectedConnection();
                }
            }
        }

        public void Start()
        {
            _plcConnection.Connect();
        }

        public void Stop()
        {
            _plcConnection.Disconnect();
        }

        public void Dispose()
        {
            _plcConnection.ConnectionStateChanged -= OnConnectionStateChanged;
            _plcConnection.Dispose();
        }

        protected virtual void OnConnectionStateChanged(object sender, PlcConnectionChangeArgs e)
        {
            lock (_apiLock)
            {
                _connected = e.Connected;
                _connection = e.Connection;
                ConnectionStateChangedInternal?.Invoke(this, e);
            }
        }
    }
}
