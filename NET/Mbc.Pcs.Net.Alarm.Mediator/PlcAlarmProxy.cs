//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using NLog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using TCEVENTLOGGERLib;
using TcEventLogProxyLib;

namespace Mbc.Pcs.Net.Alarm.Mediator
{
    /// <summary>
    /// Manages alarms from TcEventLogger V1, see https://infosys.beckhoff.com/index.php?content=../content/1031/tceventlogger/html/introduction.htm&id=4955746947972620140
    /// </summary>
    internal class PlcAlarmProxy : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly ManualResetEventSlim _activeEventsInitialized = new ManualResetEventSlim(false);
        private readonly object _alarmChangedLock = new object();
        private readonly string _adsNetId;
        private readonly int _languageId;

        private TcEventLogAdsProxyClass _tcEventLog;

        public event EventHandler<PlcAlarmChangeEventArgs> AlarmChanged;
        public event EventHandler OnDisconnect;

        public PlcAlarmProxy(string adsNetId, int languageId)
        {
            // TcEventLogger V1 Works only in a 32Bit Process, because the native dll are x86 compiled
            if (Environment.Is64BitProcess)
            {
                throw new PlatformNotSupportedException("PlcAlarmService uses TcEventLogger V1, that only supports x86");
            }

            _languageId = languageId;
            _adsNetId = adsNetId;
        }

        public bool IsConnected => _tcEventLog != null && _tcEventLog.Connected;

        public void Dispose()
        {
            Logger.Debug("Disposing");

            if (_tcEventLog != null)
            {
                Marshal.ReleaseComObject(_tcEventLog);
                _tcEventLog = null;
            }

            _activeEventsInitialized.Dispose();
        }

        public void Connect()
        {
            Logger.Debug("Connecting to {adsNetId}", _adsNetId);

            TcEventLogAdsProxyClass tcEventLog = CreateTcEventLogAdsProxyClass();

            try
            {
                tcEventLog.OnNewEvent += TcEventLogOnOnNewEvent;
                tcEventLog.OnConfirmEvent += TcEventLogOnOnConfirmEvent;
                tcEventLog.OnResetEvent += TcEventLogOnOnResetEvent;
                tcEventLog.OnSignalEvent += TcEventLogOnOnSignalEvent;
                tcEventLog.OnClearEvent += TcEventLogOnOnClearEvent;
                tcEventLog.OnDisconnect += (reason) =>
                {
                    Logger.Warn("TwinCAT Event-Logger disconnected (reason = {reason}).", reason);
                    OnDisconnect?.Invoke(this, null);
                };
                tcEventLog.Connect(_adsNetId);
                _tcEventLog = tcEventLog;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not start connection to TwincAT Event-Logger at {adsNetId}.", _adsNetId);

                tcEventLog.Disconnect();
                Marshal.ReleaseComObject(tcEventLog);

                throw;
            }

            try
            {
                InitializeActiveEvents();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not initialize events.");
                Disconnect();

                throw;
            }
        }

        public void Disconnect()
        {
            Logger.Debug("Disconnecting");

            if (_tcEventLog != null)
            {
                _tcEventLog.Disconnect();
                Marshal.ReleaseComObject(_tcEventLog);
                _tcEventLog = null;
            }
        }

        protected virtual TcEventLogAdsProxyClass CreateTcEventLogAdsProxyClass()
        {
            return new TcEventLogAdsProxyClass();
        }

        private void InitializeActiveEvents()
        {
            foreach (TcEvent tcEvent in _tcEventLog.EnumActiveEventsEx())
            {
                OnEventChange(PlcAlarmEventChangeType.New, tcEvent);
            }

            _activeEventsInitialized.Set();
        }

        private void TcEventLogOnOnClearEvent(object evtObj)
        {
            _activeEventsInitialized.Wait();
            OnEventChange(PlcAlarmEventChangeType.Clear, (ITcEvent)evtObj);
        }

        private void TcEventLogOnOnSignalEvent(object evtObj)
        {
            _activeEventsInitialized.Wait();
            OnEventChange(PlcAlarmEventChangeType.Signal, (ITcEvent)evtObj);
        }

        private void TcEventLogOnOnResetEvent(object evtObj)
        {
            _activeEventsInitialized.Wait();
            OnEventChange(PlcAlarmEventChangeType.Reset, (ITcEvent)evtObj);
        }

        private void TcEventLogOnOnConfirmEvent(object evtObj)
        {
            _activeEventsInitialized.Wait();
            OnEventChange(PlcAlarmEventChangeType.Confirm, (ITcEvent)evtObj);
        }

        private void TcEventLogOnOnNewEvent(object evtObj)
        {
            _activeEventsInitialized.Wait();
            OnEventChange(PlcAlarmEventChangeType.New, (ITcEvent)evtObj);
        }

        private void OnEventChange(PlcAlarmEventChangeType changeType, ITcEvent tcEvent)
        {
            try
            {
                var alarmEvent = new PlcAlarmEvent
                {
                    Id = tcEvent.Id,
                    SrcId = tcEvent.SrcId,
                    Class = (AlarmEventClass)tcEvent.Class,
                    Priority = tcEvent.Priority,
                    Date = tcEvent.Date + TimeSpan.FromMilliseconds(tcEvent.Ms),
                    Message = tcEvent.GetMsgString(_languageId),
                    SourceName = ReadSource(tcEvent),
                    State = (AlarmEventStates)tcEvent.State,
                    Flags = (AlarmEventFlags)tcEvent.Flags,
                    MustConfirm = tcEvent.MustConfirm != 0,
                    DateConfirmed = tcEvent.DateConfirmed + TimeSpan.FromMilliseconds(tcEvent.MsConfirmed),
                    DateReset = tcEvent.DateReset + TimeSpan.FromMilliseconds(tcEvent.MsReset),
                    UserData = tcEvent.UserData,
                    ArgumentData = ReadArgumentData(tcEvent),
                };

                lock (_alarmChangedLock)
                {
                    AlarmChanged?.Invoke(this, new PlcAlarmChangeEventArgs { ChangeType = changeType, AlarmEvent = alarmEvent });
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e, "Error handling TC-Event {0}.", tcEvent);
            }
        }

        private string ReadSource(ITcEvent tcEvent)
        {
            try
            {
                return tcEvent.SourceName[_languageId];
            }
            catch (COMException)
            {
                return string.Empty;
            }
        }

        private IReadOnlyList<object> ReadArgumentData(ITcEvent tcEvent)
        {
            Array data = null;
            tcEvent.GetData(ref data);
            var list = new List<object>(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                list.Add(data.GetValue(i));
            }

            return list.AsReadOnly();
        }
    }
}
