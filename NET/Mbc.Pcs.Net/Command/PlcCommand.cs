//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using NLog;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Kapselt ein PLC-Kommando.
    /// </summary>
    public class PlcCommand : IPlcCommand
    {
        public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Occurs when the state of a command is changed.
        /// </summary>
        public event EventHandler<PlcCommandEventArgs> StateChanged;

        private readonly IAdsConnection _adsConnection;
        private readonly string _adsCommandFbPath;
        private readonly CommandResource _commandResource;
        private readonly CommandArgumentHandler _commandArgumentHandler;

        public PlcCommand(IAdsConnection adsConnection, string adsCommandFbPath, CommandResource commandResource = null, CommandArgumentHandler commandArgumentHandler = null)
        {
            _adsConnection = adsConnection;
            _adsCommandFbPath = adsCommandFbPath;
            _commandResource = commandResource ?? new CommandResource();
            _commandArgumentHandler = commandArgumentHandler ?? new PrimitiveCommandArgumentHandler();
        }

        /// <summary>
        /// Maximale time to wait for command completion.
        /// </summary>
        public TimeSpan Timeout { get; set; } = DefaultTimeout;

        /// <summary>
        /// The PLC Variable
        /// </summary>
        public string AdsCommandFbPath => _adsCommandFbPath;

        /// <summary>
        /// Defines the beavior how to react to parallel exection of this command.
        /// Default is locking the second caller and wait for the end of the first command.
        /// </summary>
        public ExecutionBehavior ExecutionBehavior { get; set; }

        /// <summary>
        /// Executes a PLC command.
        /// </summary>
        /// <seealso cref="Execute(CancellationToken, ICommandInput, ICommandOutput)"/>
        public DateTime Execute(ICommandInput input = null, ICommandOutput output = null)
        {
            return Execute(CancellationToken.None, input, output);
        }

        /// <summary>
        /// Executes a PLC command asynchronously.
        /// </summary>
        /// <seealso cref="Execute(CancellationToken, ICommandInput, ICommandOutput)"/>
        public Task<DateTime> ExecuteAsync(ICommandInput input = null, ICommandOutput output = null)
        {
            return Task.Run(() => Execute(CancellationToken.None, input, output));
        }

        /// <summary>
        /// Executes a PLC command asynchronously.
        /// </summary>
        /// <seealso cref="Execute(CancellationToken, ICommandInput, ICommandOutput)"/>
        public Task<DateTime> ExecuteAsync(CancellationToken cancellationToken, ICommandInput input = null, ICommandOutput output = null)
        {
            return Task.Run(() => Execute(cancellationToken, input, output));
        }

        /// <summary>
        /// Executes a PLC command.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which allows to
        /// cancel the running command. The cancel request is sent to the PLC and the
        /// execution will still wait for the PLC to end to command.</param>
        /// <exception cref="InvalidOperationException">The ADS-Client given at construction
        /// time is not connected.</exception>
        protected DateTime Execute(CancellationToken cancellationToken, ICommandInput input = null, ICommandOutput output = null)
        {
            using (PlcCommandLock.AcquireLock(_adsCommandFbPath, _adsConnection.Address, ExecutionBehavior))
            {
                if (!_adsConnection.IsConnected)
                    throw new InvalidOperationException(CommandResources.ERR_NotConnected);

                if (input != null)
                {
                    WriteInputData(input);
                }

                _adsConnection.AdsNotificationEx += OnAdsNotification;
                try
                {
                    SetExecuteFlag();

                    DateTime executionTimestamp;

                    using (var cancellationRegistration = cancellationToken.Register(ResetExecuteFlag))
                    {
                        var handshakeExchange = new DataExchange<CommandChangeData>();

                        Logger.Trace("Before adding device notification of command '{command}'.", _adsCommandFbPath);
                        var cmdHandle = _adsConnection.AddDeviceNotificationEx(
                            $"{_adsCommandFbPath}.stHandshake",
                            AdsTransMode.OnChange,
                            50, // 50 statt 0 als Workaround für ein hängiges ADS-Problem mit Initial-Events
                            0,
                            Tuple.Create(this, handshakeExchange),
                            typeof(CommandHandshakeStruct));
                        Logger.Trace("After adding device notification of command '{command}'.", _adsCommandFbPath);

                        try
                        {
                            Logger.Trace("Start waiting for execution on command '{command}'.", _adsCommandFbPath);
                            executionTimestamp = WaitForExecution(handshakeExchange);
                        }
                        catch (PlcCommandErrorException ex) when (ex.ResultCode == 3 && cancellationToken.IsCancellationRequested)
                        {
                            // Im Falle eines Abbruch durch einen User-Request (CancellationToken) wird
                            // die Framework-Exception zurückgegeben.
                            throw new OperationCanceledException(CommandResources.ERR_CommandCanceled, ex, cancellationToken);
                        }
                        finally
                        {
                            Logger.Trace("End waiting for execution on command '{command}.", _adsCommandFbPath);
                            _adsConnection.DeleteDeviceNotification(cmdHandle);
                        }
                    }

                    if (output != null)
                    {
                        ReadOutputData(output);
                    }

                    return executionTimestamp;
                }
                catch (Exception ex)
                {
                    // Bei Fehlern zur Sicherheit die Ausführung abbrechen
                    try
                    {
                        ResetExecuteFlag();
                    }
                    catch (Exception resetEx)
                    {
                        ex.Data.Add("ResetExecuteFlagException", resetEx);
#pragma warning disable CA2200 // Rethrow to preserve stack details.
                        throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details.
                    }

                    throw;
                }
                finally
                {
                    _adsConnection.AdsNotificationEx -= OnAdsNotification;
                }
            }
        }

        private void ReadOutputData(ICommandOutput output)
        {
            Logger.Trace("Start writing oupt data of command '{command}'.", _adsCommandFbPath);
            _commandArgumentHandler.ReadOutputData(_adsConnection, _adsCommandFbPath, output);
            Logger.Trace("End writing oupt data of command '{command}'.", _adsCommandFbPath);
        }

        private void WriteInputData(ICommandInput input)
        {
            Logger.Trace("Start writing input data of command '{command}'.", _adsCommandFbPath);
            _commandArgumentHandler.WriteInputData(_adsConnection, _adsCommandFbPath, input);
            Logger.Trace("End writing input data of command '{command}'.", _adsCommandFbPath);
        }

        private DateTime WaitForExecution(DataExchange<CommandChangeData> dataExchange)
        {
            var timeoutStopWatch = Stopwatch.StartNew();

            int lastProgress = 0;
            int lastSubTask = 0;
            while (true)
            {
                try
                {
                    var remainingTimeout = Timeout - timeoutStopWatch.Elapsed;
                    Logger.Trace("Waiting for event with Timeout={timeout}s on command '{command}'.", remainingTimeout.Seconds, _adsCommandFbPath);

                    var commandChangeData = dataExchange.GetOrWait(remainingTimeout);
                    var handshakeData = commandChangeData.CommandHandshake;

                    Logger.Trace("New command event Execute={executeFlag}, Busy={busyFlag} ResultCode={resultCode} at {timestamp} on command '{command}'.", handshakeData.Execute, handshakeData.Busy, handshakeData.ResultCode, commandChangeData.Timestamp, _adsCommandFbPath);

                    lastProgress = handshakeData.Progress;
                    lastSubTask = handshakeData.SubTask;

                    StateChanged?.Invoke(this, new PlcCommandEventArgs(lastProgress, lastSubTask, handshakeData.IsCommandFinished, handshakeData.IsCommandCancelled, false));

                    if (handshakeData.IsCommandFinished || handshakeData.IsCommandCancelled)
                    {
                        CheckResultCode(handshakeData.ResultCode);

                        // Ausführungzeitpunkt TC-Zeitstempel des Commands
                        return commandChangeData.Timestamp;
                    }
                }
                catch (TimeoutException ex)
                {
                    // Update state
                    StateChanged?.Invoke(this, new PlcCommandEventArgs(lastProgress, lastSubTask, false, false, true));

                    throw new PlcCommandTimeoutException(_adsCommandFbPath, string.Format(CommandResources.ERR_TimeOut, Timeout.Seconds), ex);
                }
            }
        }

        private void CheckResultCode(ushort resultCode)
        {
            string errorMsg = string.Empty;

            switch (resultCode)
            {
                case (ushort)CommandResultCode.Initialized:
                case (ushort)CommandResultCode.Running:
                case (ushort)CommandResultCode.Done:
                    return;

                default:
                    errorMsg = _commandResource.GetResultCodeString(resultCode);
                    break;
            }

            throw new PlcCommandErrorException(_adsCommandFbPath, resultCode, errorMsg);
        }

        private void OnAdsNotification(object sender, AdsNotificationExEventArgs e)
        {
            var userDataTuple = e.UserData as Tuple<PlcCommand, DataExchange<CommandChangeData>>;
            if (userDataTuple == null || userDataTuple.Item1 != this)
            {
                return;
            }

            var timeStamp = DateTime.FromFileTime(e.TimeStamp);

            userDataTuple.Item2.Set(new CommandChangeData(timeStamp, (CommandHandshakeStruct)e.Value));
        }

        private void SetExecuteFlag()
        {
            try
            {
                Logger.Trace("Before setting execute-Flag on command '{command}'.", _adsCommandFbPath);
                WriteVariable(_adsCommandFbPath + ".stHandshake.bExecute", true);
                Logger.Trace("After setting execute-Flag on command '{command}'.", _adsCommandFbPath);
            }
            catch (Exception ex)
            {
                // Need because of fake connection
                AdsErrorException adsEx = null;
                if (ex is AdsErrorException exIsAds)
                    adsEx = exIsAds;
                if (ex.InnerException is AdsErrorException inexIsAds)
                    adsEx = inexIsAds;

                if (adsEx != null && adsEx.ErrorCode == AdsErrorCode.DeviceSymbolNotFound)
                {
                    throw new PlcCommandException(_adsCommandFbPath, string.Format(CommandResources.ERR_CommandNotFound, _adsCommandFbPath), adsEx);
                }

                throw;
            }
        }

        private void ResetExecuteFlag()
        {
            Logger.Trace("Before resetting execute-Flag on command '{command}'.", _adsCommandFbPath);
            WriteVariable(_adsCommandFbPath + ".stHandshake.bExecute", false);
            Logger.Trace("After resetting execute-Flag on command '{command}'.", _adsCommandFbPath);
        }

        protected void WriteVariable(string symbolName, object value)
        {
            var varHandle = _adsConnection.CreateVariableHandle(symbolName);
            try
            {
                // ToDo: Fix possible mismatch of datatype!!!
                _adsConnection.WriteAny(varHandle, value);
            }
            finally
            {
                _adsConnection.DeleteVariableHandle(varHandle);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        internal struct CommandHandshakeStruct
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool Execute;
            [MarshalAs(UnmanagedType.I1)]
            public bool Busy;
            public ushort ResultCode;
            public byte Progress;
            public ushort SubTask;

            public bool IsCommandFinished => !Execute && !Busy;

            public bool IsCommandCancelled => (!Execute && Busy) || ResultCode == (ushort)CommandResultCode.Cancelled;

            public override string ToString()
                => $"Execute={Execute} Busy={Busy} ResultCode={ResultCode} Progress={Progress} SubTask={SubTask}";
        }

        /// <summary>
        /// Enthält Daten eines einer Änderung der CommandHandshake-Struct.
        /// </summary>
        internal class CommandChangeData
        {
            public CommandChangeData(DateTime timestamp, CommandHandshakeStruct commandHandshake)
            {
                Timestamp = timestamp;
                CommandHandshake = commandHandshake;
            }

            public DateTime Timestamp { get; }

            public CommandHandshakeStruct CommandHandshake { get; }
        }
    }
}
