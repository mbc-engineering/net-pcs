//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Common.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mbc.Pcs.Net.Alarm.Service
{
    public class PlcAlarmService : IPlcAlarmService, IServiceStartable, IDisposable
    {
        private static readonly ILogger _log = LogManager.GetCurrentClassLogger();

        private readonly List<PlcAlarmEvent> _activeEvents = new List<PlcAlarmEvent>();
        private readonly string _adsNetId;
        private readonly int _testPlaceNo;
        private Process _plcAlarmServiceMediator;

        public PlcAlarmService(string adsNetId, int testPlaceNo)
        {
            _adsNetId = adsNetId;
            _testPlaceNo = testPlaceNo;
        }

        public bool IsConnected => _plcAlarmServiceMediator != null && !_plcAlarmServiceMediator.HasExited;

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
            Disconnect();
        }

        public virtual void Connect()
        {
            _plcAlarmServiceMediator = new Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            startInfo.Arguments = $"--adsnetid {_adsNetId}";
            startInfo.FileName = "Mbc.Pcs.Net.Alarm.Mediator.exe";

            _plcAlarmServiceMediator.StartInfo = startInfo;
            _plcAlarmServiceMediator.OutputDataReceived += OnPlcAlarmServiceMediatorStdoutDataReceived;
            _plcAlarmServiceMediator.Exited += OnPlcAlarmServiceMediatorExited;

            _log.Info($"Starting process {startInfo.FileName} {startInfo.Arguments}");
            _plcAlarmServiceMediator.Start();
            _plcAlarmServiceMediator.BeginOutputReadLine();
        }

        public virtual void Disconnect()
        {
            _log.Info($"Disconnecting process {nameof(PlcAlarmService)}");

            if (IsConnected)
            {
                _plcAlarmServiceMediator.OutputDataReceived -= OnPlcAlarmServiceMediatorStdoutDataReceived;
                _plcAlarmServiceMediator.Exited += OnPlcAlarmServiceMediatorExited;

                _plcAlarmServiceMediator.StandardInput.WriteLine("quit");

                if (!_plcAlarmServiceMediator.WaitForExit(5000))
                {
                    // use the hammer
                    _log.Info($"Time-Out reached for waiting of exit of Mbc.Pcs.Net.Alarm.Mediator.exe. No try to kill the process.");
                    _plcAlarmServiceMediator.Kill();
                }
            }

            lock (_activeEvents)
            {
                _activeEvents.Clear();
            }

            _plcAlarmServiceMediator = null;
        }

        public List<PlcAlarmEvent> GetActiveAlarms()
        {
            lock (_activeEvents)
            {
                return new List<PlcAlarmEvent>(_activeEvents);
            }
        }

        protected void OnPlcAlarmServiceMediatorStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            // If correct type, fire eventArgs
            if (JsonConvert.TryDeserializeObject(e.Data, out PlcAlarmChangeEventArgs eventData))
            {
                OnEventChange(eventData);
            }
        }

        private void OnPlcAlarmServiceMediatorExited(object sender, EventArgs e)
        {
            _log.Error($"Unexpected close of Mbc.Pcs.Net.Alarm.Mediator.exe with code: '{(sender as Process)?.ExitCode ?? 0}'");
        }

        protected void OnEventChange(PlcAlarmChangeEventArgs plcAlarmChangeEventArgs)
        {
            if (plcAlarmChangeEventArgs == null
                || plcAlarmChangeEventArgs.AlarmEvent == null
                || !FilterSourceId(plcAlarmChangeEventArgs.AlarmEvent.SrcId))
            {
                return;
            }

            try
            {
                lock (_activeEvents)
                {
                    var existingAlarmEvent = _activeEvents
                        .FirstOrDefault(x => x.Id == plcAlarmChangeEventArgs.AlarmEvent.Id && x.SrcId == plcAlarmChangeEventArgs.AlarmEvent.SrcId);
                    if (existingAlarmEvent != null)
                    {
                        if (plcAlarmChangeEventArgs.ChangeType == PlcAlarmEventChangeType.Clear)
                        {
                            _activeEvents.Remove(existingAlarmEvent);
                        }
                        else
                        {
                            var idx = _activeEvents.IndexOf(existingAlarmEvent);
                            _activeEvents[idx] = plcAlarmChangeEventArgs.AlarmEvent;
                        }
                    }
                    else
                    {
                        _activeEvents.Add(plcAlarmChangeEventArgs.AlarmEvent);
                    }

                    AlarmChanged?.Invoke(this, plcAlarmChangeEventArgs);
                }
            }
            catch (Exception e)
            {
                _log.Warn(e, "Error handling PlcAlarmChangeEventArgs from 'Mbc.Pcs.Net.Alarm.Mediator.exe'.");
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
    }
}
