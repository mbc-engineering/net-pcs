using FluentAssertions;
using Xunit;

namespace Mbc.Pcs.Net.Alarm.Test
{
    public class UserFlagsPlcAlarmEventFormatterTest
    {
        [Theory]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] () ()", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] ()", "", "")]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] ()", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", "")]
        [InlineData("Controller wird vorbereitet (1234ABC123) (1234ABC123)", "Controller wird vorbereitet (1234ABC123)", "1234ABC123", "1234ABC123")]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (AAAABBBB) (1234ABC123)", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (AAAABBBB)", "AAAABBBB", "1234ABC123")]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (1234ABC123)", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", "1234ABC123")]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", "")]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (AAAABBBB) (1234ABC123)", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (AAAABBBB)", null, null)]
        [InlineData("Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (AAAABBBB) (1234ABC123)", "Sicherung 400V Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (AAAABBBB)")]
        public void RemoveTheArgumentFromMessageIfFlaged(string message, string expecedMessage, params object[] arguments)
        {
            // Arrange
            var alarmEvent = new PlcAlarmEvent()
            {
                Message = message,
                UserData = 0xFFFF,
                ArgumentData = arguments,
            };
            var testee = new UserFlagsPlcAlarmEventFormatter();

            // Act
            var formatedAlarmEvent = testee.Format(alarmEvent);

            // Assert
            formatedAlarmEvent.Should().NotBeNull();
            formatedAlarmEvent.Message.Should().Be(expecedMessage);
            formatedAlarmEvent.Should().BeEquivalentTo(alarmEvent, cfg => cfg.Excluding(x => x.Message));
        }

        [Theory]
        [InlineData("Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (abc) ()", "Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (abc)")]
        [InlineData("Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] ()", "Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]")]
        public void RemoveTheArgumentFromMessageIfFlagedAndWithoutArgument(string message, string expecedMessage)
        {
            // Arrange
            var alarmEvent = new PlcAlarmEvent()
            {
                Message = message,
                UserData = 0xFFFF,
            };
            var testee = new UserFlagsPlcAlarmEventFormatter();

            // Act
            var formatedAlarmEvent = testee.Format(alarmEvent);

            // Assert
            formatedAlarmEvent.Should().NotBeNull();
            formatedAlarmEvent.Message.Should().Be(expecedMessage);
            formatedAlarmEvent.Should().BeEquivalentTo(alarmEvent, cfg => cfg.Excluding(x => x.Message));
        }

        [Fact]
        public void DoNotFormatWithoutFlag()
        {
            // Arrange
            string message = "Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (abc) ()";
            var alarmEvent = new PlcAlarmEvent()
            {
                Message = message,
                UserData = 0,
            };
            var testee = new UserFlagsPlcAlarmEventFormatter();

            // Act
            var formatedAlarmEvent = testee.Format(alarmEvent);

            // Assert
            formatedAlarmEvent.Should().NotBeNull();
            formatedAlarmEvent.Message.Should().Be(message);
            formatedAlarmEvent.Should().BeEquivalentTo(alarmEvent);
        }

        [Theory]
        [InlineData("Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1] (S123456)", "Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", "S123456")]
        [InlineData("Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", "Sicherung (400V) Niederdruck CSC, Cool-X und OLA [==PEE0=PRG1S++PRS1+PRP1-26F1/-36F1/-60F1]", 12.2)]
        public void RemoveTheArgumentFromMessageIfFlagedAndCascaded(string message, string expecedMessage, params object[] arguments)
        {
            // Arrange
            var alarmEvent = new PlcAlarmEvent()
            {
                Message = message,
                UserData = 0xFFFF,
                ArgumentData = arguments,
            };
            var testee = new UserFlagsPlcAlarmEventFormatter(new UserFlagsPlcAlarmEventFormatter(new UserFlagsPlcAlarmEventFormatter()));

            // Act
            var formatedAlarmEvent = testee.Format(alarmEvent);

            // Assert
            formatedAlarmEvent.Should().NotBeNull();
            formatedAlarmEvent.Message.Should().Be(expecedMessage);
            formatedAlarmEvent.Should().BeEquivalentTo(alarmEvent, cfg => cfg.Excluding(x => x.Message));
        }
    }
}
