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
        private readonly bool _validateConnectedState;
        private readonly ILogger _logger;
        private bool _wasConnected;
        private IAdsConnection _connection;
        private bool _initalConnection = true;

        internal event EventHandler<PlcConnectionChangeArgs> ConnectionStateChanged;

        internal PlcAdsConnectionProvider(string adsNetId, int adsPort, bool validateConnectedState = true, ILogger logger = null)
        {
            _amsAddr = new AmsAddress(adsNetId, adsPort);
            _validateConnectedState = validateConnectedState;
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
                _initalConnection = true;
                _client.Connect(_amsAddr);
            }
            catch (Exception ex)
            {
                throw new PlcAdsException($"Error establish ADS connection to {_amsAddr}.", ex);
            }
        }

        internal void Disconnect()
        {
            _logger.LogInformation("Disconnect from plc at {plc_ams_address}.", _amsAddr);
            _client.Disconnect();
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            // Validates the Events connection state, because this event return the informationen
            // how the quality of the connection is to the ADS Router, not to the target device.
            // With explicitly TryReadSate Method, the device connection is established and validated.
            if (_validateConnectedState && e.NewState == ConnectionState.Connected)
            {
                AdsErrorCode resultCode = _client.TryReadState(out StateInfo stateInfo);
                if (resultCode != AdsErrorCode.NoError)
                {
                    _logger.LogError("ADS Connection State Changed to {new_state} but the device response with AdsErrorCode={adsErrorCode}, DeviceState={deviceState} and AdsState={adsState}.", e.NewState, resultCode, stateInfo.DeviceState, stateInfo.AdsState);
                    if (_initalConnection)
                    {
                        // throw an exception. If this is is when calling _client.Connect() then the _client.Connect will throw the exception.
                        throw new PlcAdsException($"Error reading target device={_client.Address} state. AdsErrorCode={resultCode}, DeviceState={stateInfo.DeviceState}, AdsState={stateInfo.AdsState}");
                    }

                    // set the connection state to disconnected, because the device is not reachable
                    _client.Disconnect();
                }
            }

            if (_connection == null && e.NewState == ConnectionState.Connected)
            {
                _connection = ConnectionSynchronization.MakeSynchronized(_client);
                _initalConnection = false;
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
