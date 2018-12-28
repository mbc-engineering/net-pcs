using FluentAssertions;
using Mbc.Pcs.Net.Alarm;
using System;
using Xunit;

namespace Mbc.Pcs.Net.Test.Alarm.Test
{
    public class PlcAlarmEventSorterTest
    {
        [Fact]
        public void DefaultSortOrder()
        {
            // Arrange
            var alarm1 = new PlcAlarmEvent { Class = AlarmEventClass.Alarm, Date = DateTime.FromFileTime(10) };
            var alarm2 = new PlcAlarmEvent { Class = AlarmEventClass.Warning, Date = DateTime.FromFileTime(11) };
            var alarm3 = new PlcAlarmEvent { Class = AlarmEventClass.Alarm, Date = DateTime.FromFileTime(9) };
            var alarm4 = new PlcAlarmEvent { Class = AlarmEventClass.Warning, Date = DateTime.FromFileTime(10) };
            var alarmList = new[] { alarm1, alarm2, alarm3, alarm4 };

            // Act
            Array.Sort(alarmList, PlcAlarmEventSorter.DefaultSortOrder);

            // Assert
            alarmList.Should().ContainInOrder(alarm3, alarm1, alarm4, alarm2);
        }
    }
}
