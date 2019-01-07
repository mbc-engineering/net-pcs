using FluentAssertions;
using Mbc.Pcs.Net.Alarm.Service;
using System;
using Xunit;

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
            _testee = new PlcAlarmServiceTestWrapper("foo", 1);
        }

        public void Dispose()
        {
            _testee.Dispose();
        }

        /// <summary>
        /// Tests the filtering of the source id for a new received alarm event from PLC
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
            var eventArg = new PlcAlarmChangeEventArgs()
            {
                AlarmEvent = new PlcAlarmEvent()
                {
                    SrcId = srcId,
                },
            };

            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                _testee.RaiseOnPlcAlarmServiceMediatorStdoutDataReceived(eventArg);

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
        /// Used to Handle with the <see cref="PlcAlarmService"/> class
        /// </summary>
        private class PlcAlarmServiceTestWrapper : PlcAlarmService
        {
            public PlcAlarmServiceTestWrapper(string adsNetId, int testPlaceNo)
                : base(adsNetId, testPlaceNo)
            {
            }

            public void RaiseOnPlcAlarmServiceMediatorStdoutDataReceived(PlcAlarmChangeEventArgs plcAlarmChangeEventArgs)
            {
                OnEventChange(plcAlarmChangeEventArgs);
            }
        }
    }
}
