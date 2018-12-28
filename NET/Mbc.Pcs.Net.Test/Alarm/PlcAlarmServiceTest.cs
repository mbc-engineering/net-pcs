using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.Alarm;
using System;
using TCEVENTLOGGERLib;
using TcEventLogProxyLib;
using Xunit;

namespace Mbc.Pcs.Net.Test.Alarm.Test
{
    public class PlcAlarmServiceTest : IDisposable
    {
        private readonly TcEventLogAdsProxyClass _tcEventLog;
        private readonly PlcAlarmServiceWrapper _testee;

        /// <summary>
        /// All tests uses a faked TC Alarm <see cref="TcEventLogAdsProxyClass"/>
        /// The Global Test Place number is 1
        /// </summary>
        public PlcAlarmServiceTest()
        {
            _tcEventLog = A.Fake<TcEventLogAdsProxyClass>();

            _testee = new PlcAlarmServiceWrapper("foo", 1, _tcEventLog);
        }

        public void Dispose()
        {
            _testee.Dispose();
        }

        /// <summary>
        /// Tests the filtering of the source id for a new received alarm event on <see cref="TcEventLogAdsProxyClass"/> from PLC
        /// A Source id is arranged as follow gxyy
        /// g => Group (1 = Prüfplatzalarm; 3 = Prüfstandalarm; 5 Prüfgruppenalarm)
        /// x => x
        /// yy => the index of the g type
        /// </summary>
        [Theory]
        [InlineData(1001, true)]
        [InlineData(1002, false)] // Not the same testplace number
        [InlineData(3001, true)]
        [InlineData(3002, false)] // Not the same testplace
        [InlineData(5001, true)]
        [InlineData(5002, false)] // Not the same group number
        [InlineData(0, false)] // not valid group
        [InlineData(2356, false)] // not valid group
        [InlineData(11111, false)] // not valid group
        public void TcEventsShouldBeFilterOnTheSourceId(int srcId, bool shouldRaiseAlarmChanged)
        {
            // Arrange
            var tcEvent = A.Fake<ITcEvent>();
            A.CallTo(() => tcEvent.SrcId).Returns(srcId);

            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                _testee.Connect();   // For correct service state
                _testee.RaiseTcEventLogOnNewEvent(tcEvent);

                // Assert
                if (shouldRaiseAlarmChanged)
                {
                    monitoredTestee.Should().Raise(nameof(PlcAlarmService.AlarmChanged))
                        .WithArgs<PlcAlarmChangeEventArgs>(args => args.ChangeType == PlcAlarmEventChangeType.New);
                }
                else
                {
                    monitoredTestee.Should().NotRaise(nameof(PlcAlarmService.AlarmChanged));
                }
            }
        }

        /// <summary>
        /// Used to Fake the Internal COM Class <see cref="TcEventLogAdsProxyClass"/>
        /// </summary>
        private class PlcAlarmServiceWrapper : PlcAlarmService
        {
            private readonly TcEventLogAdsProxyClass _tcEventLog;

            public PlcAlarmServiceWrapper(string adsNetId, int testPlaceNo, TcEventLogAdsProxyClass tcEventLog)
                : base(adsNetId, testPlaceNo)
            {
                _tcEventLog = tcEventLog;
            }

            protected override TcEventLogAdsProxyClass CreateTcEventLogAdsProxyClass()
            {
                return _tcEventLog;
            }

            public void RaiseTcEventLogOnNewEvent(ITcEvent tcEvent)
            {
                _tcEventLog.OnNewEvent += Raise.FreeForm.With(tcEvent);
            }
        }
    }
}
