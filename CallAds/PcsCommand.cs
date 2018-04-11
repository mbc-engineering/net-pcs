using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace AtomizerUI.link
{
    public class PcsCommand
    {
        private readonly TcAdsClient _adsClient;
        private readonly string _commandVar;

        public PcsCommand(TcAdsClient adsClient, string commandVar)
        {
            _adsClient = adsClient;
            _commandVar = commandVar;
            Timeout = TimeSpan.FromSeconds(8);
        }

        public TimeSpan Timeout { get; set; }

        public Type ResultType { get; set; }

        public Task<object> ExecuteAsync(object argsIn, CancellationToken cancelToken)
        {
            return Task.Run<object>(() => {
                var argsInHndl = _adsClient.CreateVariableHandle(_commandVar + ".stArgsIn");
                try
                {
                    _adsClient.WriteAny(argsInHndl, argsIn);
                }
                finally
                {
                    _adsClient.DeleteVariableHandle(argsInHndl);
                }

                return Execute(cancelToken);
            });
        }

        public object Execute(object argsIn)
        {
            var argsInHndl = _adsClient.CreateVariableHandle(_commandVar + ".stArgsIn");
            try
            {
                _adsClient.WriteAny(argsInHndl, argsIn);
            }
            finally
            {
                _adsClient.DeleteVariableHandle(argsInHndl);
            }            

            return Execute();
        }

        public object Execute()
        {
            return Execute(CancellationToken.None);
        }

        public object Execute(CancellationToken cancelToken)
        {
            _adsClient.AdsNotificationEx += OnAdsnotificationEx;
            try
            {
                SetExecuteFlag();

                var dataExch = new DataExchange();
                var cmdHandle = _adsClient.AddDeviceNotificationEx(_commandVar + ".stCmd", AdsTransMode.OnChange,
                    TimeSpan.Zero, TimeSpan.Zero, dataExch, typeof(CommandControlData));

                cancelToken.Register(ResetExecuteFlag);
                try
                {
                    var watch = Stopwatch.StartNew();
                    while (true)
                    {
                        try
                        {
                            var controlData =
                                (CommandControlData)
                                    dataExch.GetOrWait(Timeout - TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds));

                            if (!controlData.Execute && !controlData.Busy)
                            {
                                CheckResultCode(controlData.ResultCode);
                                break;
                            }
                        }
                        catch (TimeoutException)
                        {
                            // Workaround für ein ADS-Problem, falls kein Event empfangen wurde
                            var cmdHndl = _adsClient.CreateVariableHandle(_commandVar + ".stCmd");
                            try
                            {
                                var ccd = (CommandControlData) _adsClient.ReadAny(cmdHndl, typeof (CommandControlData));
                                if (!ccd.Execute && !ccd.Busy)
                                {
                                    break;
                                }
                            }
                            finally
                            {
                                _adsClient.DeleteVariableHandle(cmdHndl);
                            }                                    

                            throw new PcsCommandTimeoutException();
                        }
                    }
                }
                finally
                {
                    _adsClient.DeleteDeviceNotification(cmdHandle);
                }
            } 
            finally
            {
                _adsClient.AdsNotificationEx -= OnAdsnotificationEx;
            }

            if (ResultType == null)
                return null;

            var argsOutHndl = _adsClient.CreateVariableHandle(_commandVar + ".stArgsOut");
            try
            {
                return _adsClient.ReadAny(argsOutHndl, ResultType);
            }
            finally
            {
                _adsClient.DeleteVariableHandle(argsOutHndl);
            }
        }

        private void SetExecuteFlag()
        {
            var executeHndl = _adsClient.CreateVariableHandle(_commandVar + ".stCmd.bExecute");
            try
            {
                _adsClient.WriteAny(executeHndl, true);
            }
            finally
            {
                _adsClient.DeleteVariableHandle(executeHndl);
            }
        }

        private void ResetExecuteFlag()
        {
            var executeHndl = _adsClient.CreateVariableHandle(_commandVar + ".stCmd.bExecute");
            try
            {
                _adsClient.WriteAny(executeHndl, false);
            }
            finally
            {
                _adsClient.DeleteVariableHandle(executeHndl);
            }
        }

        void OnAdsnotificationEx(object sender, AdsNotificationExEventArgs e)
        {
            if (e.UserData is DataExchange)
            {
                ((DataExchange) e.UserData).Set(e.Value);
            }
        }

        void CheckResultCode(ushort resultCode)
        {
            if (resultCode == 0)
                return;

            string errorMsg;

            switch (resultCode)
            {
                case 1:
                    errorMsg = "Invalid state.";
                    break;
                case 2:
                    errorMsg = "Invalid argument.";
                    break;
                case 3:
                    errorMsg = "Not ready.";
                    break;
                case 4:
                    errorMsg = "Running.";
                    break;
                case 5:
                    errorMsg = "Cancelled.";
                    break;
                case 6:
                    errorMsg = "Internal error.";
                    break;
                default:
                    errorMsg = string.Format("Unknown error: {0}", resultCode);
                    break;
            }

            throw new PcsCommandErrorException(resultCode, errorMsg);
        }
    }
}
