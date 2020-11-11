//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Common.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Mbc.Pcs.Net.Alarm.Service
{
    public class PlcAlarmService : IPlcAlarmService, IServiceStartable, IDisposable
    {
        private static readonly ILogger _log = LogManager.GetCurrentClassLogger();

        private readonly List<PlcAlarmEvent> _activeEvents = new List<PlcAlarmEvent>();
        private readonly string _adsNetId;
        private readonly int _languageId;
        private Process _plcAlarmServiceMediator;

        public PlcAlarmService(string adsNetId, int languageId)
        {
            _adsNetId = adsNetId;
            _languageId = languageId;
        }

        public PlcAlarmService(string adsNetId, int languageId, IPlcAlarmEventFormatter plcAlarmEventFormatter)
            : this(adsNetId, languageId)
        {
            PlcAlarmEventFormatter = plcAlarmEventFormatter;
        }

        public bool IsConnected => _plcAlarmServiceMediator != null && !_plcAlarmServiceMediator.HasExited;

        public IPlcAlarmEventFormatter PlcAlarmEventFormatter { get; } = new EmptyPlcAlarmEventFormatter();

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

        public event EventHandler<DataEventArgs> Error;
        public event EventHandler<DataEventArgs> Exited;

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
            startInfo.StandardOutputEncoding = Encoding.UTF8;

            startInfo.Arguments = $"--adsnetid {_adsNetId} ";
            startInfo.Arguments += $"--languageid {_languageId} ";
            startInfo.FileName = "Mbc.Pcs.Net.Alarm.Mediator.exe";

            _plcAlarmServiceMediator.StartInfo = startInfo;
            _plcAlarmServiceMediator.EnableRaisingEvents = true;
            _plcAlarmServiceMediator.Exited += OnPlcAlarmServiceMediatorExited;
            _plcAlarmServiceMediator.OutputDataReceived += OnPlcAlarmServiceMediatorStdoutDataReceived;

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
                _plcAlarmServiceMediator.Exited -= OnPlcAlarmServiceMediatorExited;

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

        protected virtual void OnPlcAlarmServiceMediatorStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            // If correct type, fire eventArgs
            if (JsonConvert.TryDeserializeObject(e.Data, out PlcAlarmChangeEventArgs eventData))
            {
                OnEventChange(eventData);
            }
            else if (e.Data != null)
            {
                _log.Error($"Could not read incoming plc event-data. Data:" + e.Data);
                OnError(e.Data);
            }
            else if (e.Data == null)
            {
                _log.Error($"Message is empty, service has probably been stopped.");
                Stop();
            }
        }

        protected virtual void OnPlcAlarmServiceMediatorExited(object sender, EventArgs e)
        {
            _log.Error($"Unexpected close of Mbc.Pcs.Net.Alarm.Mediator.exe with code: '{(sender as Process)?.ExitCode ?? 0}'");
            Exited?.Invoke(this, new DataEventArgs($"Exit code: '{(sender as Process)?.ExitCode ?? 0}'"));
        }

        protected virtual void OnEventChange(PlcAlarmChangeEventArgs plcAlarmChangeEventArgs)
        {
            if (plcAlarmChangeEventArgs == null
                || plcAlarmChangeEventArgs.AlarmEvent == null
                || !FilterSourceId(plcAlarmChangeEventArgs.AlarmEvent.SrcId))
            {
                return;
            }

            try
            {
                var formatedAlarmEventArgs = new PlcAlarmChangeEventArgs()
                {
                    ChangeType = plcAlarmChangeEventArgs.ChangeType,
                    AlarmEvent = FormatPlcAlarmEvent(plcAlarmChangeEventArgs.AlarmEvent),
                };

                lock (_activeEvents)
                {
                    var existingAlarmEvent = _activeEvents
                        .FirstOrDefault(x => x.Id == formatedAlarmEventArgs.AlarmEvent.Id && x.SrcId == formatedAlarmEventArgs.AlarmEvent.SrcId);
                    if (existingAlarmEvent != null)
                    {
                        if (formatedAlarmEventArgs.ChangeType == PlcAlarmEventChangeType.Clear)
                        {
                            _activeEvents.Remove(existingAlarmEvent);
                        }
                        else
                        {
                            var idx = _activeEvents.IndexOf(existingAlarmEvent);
                            _activeEvents[idx] = formatedAlarmEventArgs.AlarmEvent;
                        }
                    }
                    else
                    {
                        _activeEvents.Add(formatedAlarmEventArgs.AlarmEvent);
                    }

                    AlarmChanged?.Invoke(this, formatedAlarmEventArgs);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Error handling PlcAlarmChangeEventArgs from 'Mbc.Pcs.Net.Alarm.Mediator.exe'.");
                OnError(e.Message);
            }
        }

        protected virtual PlcAlarmEvent FormatPlcAlarmEvent(PlcAlarmEvent plcAlarmEvent)
        {
            if (PlcAlarmEventFormatter != null)
            {
                return PlcAlarmEventFormatter.Format(plcAlarmEvent);
            }

            return plcAlarmEvent;
        }

        protected virtual void OnError(string data)
        {
            Error?.Invoke(this, new DataEventArgs(data));
        }

        protected virtual bool FilterSourceId(int srcId)
        {
            return true;
        }
    }
}
