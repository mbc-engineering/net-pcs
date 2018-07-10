using FluentAssertions;
using Mbc.Pcs.Net.Command;
using Mbc.Pcs.Net.Test.Util.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;
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
        [Fact]
        public void Execute_WithoutArguments()
        {
            // Arrange            
            var fakeConnection = new AdsCommandConnectionFake();
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbBaseCommand1");
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            var ex = Record.Exception(() => subject.Execute());

            // Assert
            ex.Should().BeNull();
            stateChanges.Count.Should().Be(1);
            stateChanges[0].IsFinished.Should().Be(true);
            stateChanges[0].IsCancelled.Should().Be(false);
            stateChanges[0].Progress.Should().Be(100);
        }
        
        [Fact(Skip = "true")]        
        public void Execute_WithArguments()
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
                { "Result", null }
            });
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbAddCommand1");


            // Act
            var ex = Record.Exception(() => subject.Execute(input, output));

            // Assert
            ex.Should().BeNull();
        }

        [Fact(DisplayName="SystemTestOnly", Skip = "true")]
        [Trait("Category", "SystemTest")]
        public async Task Execute_fbDelayedAddCommand1_Async()
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
                { "Result", null }
            });
            var subject = new PlcCommand(fakeConnection.AdsConnection, "Commands.fbDelayedAddCommand1xxx");

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(input, output));

            // Assert
            ex.Should().BeNull();
            output.Should().NotBeNull();
            output.GetOutputData<double>("Result").Should().BeApproximately(56.6, 0.01);
        }
    }
}
