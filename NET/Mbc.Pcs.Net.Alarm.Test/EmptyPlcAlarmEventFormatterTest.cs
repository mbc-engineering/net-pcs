using FluentAssertions;
using Xunit;

namespace Mbc.Pcs.Net.Alarm.Test
{
    public class EmptyPlcAlarmEventFormatterTest
    {
        [Fact]
        public void DoNotFormat()
        {
            // Arrange
            string message = "Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (abc) ()";
            var alarmEvent = new PlcAlarmEvent()
            {
                Message = message,
                UserData = 0,
            };
            var testee = new EmptyPlcAlarmEventFormatter(new EmptyPlcAlarmEventFormatter());

            // Act
            var formatedAlarmEvent = testee.Format(alarmEvent);

            // Assert
            formatedAlarmEvent.Should().NotBeNull();
            formatedAlarmEvent.Message.Should().Be(message);
            formatedAlarmEvent.Should().BeEquivalentTo(alarmEvent);
        }
    }
}
