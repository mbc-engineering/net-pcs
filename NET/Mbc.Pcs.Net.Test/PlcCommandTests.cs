using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.Test.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            IPlcCommand subject = new PlcCommand(null, "fbXyz");

            // Act
            ;

            // Assert
            subject.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void TestExecuteWithoutArguments_WithInternalTypes()
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
            IPlcCommand subject = new PlcCommand(connection, "cmd");

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

        /// <summary>
        /// Example for customer code. see also constructor
        /// </summary>
        [Fact]
        public void ExecuteAsync_WithoutArguments()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "cmd");

            // Act
            Func<Task> act = async () => await subject.ExecuteAsync();

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Example for customer code. see also constructor
        /// </summary>
        [Fact]
        public void ExecuteAsync_WithArguments()
        {
            // Arrange     
            var fakeConnection = new AdsCommandConnectionFake();
            fakeConnection.AddAdsSubItem("Val1", typeof(Int16), true);
            fakeConnection.AddAdsSubItem("Val2", typeof(Int16), true);
            fakeConnection.AddAdsSubItem("Result", typeof(Int16), false);

            var input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 11 },                
                { "Val2", 22 },
            });
            var output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Result", null }
            });
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "cmd");            

            // Act
            Func<Task> act = async () => await subject.ExecuteAsync(input, output);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task ExecuteAsync_ExecutionBehaviorIsLock_ShouldLockingExecutionOrder()
        {
            // Arrange 
            var fakeConnection = new AdsCommandConnectionFake();
            IPlcCommand command1 = new PlcCommand(fakeConnection.AdsConnection, "cmd");
            IPlcCommand command2 = new PlcCommand(fakeConnection.AdsConnection, "cmd");
            IPlcCommand command3 = new PlcCommand(fakeConnection.AdsConnection, "cmd");
            int lastCommand = 0;
            command1.StateChanged += (obj, args) => { lastCommand = 1; };
            command2.StateChanged += (obj, args) => { lastCommand = 2; };
            command3.StateChanged += (obj, args) => { lastCommand = 3; };

            // Act
            await Task.WhenAll(new[] {
                command1.ExecuteAsync(),
                command2.ExecuteAsync(A.Fake<ICommandInput>()),
                command3.ExecuteAsync(output: A.Fake<ICommandOutput>()),
            });

            // Assert
            lastCommand.Should().Be(3);
        }

        [Fact]
        public async Task ExecuteAsync_ExecutionBehaviorIsThrowException_ShouldThrowException()
        {
            // Arrange 
            var fakeConnection = new AdsCommandConnectionFake();
            IPlcCommand command1 = new PlcCommand(fakeConnection.AdsConnection, "cmd")
            {
                ExecutionBehavior = ExecutionBehavior.ThrowException,
            };
            IPlcCommand command2 = new PlcCommand(fakeConnection.AdsConnection, "cmd")
            {
                ExecutionBehavior = ExecutionBehavior.ThrowException,
            };
            IPlcCommand command3 = new PlcCommand(fakeConnection.AdsConnection, "cmd")
            {
                ExecutionBehavior = ExecutionBehavior.ThrowException,
            };
            int lastCommand = 0;
            command1.StateChanged += (obj, args) => { lastCommand = 1; };
            command2.StateChanged += (obj, args) => { lastCommand = 2; };
            command3.StateChanged += (obj, args) => { lastCommand = 3; };

            // Act
            var tasks = new[] {
                Record.ExceptionAsync(() => command1.ExecuteAsync()),
                Record.ExceptionAsync(() => command2.ExecuteAsync(A.Fake<ICommandInput>())),
                Record.ExceptionAsync(() => command3.ExecuteAsync(output: A.Fake<ICommandOutput>())),
            };
            await Task.WhenAll(tasks);

            // Assert
            lastCommand.Should().Be(1);
            tasks[0].Result.Should().BeNull();
            tasks[1].Result.Should().BeOfType<PlcCommandLockException>();
            (tasks[1].Result as PlcCommandLockException).CommandVariable.Should().Be("cmd");
            tasks[2].Result.Should().BeOfType<PlcCommandLockException>();
            (tasks[2].Result as PlcCommandLockException).CommandVariable.Should().Be("cmd");
        }
    }
}