using AwesomeAssertions;
using Mbc.Pcs.Net.AsyncUtils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.AsyncUtils
{
    public class MonitoredExecutionTest
    {
        [Fact]
        public void NoExecutionTest()
        {
            // Arrange
            var monitor = new MonitoredExecution();

            // Act
            var success = monitor.DisableAndWait(TimeSpan.FromMilliseconds(10));

            // Assert
            success.Should().BeTrue();
        }

        [Fact]
        public void WithExecutionTest()
        {
            // Arrange
            var monitor = new MonitoredExecution();
            var inExecution = new ManualResetEventSlim();
            var finishExecution = new ManualResetEventSlim();
            var executionCompleted = new ManualResetEventSlim();

            // Act
            Task.Run(() =>
            {
                monitor.Execute(() =>
                {
                    inExecution.Set();
                    finishExecution.Wait(TestContext.Current.CancellationToken);
                });
                executionCompleted.Set();
            }, TestContext.Current.CancellationToken);
            inExecution.Wait(TestContext.Current.CancellationToken);
            var success1 = monitor.DisableAndWait(TimeSpan.FromMilliseconds(50));
            finishExecution.Set();
            // Wait for the execution to fully complete before testing DisableAndWait
            executionCompleted.Wait(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            var success2 = monitor.DisableAndWait(TimeSpan.FromMilliseconds(50));

            // Assert
            success1.Should().BeFalse();
            success2.Should().BeTrue();
        }
    }
}
