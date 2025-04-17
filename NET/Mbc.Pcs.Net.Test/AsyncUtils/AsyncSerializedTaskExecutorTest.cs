using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.AsyncUtils;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.AsyncUtils
{
    public class AsyncSerializedTaskExecutorTest : IDisposable
    {
        private AsyncSerializedTaskExecutor _executor;

        public AsyncSerializedTaskExecutorTest()
        {
            _executor = new AsyncSerializedTaskExecutor();
        }

        public void Dispose()
        {
            _executor.Dispose();
        }

        [Fact]
        public void StartStopWithoutExecutions()
        {
            // Arrange

            // Act

            // Assert
        }

        [Fact]
        public void ExecuteSingleAction()
        {
            // Arrange
            var executedAction = A.Fake<Action>();

            // Act
            _executor.Execute(executedAction);
            _executor.WaitForExecution();

            // Assert
            A.CallTo(() => executedAction.Invoke()).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void IsCalledFromExecutor()
        {
            // Arrange
            bool result = false;
            var executedAction = A.Fake<Action>();
            A.CallTo(() => executedAction.Invoke())
                .Invokes(() => { result = _executor.IsCalledFromExecutor; });

            // Act
            _executor.Execute(executedAction);
            _executor.WaitForExecution();

            // Assert
            _executor.IsCalledFromExecutor.Should().BeFalse();
            result.Should().BeTrue();
        }

        [Fact]
        public void ExceptionWillNotStopExecuting()
        {
            // Arrange
            var wait1 = new ManualResetEvent(false);
            var executedAction1 = A.Fake<Action>();
            A.CallTo(() => executedAction1.Invoke())
                .Invokes(() =>
                {
                    wait1.WaitOne();
                }).Throws(new Exception("foo"));
            var executedAction2 = A.Fake<Action>();
            var exceptionHandler = A.Fake<EventHandler<Exception>>();
            _executor.UnhandledException = exceptionHandler;

            // Act
            _executor.Execute(executedAction1);
            _executor.Execute(executedAction2);
            wait1.Set();
            _executor.WaitForExecution();

            // Assert
            A.CallTo(() => executedAction1.Invoke()).MustHaveHappenedOnceExactly();
            A.CallTo(() => executedAction2.Invoke()).MustHaveHappenedOnceExactly();
            A.CallTo(() => exceptionHandler.Invoke(_executor, A<Exception>.That.Matches(x => x.Message == "foo")))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void AsyncTaskSuccess()
        {
            // Arrange
            var action = A.Fake<Action>();

            // Act
            var task = _executor.ExecuteAsync(action);
            _executor.WaitForExecution();

            // Assert
            A.CallTo(() => action.Invoke()).MustHaveHappenedOnceExactly();
            task.IsCompleted.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void AsyncTaskException()
        {
            // Arrange
            var action = A.Fake<Action>();
            A.CallTo(() => action.Invoke())
                .Throws(new Exception("foo"));

            // Act
            var task = _executor.ExecuteAsync(action);
            _executor.WaitForExecution();

            // Assert
            A.CallTo(() => action.Invoke()).MustHaveHappenedOnceExactly();
            task.IsCompleted.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Faulted);
            task.Exception.InnerException.Message.Should().Be("foo");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "<Pending>")]
        public void AsyncTaskWithResultSuccess()
        {
            // Arrange
            var func = A.Fake<Func<int>>();
            A.CallTo(() => func.Invoke()).Returns(42);

            // Act
            var task = _executor.ExecuteAsync(func);
            _executor.WaitForExecution();

            // Assert
            A.CallTo(() => func.Invoke()).MustHaveHappenedOnceExactly();
            task.IsCompleted.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.RanToCompletion);
            task.Result.Should().Be(42);
        }


        [Fact]
        public void AsyncTaskWithResultException()
        {
            // Arrange
            var func = A.Fake<Func<int>>();
            A.CallTo(() => func.Invoke()).Throws(new Exception("foo"));

            // Act
            var task = _executor.ExecuteAsync(func);
            _executor.WaitForExecution();

            // Assert
            A.CallTo(() => func.Invoke()).MustHaveHappenedOnceExactly();
            task.IsCompleted.Should().BeTrue();
            task.Status.Should().Be(TaskStatus.Faulted);
            task.Exception.InnerException.Message.Should().Be("foo");
        }
    }
}
