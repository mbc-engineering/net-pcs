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
    public class PcsCommand2<Tout>
    {
        private readonly TcAdsClient _adsClient;
        private readonly string _commandVar;

        public PcsCommand2(TcAdsClient adsClient, string commandVar)
        {
            _adsClient = adsClient;
            _commandVar = commandVar;
            Timeout = TimeSpan.FromSeconds(8);
        }

        public TimeSpan Timeout { get; set; }

        //public Task<object> ExecuteAsync<Tin>(Tin argsIn, CancellationToken cancelToken)
        //{
        //    return Task.Run<object>(() => {
        //        var argsInHndl = _adsClient.CreateVariableHandle(_commandVar + ".stArgsIn");
        //        try
        //        {
        //            _adsClient.WriteAny(argsInHndl, argsIn);
        //        }
        //        finally
        //        {
        //            _adsClient.DeleteVariableHandle(argsInHndl);
        //        }

        //        return Execute(cancelToken);
        //    });
        //}

        public Tout Execute<Tin>(Tin argsIn)
        {
            // Write all input param
            foreach (var prop in typeof(Tin).GetProperties())
            {
                WriteVariable($"{_commandVar}.{prop.Name}", prop.GetValue(argsIn));
            }                       

            return Execute();
        }

        private void WriteVariable(string varName, object value)
        {
            
            var argsInHndl = _adsClient.CreateVariableHandle(varName);
            try
            {
                // ToDo: Fix possible mismatch of datatype!!!
                _adsClient.WriteAny(argsInHndl, value);
            }
            finally
            {
                _adsClient.DeleteVariableHandle(argsInHndl);
            }
        }

        public Tout Execute()
        {
            return Execute(CancellationToken.None);
        }

        public Tout Execute(CancellationToken cancelToken)
        {
            _adsClient.AdsNotificationEx += OnAdsnotificationEx;
            try
            {
                SetExecuteFlag();

                // ToDo: Use Symbolic
                Console.WriteLine("Command handling doesent work yet, waiting vor error");
                var dataExch = new DataExchange();
                var cmdHandle = _adsClient.AddDeviceNotificationEx(_commandVar, AdsTransMode.OnChange,
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
            
            var argsOutHndl = _adsClient.CreateVariableHandle(_commandVar + ".stArgsOut");
            try
            {
                return (Tout)_adsClient.ReadAny(argsOutHndl, typeof(Tout));
            }
            finally
            {
                _adsClient.DeleteVariableHandle(argsOutHndl);
            }
        }

        private void SetExecuteFlag()
        {
            WriteVariable(_commandVar + ".Execute", true);            
        }

        private void ResetExecuteFlag()
        {
            WriteVariable(_commandVar + ".Execute", false);
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
