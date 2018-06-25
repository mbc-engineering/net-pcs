using FluentAssertions;
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
    /// This system test requires the the test project "Mbc.Tc3.Pcs.Samples". 
    /// This TwinCat 3 PLC Project must be activated and loaded into the local Runtime.
    /// All thest should be executed in serial order => [assembly: CollectionBehavior(DisableTestParallelization = true)]
    /// </summary>
    public class PlcCommandSystemTests
    {
        private readonly string _plcAmsNetId = "172.16.23.76.1.1";
        private readonly int _plcAmsPort = 851;
        private readonly TcAdsClient _adsClient;

        public PlcCommandSystemTests()
        {
            _adsClient = new TcAdsClient();
            _adsClient.Connect(_plcAmsNetId, _plcAmsPort);
        }

        [Fact]
        [Trait("Category", "SystemTest")]
        public void Execute_fbBaseCommand1()
        {
            // Arrange            
            var subject = new PlcCommand(_adsClient, "Commands.fbBaseCommand1");
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

        [Fact]
        [Trait("Category", "SystemTest")]
        public async Task Execute_fbBaseCommand1_Async()
        {
            // Arrange
            var subject = new PlcCommand(_adsClient, "Commands.fbBaseCommand1");
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync());

            // Assert
            ex.Should().BeNull();
            stateChanges.Count.Should().Be(1);
            stateChanges[0].IsFinished.Should().Be(true);
            stateChanges[0].IsCancelled.Should().Be(false);
            stateChanges[0].Progress.Should().Be(100);
        }

        private void Subject_StateChanged(object sender, PlcCommandEventArgs e)
        {
            throw new NotImplementedException();
        }

        [Fact]
        [Trait("Category", "SystemTest")]
        public void Execute_fbAddCommand1()
        {
            // Arrange            
            var input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 11 },
                { "Val2", 22 },
            });
            var output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Result", null }
            });
            var subject = new PlcCommand(_adsClient, "Commands.fbAddCommand1");


            // Act
            var ex = Record.Exception(() => subject.Execute(input, output));

            // Assert
            ex.Should().BeNull();
        }

        [Fact]
        [Trait("Category", "SystemTest")]
        public async Task Execute_fbDelayedAddCommand1_Async()
        {
            // Arrange
            ICommandInput input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 23.3 },
                { "Val2", 33.3 },
            });
            ICommandOutput output = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Result", null }
            });
            var subject = new PlcCommand(_adsClient, "Commands.fbDelayedAddCommand1");

            // Act
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(input, output));

            // Assert
            ex.Should().BeNull();
            output.Should().NotBeNull();
            output.GetOutputData<double>("Result").Should().BeApproximately(56.6, 0.01);
        }
        
        /// <summary>
        /// Long runing commands can be canceled by a Cancel Token. 
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "SystemTest")]
        public async Task ExecuteAndCancel_fbDelayedAddCommand1_Async()
        {
            // Arrange
            ICommandInput input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 23.3 },
                { "Val2", 33.3 },
            });
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            var subject = new PlcCommand(_adsClient, "Commands.fbDelayedAddCommand1");
            var stateChanges = new List<PlcCommandEventArgs>();
            subject.StateChanged += (sender, arg) => stateChanges.Add(arg);

            // Act
            cancellationToken.CancelAfter(TimeSpan.FromMilliseconds(200));
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(cancellationToken.Token, input));

            // Assert
            ex.Should().BeOfType<OperationCanceledException>();
            ex.InnerException.Should().BeOfType<PlcCommandErrorException>()
                .Subject.ResultCode.Should().Be(3);
            stateChanges.Last().Should().NotBeNull();
            stateChanges.Last().IsFinished.Should().Be(false);
            stateChanges.Last().IsCancelled.Should().Be(true);
        }

        /// <summary>
        /// Long runing commands have a time out. 
        /// The duration to execute the command Commands.fbDelayedAddCommand1 should be around 4seconds (PLC Definition => DelayedAddCommand(tDelayTime := T#4S))
        /// </summary>
        /// <returns></returns>
        [Theory]
        [InlineData(200)]
        [InlineData(3900)]
        [Trait("Category", "SystemTest")]
        public async Task ExecuteAndWaitForTimeOut_fbDelayedAddCommand1_Async(double timeout)
        {
            // Arrange
            ICommandInput input = CommandInputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "Val1", 23.3 },
                { "Val2", 33.3 },
            });
            var subject = new PlcCommand(_adsClient, "Commands.fbDelayedAddCommand1");
            subject.Timeout = TimeSpan.FromMilliseconds(timeout);
            PlcCommandEventArgs stateChange = null;
            subject.StateChanged += (sender, arg) => stateChange = arg;

            // Act
            Stopwatch stopWatch = Stopwatch.StartNew();
            var ex = await Record.ExceptionAsync(() => subject.ExecuteAsync(input));
            stopWatch.Stop();

            // Assert
            ex.Should().BeOfType<PlcCommandTimeoutException>()
                .Subject.CommandVariable.Should().Be("Commands.fbDelayedAddCommand1");

            // ToDo: Fix cancel event
            // stateChange.Should().NotBeNull();
            // stateChange.IsFinished.Should().Be(false);
            // stateChange.IsCancelled.Should().Be(true);

            stopWatch.Elapsed.Should().BeCloseTo(TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(250));
        }
    }
}
