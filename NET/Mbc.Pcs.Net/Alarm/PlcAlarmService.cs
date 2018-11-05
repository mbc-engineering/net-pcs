using Mbc.Common.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using TCEVENTLOGGERLib;
using TcEventLogProxyLib;

namespace Mbc.Pcs.Net.Alarm
{
    public class PlcAlarmService : IPlcAlarmService, IServiceStartable, IDisposable
    {
        private const int LcidGerman = 1031;

        private static readonly ILogger _log = LogManager.GetCurrentClassLogger();

        private readonly List<PlcAlarmEvent> _activeEvents = new List<PlcAlarmEvent>();
        private readonly ManualResetEventSlim _activeEventsInitialized = new ManualResetEventSlim(false);
        private readonly string _adsNetId;
        private readonly int _testPlaceNo;

        private TcEventLogAdsProxyClass _tcEventLog;

        public PlcAlarmService(string adsNetId, int testPlaceNo)
        {
            _adsNetId = adsNetId;
            _testPlaceNo = testPlaceNo;
        }

        public bool IsConnected => _tcEventLog != null;

        public event EventHandler<PlcAlarmChangeEventArgs> AlarmChanged;

        public event EventHandler<PlcAlarmChangeEventArgs> AlarmChangedWithInitialization
        {
            add
            {
                lock (_activeEvents)
                {
                    AlarmChanged += value;
                    foreach (var activeEvent in _activeEvents)
                    {
                        value(this, new PlcAlarmChangeEventArgs { ChangeType = PlcAlarmEventChangeType.New, AlarmEvent = activeEvent });
                    }
                }
            }
            remove
            {
                AlarmChanged -= value;
            }
        }

        public void Start()
        {
            Connect();
        }

        public void Stop()
        {
            Disconnect();
        }

        public void Dispose()
        {
            _activeEventsInitialized.Dispose();
        }

        public List<PlcAlarmEvent> GetActiveAlarms()
        {
            lock (_activeEvents)
            {
                return new List<PlcAlarmEvent>(_activeEvents);
            }
        }

        public void Connect()
        {
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
                    _log.Info("TwinCAT Event-Logger disconnected (reason = {reason}).", reason);
                };
                tcEventLog.Connect(_adsNetId);
                _tcEventLog = tcEventLog;
            }
            catch (Exception e)
            {
                _log.Error(e, "Could not start connection to TwincAT Event-Logger at {adsNetId}.", _adsNetId);

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
                _log.Error(e, "Could not initialize events.");
                Disconnect();

                throw;
            }
        }

        public void Disconnect()
        {
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
            if (!FilterSourceId(tcEvent.SrcId))
            {
                return;
            }

            try
            {
                var alarmEvent = new PlcAlarmEvent
                {
                    Id = tcEvent.Id,
                    SrcId = tcEvent.SrcId,
                    Class = (AlarmEventClass)tcEvent.Class,
                    Priority = tcEvent.Priority,
                    Date = tcEvent.Date + TimeSpan.FromMilliseconds(tcEvent.Ms),
                    Message = tcEvent.GetMsgString(LcidGerman),
                    SourceName = ReadSource(tcEvent),
                    State = (AlarmEventStates)tcEvent.State,
                    Flags = (AlarmEventFlags)tcEvent.Flags,
                    MustConfirm = tcEvent.MustConfirm != 0,
                    DateConfirmed = tcEvent.DateConfirmed + TimeSpan.FromMilliseconds(tcEvent.MsConfirmed),
                    DateReset = tcEvent.DateReset + TimeSpan.FromMilliseconds(tcEvent.MsReset),
                    UserData = tcEvent.UserData,
                    Context = GetAlarmContextFromSrcId(tcEvent.SrcId),
                };

                lock (_activeEvents)
                {
                    var existingAlarmEvent = _activeEvents
                        .FirstOrDefault(x => x.Id == alarmEvent.Id && x.SrcId == alarmEvent.SrcId);
                    if (existingAlarmEvent != null)
                    {
                        if (changeType == PlcAlarmEventChangeType.Clear)
                        {
                            _activeEvents.Remove(existingAlarmEvent);
                        }
                        else
                        {
                            var idx = _activeEvents.IndexOf(existingAlarmEvent);
                            _activeEvents[idx] = alarmEvent;
                        }
                    }
                    else
                    {
                        _activeEvents.Add(alarmEvent);
                    }

                    AlarmChanged?.Invoke(this, new PlcAlarmChangeEventArgs { ChangeType = changeType, AlarmEvent = alarmEvent });
                }
            }
            catch (Exception e)
            {
                _log.Warn(e, "Error handling TC-Event {0}.", tcEvent);
            }
        }

        private bool FilterSourceId(int srcId)
        {
            var group = srcId - (srcId % 1000);
            switch (group)
            {
                case 1000: // 1xyy => Prüfplatzalarm (yy=Prüfplatznummer 1-12)
                    return (srcId % 100) == _testPlaceNo;
                case 3000: // 3xyy => Prüfstandalarm (yy=Prüfstand 1-4)
                    return (srcId % 100) == (((_testPlaceNo - 1) % 3) + 1);
                case 5000: // 5xyy => Prüfgruppenalarm (yy=Prüfgruppe 1-2)
                    return (srcId % 100) == (((_testPlaceNo - 1) % 6) + 1);
                default:
                    return false;
            }
        }

        private AlarmEventContext GetAlarmContextFromSrcId(int srcId)
        {
            var group = srcId - (srcId % 1000);
            switch (group)
            {
                case 1000: // 1xyy => Prüfplatzalarm (yy=Prüfplatznummer 1-12)
                    return AlarmEventContext.TestPlace;
                case 3000: // 3xyy => Prüfstandalarm (yy=Prüfstand 1-4)
                    return AlarmEventContext.TestStand;
                case 5000: // 5xyy => Prüfgruppenalarm (yy=Prüfgruppe 1-2)
                    return AlarmEventContext.TestGroup;
                default:
                    throw new ArgumentOutOfRangeException(nameof(srcId), $"Source-ID {srcId} not in valid range.");
            }
        }

        private string ReadSource(ITcEvent tcEvent)
        {
            try
            {
                return tcEvent.SourceName[LcidGerman];
            }
            catch (COMException)
            {
                return string.Empty;
            }
        }
    }
}
