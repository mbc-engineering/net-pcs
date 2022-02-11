//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils.Connection;
using Mbc.AsyncUtils;
using NLog;
using Optional;
using System;
using TwinCAT;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Connection
{
    internal class PlcAdsConnectionProvider : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private AsyncSerializedTaskExecutor _notificationExecutor = new AsyncSerializedTaskExecutor();
        private readonly AmsAddress _amsAddr;
        private readonly AdsClient _client;
        private bool _wasConnected;
        private IAdsConnection _connection;

        internal event EventHandler<PlcConnectionChangeArgs> ConnectionStateChanged;

        internal PlcAdsConnectionProvider(string adsNetId, int adsPort)
        {
            _amsAddr = new AmsAddress(adsNetId, adsPort);

            var settings = new AdsClientSettings(1000);
            _client = new AdsClient(settings);

            _client.AdsNotificationError += OnAdsNotificationError;
            _client.ConnectionStateChanged += OnConnectionStateChanged;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        internal virtual Option<IAdsConnection> Connection => _connection.SomeNotNull<IAdsConnection>();

        internal virtual void Connect()
        {
            try
            {
                _logger.Info("Trying to connect to plc at {plc_ams_address}.", _amsAddr);
                _client.Connect(_amsAddr);
            }
            catch (Exception ex)
            {
                throw new PlcAdsException("Error starting ADS connection. (See inner exception for details.)", ex);
            }

        }

        internal void Disconnect()
        {
            _logger.Info("Disconnect from plc at {plc_ams_address}.", _amsAddr);
            _client.Disconnect();
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (_connection == null && e.NewState == ConnectionState.Connected)
            {
                _connection = ConnectionSynchronization.MakeSynchronized(_client);
            }

            if (e.NewState == ConnectionState.Disconnected)
            {
                _connection = null;
            }

            _logger.Info("ADS Connection State Change {old_state} -> {new_state} because of {reason}.", e.OldState, e.NewState, e.Reason);

            if (e.Exception != null)
            {
                _logger.Error(e.Exception, "ADS Connection State Changed to {new_state} Exception", e.NewState);
            }

            var connected = e.NewState == ConnectionState.Connected;
            if (_wasConnected != connected)
            {
                _wasConnected = connected;
                _logger.Info("Notify Listener about connection state change to {state}.", e.NewState);

                ConnectionStateChanged?.Invoke(this, new PlcConnectionChangeArgs(connected, _connection));
                try
                {
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Listener throws error: {error}", ex.Message);
                }
            }
        }

        private void OnAdsNotificationError(object sender, AdsNotificationErrorEventArgs e)
        {
            _logger.Error(e.Exception, "ADS Notification Error.");
        }

        internal virtual IAdsConnection GetConnectedConnection()
        {
            return Connection.ValueOr(() =>
            {
                throw new PlcAdsException(ConnectionResources.NoPlcAdsConnection);
            });
        }
    }
}
