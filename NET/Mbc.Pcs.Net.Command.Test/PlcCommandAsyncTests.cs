using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.Command;
using Mbc.Pcs.Net.Test.Util.Command;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;
using Xunit;

namespace Mbc.Pcs.Net.Test
{
    /// <summary>
    /// If a real PLC is used for system tests, the following is importend!
    /// This system test requires the the test project "Mbc.Tc3.Pcs.Samples".
    /// This TwinCat 3 PLC Project must be activated and loaded into the local Runtime.
    /// </summary>
    public class PlcCommandAsyncTests : MbcPlcCommandTestBase
    {
        private readonly ILogger _logger;

        public PlcCommandAsyncTests()
        {
            _logger = NullLogger<PlcCommandAsyncTests>.Instance;
        }

        /// <summary>
        /// A command has a default timeout of 5 seconds
        /// </summary>
        [Fact]
        public void CheckDefaultTimeOut()
        {
            // Arrange
            IPlcCommand subject = new PlcCommand(null, "fbXyz", _logger);

            // Act
            // nothing

            // Assert
            subject.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void TestExecuteWithoutArgumentsWithInternalTypes()
        {
            // Arrange
            var connection = A.Fake<IAdsConnection>();
            A.CallTo(() => connection.IsConnected)
                .Returns(true);
            A.CallTo(() => connection.CreateVariableHandle("cmd.stHandshake.bExecute"))
                .Returns(1u);
            A.CallTo(() => connection.AddDeviceNotificationEx("cmd.stHandshake", A<NotificationSettings>.Ignored, A<object>.Ignored, typeof(PlcCommand.CommandHandshakeStruct)))
                .Invokes(call =>
                {
                    var userData = call.Arguments[2];
                    var handshake = new PlcCommand.CommandHandshakeStruct { };
                    var notification = new Notification(80, new DateTimeOffset(0, TimeSpan.Zero), userData, null);
                    var eventArgs = new AdsNotificationExEventArgs(notification, handshake);
                    connection.AdsNotificationEx += Raise.FreeForm<EventHandler<AdsNotificationExEventArgs>>
                        .With(connection, eventArgs);
                })
                .Returns(80u);
            IPlcCommand subject = new PlcCommand(connection, "cmd", _logger);

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
        public void ExecuteAsyncWithoutArguments()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbBaseCommand1", _logger);
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            Func<Task> act = async () => await subject.ExecuteAsync();

            // Assert
            act.Should().NotThrow();
            stateChanges.Count.Should().Be(1);
            stateChanges[0].IsFinished.Should().Be(true);
            stateChanges[0].IsCancelled.Should().Be(false);
            stateChanges[0].Progress.Should().Be(100);
        }

        /// <summary>
        /// Example for customer code. see also constructor
        /// </summary>
        [Fact(Skip = "Not fully implemented - see AdsCommandConnectionFake create variable handle sum")]
        public void ExecuteAsyncWithArguments()
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
                { "Result", null },
            });
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbAddCommand1", _logger);

