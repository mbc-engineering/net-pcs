using FluentAssertions;
using System;
using Xunit;

namespace Mbc.Pcs.Net.Alarm.Test
{
    public class PlcAlarmEventSorterTest
    {
        [Fact]
        public void DefaultSortOrder()
        {
            // Arrange
            var alarm1 = new PlcAlarmEvent { Class = AlarmEventClass.Alarm, Date = new DateTime(2019, 02, 15, 16, 44, 45) };
            var alarm2 = new PlcAlarmEvent { Class = AlarmEventClass.Warning, Date = new DateTime(2019, 02, 14, 15, 25, 45) };
            var alarm3 = new PlcAlarmEvent { Class = AlarmEventClass.Alarm, Date = new DateTime(2019, 02, 15, 18, 25, 45) };
            var alarm4 = new PlcAlarmEvent { Class = AlarmEventClass.Warning, Date = new DateTime(2019, 02, 15, 15, 26, 45) };
            var alarmList = new[] { alarm1, alarm2, alarm3, alarm4 };

            // Act
            Array.Sort(alarmList, PlcAlarmEventSorter.DefaultSortOrder);

            // Assert
            alarmList.Should().ContainInOrder(alarm3, alarm1, alarm4, alarm2);
        }
    }
}
