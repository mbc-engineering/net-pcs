//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils.Connection;
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

            _client.ConnectionStateChanged += (s, e) =>
            {
                // Log some statistic
                _logger.Debug("ADS connection state changed from {oldState} to {newState}, reason {reason}.", e.OldState, e.NewState, e.Reason);

                OnConnectionStateChanged(e);
            };
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

                if (_client.IsConnected)
                {
                    _connection = ConnectionSynchronization.MakeSynchronized(_client);
                    _connection.AdsNotificationError += OnAdsNotificationError;

                    OnConnectionStateChanged(new ConnectionStateChangedEventArgs(
                        ConnectionStateChangedReason.Established, ConnectionState.Connected, ConnectionState.Disconnected));
                }
                else
                {
                    _connection = null;
                }
            }
            catch (Exception ex)
            {
                throw new PlcAdsException(string.Format(ConnectionResources.PlcAdsConnectingFailed, _amsAddr.NetId, _amsAddr.Port), ex);
            }
        }

        internal void Disconnect()
        {
            _logger.Info("Disconnect from plc at {plc_ams_address}.", _amsAddr);
            ConnectionStateChanged?.Invoke(this, new PlcConnectionChangeArgs(false, _connection));
            _client.Disconnect();
            _connection = null;
        }

        private void OnConnectionStateChanged(ConnectionStateChangedEventArgs e)
        {
            _logger.Info("ADS Connection State Change {old_state} -> {new_state} because of {reason}.", e.OldState, e.NewState, e.Reason);

            if (e.Exception != null)
            {
                _logger.Error(e.Exception, "ADS Connection State Changed to {new_state} Exception", e.NewState);
            }

            var connected = e.NewState == ConnectionState.Connected;
            if (_wasConnected != connected)
            {
                _wasConnected = connected;
                ConnectionStateChanged?.Invoke(this, new PlcConnectionChangeArgs(connected, _connection));
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
