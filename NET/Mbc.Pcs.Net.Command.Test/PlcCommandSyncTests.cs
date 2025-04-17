using FluentAssertions;
using Mbc.Pcs.Net.Command;
using Mbc.Pcs.Net.Test.Util.Command;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.Systemtest
{
    /// <summary>
    /// If a real PLC is used for system tests, the following is importend!
    /// This system test requires the the test project "Mbc.Tc3.Pcs.Samples".
    /// This TwinCat 3 PLC Project must be activated and loaded into the local Runtime.
    /// All thest should be executed in serial order => [assembly: CollectionBehavior(DisableTestParallelization = true)]
    /// </summary>
    public class PlcCommandSyncTests : MbcPlcCommandTestBase
    {
        private readonly ILogger _logger;

        public PlcCommandSyncTests()
        {
            _logger = NullLogger<PlcCommandSyncTests>.Instance;
        }

        [Fact]
        public void ExecuteWithoutArguments()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake()
            {
                ResponseTimestamp = new DateTime(2000, 1, 2, 3, 4, 5),
            };
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbBaseCommand1", _logger);
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            var executionTimestamp = subject.Execute();

            // Assert
            executionTimestamp.Should().Be(new DateTime(2000, 1, 2, 3, 4, 5));
            stateChanges.Count.Should().Be(1);
            stateChanges[0].IsFinished.Should().Be(true);
            stateChanges[0].IsCancelled.Should().Be(false);
            stateChanges[0].Progress.Should().Be(100);
        }

        [Fact(Skip = "true")]
        public void ExecuteWithArguments()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            var input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 11 },
                { "Val2", 22 },
            });
            var output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Result", null },
            });
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbAddCommand1", _logger);

            // Act
            var ex = Record.Exception(() => subject.Execute(input, output));

            // Assert
            ex.Should().BeNull();
        }

        [Fact(DisplayName = "SystemTestOnly", Skip = "true")]
        [Trait("Category", "SystemTest")]
        public async Task ExecutefbDelayedAddCommand1Async()
        {
            // Arrange
            var fakeConnection = new AdsCommandConnectionFake();
            ICommandInput input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 23.3 },
                { "Val2", 33.3 },
            });
            ICommandOutput output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Result", null },
            });
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1xxx", _logger);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(input, output));

            // Assert
            ex.Should().BeNull();
            output.Should().NotBeNull();
            output.GetOutputData<double>("Result").Should().BeApproximately(56.6, 0.01);
        }
    }
}
