using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.State;
using Mbc.Pcs.Net.State.Utils;
using Mbc.Pcs.Net.Test.TestUtils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.State.Utils
{
    public class IPlcStateSamplerExtensionsTest
    {
        [Fact]
        public async Task StateIsEnsured()
        {
            // Arrange
            var sampler = A.Fake<IPlcStateSampler<TestState>>();
            var stateTrue1 = new TestState { PlcTimeStamp = DateTime.FromFileTime(0), Foo = 0, };
            var stateTrue2 = new TestState { PlcTimeStamp = stateTrue1.PlcTimeStamp.AddMilliseconds(20), Foo = 0, };
            var stateTrue3 = new TestState { PlcTimeStamp = stateTrue1.PlcTimeStamp.AddMilliseconds(100), Foo = 0, };

            // Act
            var task = sampler.EnsureStateAsync(x => x.Foo == 0, TimeSpan.FromMilliseconds(100), default);
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { stateTrue1 }));
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { stateTrue2 }));
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { stateTrue3 }));
            var ensureResult = await task;

            // Assert
            ensureResult.Should().BeTrue();
        }

        [Fact]
        public async Task StateIsNotEnsured()
        {
            // Arrange
            var sampler = A.Fake<IPlcStateSampler<TestState>>();
            var stateTrue = new TestState { PlcTimeStamp = DateTime.FromFileTime(0), Foo = 0, };
            var stateFalse = new TestState { PlcTimeStamp = stateTrue.PlcTimeStamp.AddMilliseconds(20), Foo = 42, };

            // Act
            var task = sampler.EnsureStateAsync(x => x.Foo == 0, TimeSpan.FromMilliseconds(100), default);
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { stateTrue }));
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { stateFalse }));
            var ensureResult = await task;

            // Assert
            ensureResult.Should().BeFalse();
            task.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public async Task EnsureStateAsyncExceptionTest()
        {
            // Arrange
            var sampler = A.Fake<IPlcStateSampler<TestState>>();
            var state = new TestState { PlcTimeStamp = DateTime.FromFileTime(0) };

            // Act
            var exceptionTask = Record.ExceptionAsync(async () => await sampler.EnsureStateAsync(x => throw new Exception("throwed Exeption"), TimeSpan.FromSeconds(10), default));
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { state }));
            var exception = await exceptionTask;

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be("throwed Exeption");
        }

        [Fact]
        public async Task WaitForStateAsyncTest()
        {
            // Arrange
            var initialState = new TestState { PlcTimeStamp = DateTime.FromFileTime(0), Foo = 1 };
            var sampler = A.Fake<PlcStateSamplerMock>();
            A.CallTo(() => sampler.CurrentSample).Returns(initialState);
            A.CallTo(() => sampler.StatesChangedHandlerAdded(A<EventHandler<PlcMultiStateChangedEventArgs<TestState>>>.Ignored))
               .Invokes(() =>
               {
                   sampler.OnStateChange(new TestState { PlcTimeStamp = DateTime.FromFileTime(10), Foo = 2 }, DateTime.FromFileTime(10));
               });

            // Act
            var waitTime = await sampler.WaitForStateAsync(s => s.Foo == 2, TimeSpan.FromSeconds(10), CancellationToken.None);

            // Assert
            waitTime.Should().Be(DateTime.FromFileTime(10));
        }

        [Fact]
        public async Task WaitForStateAsyncCancelTest()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var initialState = new TestState { PlcTimeStamp = DateTime.FromFileTime(0), Foo = 1 };
            var sampler = A.Fake<PlcStateSamplerMock>();
            A.CallTo(() => sampler.CurrentSample).Returns(initialState);
            A.CallTo(() => sampler.StatesChangedHandlerAdded(A<EventHandler<PlcMultiStateChangedEventArgs<TestState>>>.Ignored))
               .Invokes(() =>
               {
                   sampler.OnStateChange(new TestState { PlcTimeStamp = DateTime.FromFileTime(2), Foo = 1 }, DateTime.FromFileTime(2));
                   cts.Cancel();
               });

            // Act
            var ex = await Record.ExceptionAsync(() => sampler.WaitForStateAsync(s => s.Foo == 2, TimeSpan.FromSeconds(10), cts.Token));

            // Assert
            ex.Should().BeOfType<TaskCanceledException>();
        }

        [Fact]
        public async Task WaitForStateAsyncTimeOutTest()
        {
            // Arrange
            var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var initialState = new TestState { PlcTimeStamp = DateTime.FromFileTime(0), Foo = 1 };
            var sampler = A.Fake<PlcStateSamplerMock>();
            A.CallTo(() => sampler.CurrentSample).Returns(initialState);
            A.CallTo(() => sampler.StatesChangedHandlerAdded(A<EventHandler<PlcMultiStateChangedEventArgs<TestState>>>.Ignored))
               .Invokes(() =>
               {
                   sampler.OnStateChange(new TestState { PlcTimeStamp = DateTime.FromFileTime(2), Foo = 1 }, DateTime.FromFileTime(2));
               });

            // Act
            var ex = await Record.ExceptionAsync(() => sampler.WaitForStateAsync(s => s.Foo == 2, TimeSpan.FromSeconds(10), cts.Token));

            // Assert
            ex.Should().BeOfType<TaskCanceledException>();
        }

        [Fact]
        public async Task WaitForStateAsyncExceptionTest()
        {
            // Arrange
            var sampler = A.Fake<IPlcStateSampler<TestState>>();
            var state = new TestState { PlcTimeStamp = DateTime.FromFileTime(0) };

            // Act
            var exceptionTask = Record.ExceptionAsync(async () => await sampler.WaitForStateAsync(x => throw new Exception("throwed Exeption"), TimeSpan.FromSeconds(10), default));
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { state }));
            var exception = await exceptionTask;

            // Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Be("throwed Exeption");
        }
    }
}
