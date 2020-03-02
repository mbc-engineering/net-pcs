using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.State;
using Mbc.Pcs.Net.Test.TestUtils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.State
{
    public class IPlcStateSamplerExtensionsTest
    {
        public IPlcStateSamplerExtensionsTest()
        {
        }

        [Fact(Skip = "Geht noch nicht wegen async")]
        public void StateIsEnsured()
        {
            // Arrange
            var sampler = A.Fake<IPlcStateSampler<TestState>>();
            var state = new TestState { PlcTimeStamp = DateTime.FromFileTime(0) };
            var task = sampler.EnsureStateAsync(x => x.Foo == 0, TimeSpan.FromMilliseconds(100), default);

            // Act
            var completed1 = task.IsCompleted;
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { state }));
            var completed2 = task.IsCompleted;
            state.PlcTimeStamp += TimeSpan.FromMilliseconds(100);
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { state }));

            // Assert
            completed1.Should().BeFalse();
            completed2.Should().BeFalse();
            task.IsCompleted.Should().BeTrue();
            task.Result.Should().BeTrue();
        }

        [Fact(Skip = "Geht noch nicht wegen async")]
        public void StateIsNotEnsured()
        {
            // Arrange
            var sampler = A.Fake<IPlcStateSampler<TestState>>();
            var state = new TestState { PlcTimeStamp = DateTime.FromFileTime(0) };
            var task = sampler.EnsureStateAsync(x => x.Foo == 0, TimeSpan.FromMilliseconds(100), default);

            // Act
            var completed1 = task.IsCompleted;
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { state }));
            var completed2 = task.IsCompleted;
            state.PlcTimeStamp += TimeSpan.FromMilliseconds(100);
            state.Foo = 42;
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<TestState>(new List<TestState> { state }));

            // Assert
            completed1.Should().BeFalse();
            completed2.Should().BeFalse();
            task.IsCompleted.Should().BeTrue();
            task.Result.Should().BeFalse();
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
    }
}
