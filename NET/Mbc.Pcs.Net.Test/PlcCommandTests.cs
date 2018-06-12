using FakeItEasy;
using FluentAssertions;
using System;
using TwinCAT.Ads;
using Xunit;

namespace Mbc.Pcs.Net.Test
{
    public class PlcCommandTests
    {
        /// <summary>
        /// A command has a default timeout of 5 seconds
        /// </summary>
        [Fact]
        public void CheckDefaultTimeOut()
        {
            // Arrange            
            var subject = new PlcCommand(null, "fbXyz");

            // Act
            ;

            // Assert
            subject.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void TestExecuteWithoutArguments()
        {
            // Arrange
            var connection = A.Fake<IAdsConnection>();
            A.CallTo(() => connection.IsConnected)
                .Returns(true);
            A.CallTo(() => connection.CreateVariableHandle("cmd.stHandshake.bExecute"))
                .Returns(1);
            A.CallTo(() => connection.AddDeviceNotificationEx("cmd.stHandshake", AdsTransMode.OnChange, 50, 0, A<object>.Ignored, typeof(PlcCommand.CommandHandshakeStruct)))
                .Invokes(call =>
                {
                    var userData = call.Arguments[4];
                    var handshake = new PlcCommand.CommandHandshakeStruct { };
                    var eventArgs = new AdsNotificationExEventArgs(1, userData, 80, handshake);
                    connection.AdsNotificationEx += Raise.FreeForm<AdsNotificationExEventHandler>
                        .With(connection, eventArgs);
                })
                .Returns(80);
            var subject = new PlcCommand(connection, "cmd");

            // Act
            subject.Execute();

            // Assert
            A.CallTo(() => connection.CreateVariableHandle("cmd.stHandshake.bExecute"))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => connection.WriteAny(1, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => connection.DeleteVariableHandle(1))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => connection.DeleteDeviceNotification(80))
                .MustHaveHappenedOnceExactly();
        }
    }
}
