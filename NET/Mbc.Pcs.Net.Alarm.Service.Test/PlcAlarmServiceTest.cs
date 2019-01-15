using Mbc.Pcs.Net.Alarm.Service;
using System;

namespace Mbc.Pcs.Net.Alarm.Test
{
    public class PlcAlarmServiceTest : IDisposable
    {
        private readonly PlcAlarmServiceTestWrapper _testee;

        /// <summary>
        /// All tests uses a faked TC Alarm <see cref="TcEventLogAdsProxyClass"/>
        /// The Global Test Place number is 1
        /// </summary>
        public PlcAlarmServiceTest()
        {
            _testee = new PlcAlarmServiceTestWrapper("foo");
        }

        public void Dispose()
        {
            _testee.Dispose();
        }

        /// <summary>
        /// Used to Handle with the <see cref="PlcAlarmService"/> class
        /// </summary>
        private class PlcAlarmServiceTestWrapper : PlcAlarmService
        {
            public PlcAlarmServiceTestWrapper(string adsNetId)
                : base(adsNetId)
            {
            }

            public void RaiseOnPlcAlarmServiceMediatorStdoutDataReceived(PlcAlarmChangeEventArgs plcAlarmChangeEventArgs)
            {
                OnEventChange(plcAlarmChangeEventArgs);
            }
        }
    }
}
