//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils.Connection;
using Microsoft.Extensions.Logging;
using Optional;
using System;
using TwinCAT;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Connection
{
    internal class PlcAdsConnectionProvider : IDisposable
    {
        private readonly AmsAddress _amsAddr;
        private readonly AdsClient _client;
        private readonly ILogger _logger;
        private bool _wasConnected;
        private IAdsConnection _connection;

        internal event EventHandler<PlcConnectionChangeArgs> ConnectionStateChanged;

        internal PlcAdsConnectionProvider(string adsNetId, int adsPort, ILogger logger = null)
        {
            _amsAddr = new AmsAddress(adsNetId, adsPort);
            _logger = logger;

            var settings = new AdsClientSettings(1000);
            _client = new AdsClient(null, settings, logger);

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
                _logger.LogInformation("Trying to connect to plc at {plc_ams_address}.", _amsAddr);
                _client.Connect(_amsAddr);
            }
            catch (Exception ex)
            {
                throw new PlcAdsException("Error starting ADS connection. (See inner exception for details.)", ex);
            }
        }

        internal void Disconnect()
        {
            _logger.LogInformation("Disconnect from plc at {plc_ams_address}.", _amsAddr);
            _client.Disconnect();
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (_connection == null && e.NewState == ConnectionState.Connected)
            {
                _connection = ConnectionSynchronization.MakeSynchronized(_client);
            }

            _logger.LogInformation("ADS Connection State Change {old_state} -> {new_state} because of {reason}.", e.OldState, e.NewState, e.Reason);

            if (e.Exception != null)
            {
                _logger.LogError(e.Exception, "ADS Connection State Changed to {new_state} Exception", e.NewState);
            }

            var connected = e.NewState == ConnectionState.Connected;
            if (_wasConnected != connected)
            {
                _wasConnected = connected;
                _logger.LogInformation("Notify Listener about connection state change to {state}.", e.NewState);

                try
                {
                    ConnectionStateChanged?.Invoke(this, new PlcConnectionChangeArgs(connected, _connection));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Listener throws error: {error}", ex.Message);
                    throw;
                }
            }

            if (e.NewState == ConnectionState.Disconnected)
            {
                // remove connection after all listener are notified
                _connection = null;
            }
        }

        private void OnAdsNotificationError(object sender, AdsNotificationErrorEventArgs e)
        {
            _logger.LogError(e.Exception, "ADS Notification Error.");
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
