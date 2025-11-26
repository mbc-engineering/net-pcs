//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
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
        private bool _autoReconnectEnabled;
        private ILogger _logger = null;
        private bool _serviceStarted;
        private CancellationTokenSource _reconnectCancellationTokenSource;
        private Task _reconnectTask;

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

        public PlcAdsConnectionService(string plcAdsHost, int plcAdsPort, bool validateConnectedState = true, bool autoReconnectEnabled = false, ILoggerFactory loggerFactory = null)
        {
            _plcConnection = new PlcAdsConnectionProvider(plcAdsHost, plcAdsPort, validateConnectedState, loggerFactory);
            _plcConnection.ConnectionStateChanged += OnConnectionStateChanged;
            _autoReconnectEnabled = autoReconnectEnabled;
            _logger = loggerFactory?.CreateLogger<PlcAdsConnectionService>();
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

        public TimeSpan ReconnectionTime { get; set; } = TimeSpan.FromSeconds(30);

        public void Start()
        {
            _plcConnection.Connect();
            _serviceStarted = true;
        }

        public void Stop()
        {
            _serviceStarted = false;
            StopReconnectTask();
            _plcConnection.Disconnect();
        }

        public void Dispose()
        {
            StopReconnectTask();
            _plcConnection.ConnectionStateChanged -= OnConnectionStateChanged;
            _plcConnection.Dispose();
        }

        protected virtual void OnConnectionStateChanged(object sender, PlcConnectionChangeArgs e)
        {
            lock (_apiLock)
            {
                _connected = e.Connected;
                _connection = e.Connection;

                // Execute auto-reconnect if enabled
                if (_autoReconnectEnabled && _serviceStarted && !_connected)
                {
                    StartReconnectTask();
                }
                else if (_connected)
                {
                    StopReconnectTask();
                }
            }

            ConnectionStateChangedInternal?.Invoke(this, e);
        }

        private void StartReconnectTask()
        {
            
            // Stop existing reconnect task if running
            StopReconnectTask();

            _reconnectCancellationTokenSource = new CancellationTokenSource();
            var cancelationToken = _reconnectCancellationTokenSource.Token;

            _reconnectTask = Task.Run(async () =>
            {
                while (!cancelationToken.IsCancellationRequested)
                {
                    try
                    {
                        _logger?.LogInformation("Wait {time} to try a reconnection to PLC.", ReconnectionTime);
                        await Task.Delay(ReconnectionTime, cancelationToken);

                        if (!cancelationToken.IsCancellationRequested && !_connected)
                        {
                            _logger?.LogInformation("Try a reconnection to PLC.");
                            _plcConnection.Connect();
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when cancellation is requested
                        break;
                    }
                    catch (Exception)
                    {
                        // Continue reconnection attempts even if one fails
                    }
                }
            }, cancelationToken);
        }

        private void StopReconnectTask()
        {
            _reconnectCancellationTokenSource?.Cancel();
            _reconnectCancellationTokenSource?.Dispose();
            _reconnectCancellationTokenSource = null;
            _reconnectTask = null;
        }
    }
}
