using FakeItEasy;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test
{
    public class PlcCommandExtensionsTest
    {
        [Fact]
        public void LockedExecuteAsync_ShouldLocking()
        {
            // Arrange                        
            IPlcCommand command1 = A.Fake<IPlcCommand>();
            DateTime command1ExecutionTime = DateTime.MinValue;
            A.CallTo(() => command1.ExecuteAsync(null, null))
                .Invokes(() => command1ExecutionTime = DateTime.Now)
                .ReturnsLazily(() => Task.Delay(200));
            IPlcCommand command2 = A.Fake<IPlcCommand>();
            DateTime command2ExecutionTime = DateTime.MinValue;
            A.CallTo(() => command2.ExecuteAsync(A<ICommandInput>._, null))
                .Invokes(() => command2ExecutionTime = DateTime.Now);
            SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

            // Act
            Task.WaitAll(new[] {
                command1.LockedExecuteAsync(_semaphore),
                command2.LockedExecuteAsync(_semaphore, A.Fake<ICommandInput>()),
            });

            // Assert
            A.CallTo(() => command1.ExecuteAsync(null, null))
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => command2.ExecuteAsync(A<ICommandInput>._, null))
                .MustHaveHappened(1, Times.Exactly);
            // With the delay of 200ms in command1, command2 should minimal be this time later
            command2ExecutionTime.Should().BeAfter(command1ExecutionTime.AddMilliseconds(200));
        }
    }
}