            // Act
            Func<Task> act = async () => await subject.ExecuteAsync(input, output);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public async Task ExecuteAsyncNiceErrorForMissingCommandWithoutParameter()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.ResponseFbPathNotExist);
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbMissingCommand", _logger);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync());

            // Assert
            ex.Should().BeOfType<PlcCommandException>();
            (ex as PlcCommandException).Message.Should().Be(string.Format(CommandResources.ERR_CommandNotFound, "Commands.fbMissingCommand"));
            (ex as PlcCommandException).CommandVariable.Should().Be("Commands.fbMissingCommand");
        }

        [Fact]
        public async Task ExecuteAsyncNiceErrorForMissingCommandWithParameter()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.ResponseFbPathNotExist);
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbMissingCommand", _logger);
            var input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 11 },
            });

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(input));

            // Assert
            ex.Should().BeOfType<PlcCommandException>();
            (ex as PlcCommandException).Message.Should().Be(string.Format(CommandResources.ERR_CommandNotFound, "Commands.fbMissingCommand"));
            (ex as PlcCommandException).CommandVariable.Should().Be("Commands.fbMissingCommand");
        }

        [Fact]
        public async Task ExecuteAsyncNiceErrorForMissingInputVarialbe()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            var input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 11 },
                { "Val2", 22 },
            });
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbBaseCommand1", _logger);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(input));

            // Assert
            ex.Should().BeOfType<PlcCommandException>();
            (ex as PlcCommandException).Message.Should().Be(string.Format(CommandResources.ERR_InputVariablesMissing, "Val1,Val2"));
            (ex as PlcCommandException).CommandVariable.Should().Be("Commands.fbBaseCommand1");
        }

        [Fact]
        public async Task ExecuteAsyncNiceErrorForMissingOutputVarialbe()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            var output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Result", null },
            });
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbBaseCommand1", _logger);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(output: output));

            // Assert
            ex.Should().BeOfType<PlcCommandException>();
            (ex as PlcCommandException).Message.Should().Be(string.Format(CommandResources.ERR_OutputVariablesMissing, "Result"));
            (ex as PlcCommandException).CommandVariable.Should().Be("Commands.fbBaseCommand1");
        }

        /// <summary>
        /// Example for customer code. see also constructor
        /// </summary>
        [Fact]
        public async Task ExecuteAsyncWaitForTimeOut()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.NoResponse);
            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger)
            {
                Timeout = TimeSpan.FromMilliseconds(100),
            };
            PlcCommandEventArgs stateChange = null;
            subject.StateChanged += (sender, arg) => stateChange = arg;

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync());

            // Assert
            ex.Should().BeOfType<PlcCommandTimeoutException>()
                .Subject.CommandVariable.Should().Be("Commands.fbDelayedAddCommand1");
            stateChange.Should().NotBeNull();
            stateChange.IsFinished.Should().Be(false);
            stateChange.IsCancelled.Should().Be(false);
            stateChange.IsTimeOut.Should().Be(true);
        }

        /// <summary>
        /// Long runing commands can be canceled by a Cancel Token.
        /// </summary>
        [Fact(Skip = "Not fully implemented - see AdsCommandConnectionFake create variable handle sum")]
        [Trait("Category", "SimulationTest")]
        public async Task ExecuteAsyncCancelByDotNet()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.NoResponse);
            fakeConnection.AddAdsSubItem("Val1", typeof(short), true);
            fakeConnection.AddAdsSubItem("Val2", typeof(short), true);
            ICommandInput input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 23.3 },
                { "Val2", 33.3 },
            });
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger);
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            cancellationToken.CancelAfter(TimeSpan.FromMilliseconds(100));
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(cancellationToken.Token, input));

            // Assert
            ex.Should().BeOfType<OperationCanceledException>();
            ex.InnerException.Should().BeOfType<PlcCommandErrorException>();
            (ex.InnerException as PlcCommandErrorException).ResultCode.Should().Be(3);
            (ex.InnerException as PlcCommandErrorException).CommandVariable.Should().Be("Commands.fbDelayedAddCommand1");
            stateChanges.Last().Should().NotBeNull();
            stateChanges.Last().IsFinished.Should().Be(false);
            stateChanges.Last().IsTimeOut.Should().Be(false);
            stateChanges.Last().IsCancelled.Should().Be(true);
        }

        /// <summary>
        /// commands can be canceled by the PLC
        /// </summary>
        [Fact]
        [Trait("Category", "SimulationTest")]
        public async Task ExecuteAsyncCancelByPlc()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.ResponseDelayedCancel);
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger);
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync());

            // Assert
            ex.Should().BeOfType<PlcCommandErrorException>();
            (ex as PlcCommandErrorException).ResultCode.Should().Be(3);
            (ex as PlcCommandErrorException).CommandVariable.Should().Be("Commands.fbDelayedAddCommand1");
            stateChanges.Last().Should().NotBeNull();
            stateChanges.Last().IsFinished.Should().Be(false);
            stateChanges.Last().IsTimeOut.Should().Be(false);
            stateChanges.Last().IsCancelled.Should().Be(true);
        }

        /// <summary>
        /// Example for customer status code. see also constructor
        /// </summary>
        [Fact]
        public async Task ExecuteAsyncFailWithCustomFailStatusCode()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.ResponseImmediatelyFinished);
            fakeConnection.ResponseStatusCode = 101;

            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbCustomStateCommand1", _logger);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync());

            // Assert
            ex.Should().BeOfType<PlcCommandErrorException>();
            (ex as PlcCommandErrorException).ResultCode.Should().Be(101);
            (ex as PlcCommandErrorException).Message.Should().Be(string.Format(CommandResources.ERR_ResultCode, 101));
            (ex as PlcCommandErrorException).CommandVariable.Should().Be("Commands.fbCustomStateCommand1");
        }

        /// <summary>
        /// Example for customer status code. see also constructor
        /// </summary>
        [Fact]
        public async Task ExecuteAsyncFailWithCustomFailStatusCodeCustomText()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.ResponseImmediatelyFinished);
            fakeConnection.ResponseStatusCode = 101;
            string resultCode101Text = "Test Status code message.";
            CommandResource subjectResources = new CommandResource();
            subjectResources.AddCustomResultCodeText(101, resultCode101Text);

            IPlcCommand subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbCustomStateCommand1", _logger, subjectResources);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync());

            // Assert
            ex.Should().BeOfType<PlcCommandErrorException>();
            (ex as PlcCommandErrorException).ResultCode.Should().Be(101);
            (ex as PlcCommandErrorException).Message.Should().Be(resultCode101Text);
            (ex as PlcCommandErrorException).CommandVariable.Should().Be("Commands.fbCustomStateCommand1");
        }

        [Fact]
        public async Task ExecuteAsyncExecutionBehaviorIsLockShouldLockingExecutionOrder()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            IPlcCommand command1 = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger);
            IPlcCommand command2 = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger);
            int lastCommand = 0;
            command1.StateChanged += (obj, args) => { lastCommand = 1; };
            command2.StateChanged += (obj, args) => { lastCommand = 2; };

            // Act
            Task[] tasks = new Task[2];
            tasks[0] = command1.ExecuteAsync(A.Fake<ICommandInput>());
            tasks[1] = command2.ExecuteAsync(A.Fake<ICommandInput>());
            await Task.WhenAll(tasks);

            // Assert
            lastCommand.Should().BeCloseTo(2, 1);
        }

        [Fact]
        public async Task ExecuteAsyncExecutionBehaviorIsThrowExceptionShouldThrowException()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake(PlcCommandFakeOption.ResponseDelayedFinished);
            IPlcCommand command1 = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger)
            {
                ExecutionBehavior = ExecutionBehavior.ThrowException,
            };
            IPlcCommand command2 = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger)
            {
                ExecutionBehavior = ExecutionBehavior.ThrowException,
            };
            IPlcCommand command3 = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1", _logger)
            {
                ExecutionBehavior = ExecutionBehavior.ThrowException,
            };
            int lastCommand = 0;
            command1.StateChanged += (obj, args) => { lastCommand = 1; };
            command2.StateChanged += (obj, args) => { lastCommand = 2; };
            command3.StateChanged += (obj, args) => { lastCommand = 3; };

            // Act
            var tasks = new[]
            {
                Record.ExceptionAsync(() => command1.ExecuteAsync()),
                Record.ExceptionAsync(() => command2.ExecuteAsync(A.Fake<ICommandInput>())),
                Record.ExceptionAsync(() => command3.ExecuteAsync(output: A.Fake<ICommandOutput>())),
            };

            await Task.WhenAll(tasks);

            // Assert (The Threading order is not 100% repeatable, so assert depend on lastCommand)
            lastCommand.Should().BeGreaterThan(0).And.BeLessOrEqualTo(3);
            tasks[lastCommand - 1].Result.Should().BeNull();
            for (int idx = 0; idx < tasks.Length; idx++)
            {
                if (tasks[idx].Result != null)
                {
                    tasks[idx].Result.Should().BeOfType<PlcCommandLockException>($"{idx} is not the last command {lastCommand - 1}");
                    (tasks[idx].Result as PlcCommandLockException).CommandVariable.Should().Be("Commands.fbDelayedAddCommand1");
                    (tasks[idx].Result as PlcCommandLockException).Behavior.Should().Be(ExecutionBehavior.ThrowException);
                }
            }
        }

        [Fact]
        public void RequireInitialNotifictaionOnAddDeviceNotificationExWithOnChange()
        {
            // Arrange
            var connection = A.Fake<IAdsConnection>();
            A.CallTo(() => connection.IsConnected)
                .Returns(true);
            A.CallTo(() => connection.CreateVariableHandle("cmd.stHandshake.bExecute"))
                .Returns(1u);
            A.CallTo(() => connection.AddDeviceNotificationEx("cmd.stHandshake", A<NotificationSettings>.Ignored, A<object>.Ignored, typeof(PlcCommand.CommandHandshakeStruct)))
                .Invokes(call =>
                {
                    // No Initial ivent is send
                    // connection.AdsNotificationEx += Raise.FreeForm<AdsNotificationExEventHandler>.With(connection, eventArgs);
                })
                .Returns(80u);
            PlcCommand subject = new PlcCommand(connection, "cmd", _logger);
            subject.Configuration.UseCyclicNotifications = false;
            subject.Configuration.MaxRetriesForInitialEvent = 3;

            // Act
            var ex = Record.Exception(() => subject.Execute());
            // throw new PlcCommandException(_adsCommandFbPath, $"Failed to register device notification {registerRepeatCount} times.");
            // Assert
            ex.Should().NotBeNull();
            ex.Should().BeOfType<PlcCommandException>()
                .Subject.Message.Should().Be("Failed to register device notification 3 times.");
            A.CallTo(() => connection.CreateVariableHandle("cmd.stHandshake.bExecute"))
                .MustHaveHappenedTwiceExactly(); // Because of fail the execute will be reseted
            A.CallTo(() => connection.WriteAny(1, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => connection.DeleteVariableHandle(1)) // Handle one is cmd.stHandshake.bExecute
                .MustHaveHappenedTwiceExactly(); // Because of fail the execute will be reseted
            A.CallTo(() => connection.DeleteDeviceNotification(80))
                .MustHaveHappened(3, Times.Exactly);
        }
    }
}
