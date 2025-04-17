using FluentAssertions;
using Mbc.Pcs.Net.AsyncUtils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.AsyncUtils
{
    public class TaskExtensionTest
    {
        [Fact]
        public void TimeoutAfterNormal()
        {
            // Arrange
            var tcs = new TaskCompletionSource();
            var task = tcs.Task;

            // Act
            var monitoredTask = task.TimeoutAfter(TimeSpan.FromSeconds(1));

            // Assert
            monitoredTask.Status.Should().Be(TaskStatus.WaitingForActivation);
            tcs.SetResult();
            monitoredTask.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void TimeoutAfterException()
        {
            // Arrange
            var tcs = new TaskCompletionSource();
            var task = tcs.Task;
            var ex = new Exception("foo");

            // Act
            var monitoredTask = task.TimeoutAfter(TimeSpan.FromSeconds(1));

            // Assert
            monitoredTask.Status.Should().Be(TaskStatus.WaitingForActivation);
            tcs.SetException(ex);
            monitoredTask.Status.Should().Be(TaskStatus.Faulted);
            monitoredTask.Exception.InnerException.Should().BeSameAs(ex);
        }

        [Fact]
        public void TimeoutAfterCancelled()
        {
            // Arrange
            var tcs = new TaskCompletionSource();
            var task = tcs.Task;

            // Act
            var monitoredTask = task.TimeoutAfter(TimeSpan.FromSeconds(1));

            // Assert
            monitoredTask.Status.Should().Be(TaskStatus.WaitingForActivation);
            tcs.SetCanceled();
            monitoredTask.Status.Should().Be(TaskStatus.Canceled);
        }

        [Fact]
        public void TimeoutAfterTimeout()
        {
            // Arrange
            var tcs = new TaskCompletionSource();
            var task = tcs.Task;

            // Act
            Func<Task> func = async () => await task.TimeoutAfter(TimeSpan.FromSeconds(1));

            // Assert
            func.Should().ThrowAsync<TimeoutException>();
        }

        [Fact]
        public async Task TimeoutAfterCancelledFromCancellationToken()
        {
            // Arrange
            var tcs = new TaskCompletionSource();
            var task = tcs.Task;
            var cts = new CancellationTokenSource();

            // Act
            var monitoredTask = task.TimeoutAfter(TimeSpan.FromSeconds(1), cts.Token);

            // Assert
            monitoredTask.Status.Should().Be(TaskStatus.WaitingForActivation);
            cts.Cancel();
            try
            {
                await monitoredTask;
            }
            catch (Exception e)
            {
                e.Should().BeOfType<TaskCanceledException>();
            }

            monitoredTask.Status.Should().Be(TaskStatus.Canceled);
        }

        [Theory]
        [InlineData(-922337203685476.0, false)] // minvalue
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(10000, true)] // 10s
        [InlineData(4233600000, true)] // 49 Days
        [InlineData(4233600001, false)] // 49 Days + 1ms
        [InlineData(922337203685476.0, false)] // max value
        public async Task TimeoutAfterTimOutRange(double timeOutMs, bool pass)
        {
            // Arrange
            var ts = TimeSpan.FromMilliseconds(timeOutMs);
            var tcs = new TaskCompletionSource();
            var task = tcs.Task;
            var cts = new CancellationTokenSource();

            // Act
            var monitoredTask = Record.ExceptionAsync(() => task.TimeoutAfter(ts, cts.Token));
            cts.Cancel();
            Exception ex = await monitoredTask;

            // Assert
            if (pass)
            {
                ex.Should().BeOfType<TaskCanceledException>();
            }
            else
            {
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }
        }

        private class TaskCompletionSource
        {
            private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

            public Task Task => _tcs.Task;

            public void SetResult()
            {
                _tcs.SetResult(null);
            }

            public void SetException(Exception exception)
            {
                _tcs.SetException(exception);
            }

            public void SetCanceled()
            {
                _tcs.SetCanceled();
            }
        }
    }
}
