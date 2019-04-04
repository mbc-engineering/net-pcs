using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.State;
using System;
using System.Collections.Generic;
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
            var sampler = A.Fake<IPlcStateSampler<StateMock>>();
            var state = new StateMock { PlcTimeStamp = DateTime.FromFileTime(0) };
            var task = sampler.EnsureStateAsync(x => x.Foo == 0, TimeSpan.FromMilliseconds(100), default);

            // Act
            var completed1 = task.IsCompleted;
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<StateMock>(new List<StateMock> { state }));
            var completed2 = task.IsCompleted;
            state.PlcTimeStamp += TimeSpan.FromMilliseconds(100);
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<StateMock>(new List<StateMock> { state }));

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
            var sampler = A.Fake<IPlcStateSampler<StateMock>>();
            var state = new StateMock { PlcTimeStamp = DateTime.FromFileTime(0) };
            var task = sampler.EnsureStateAsync(x => x.Foo == 0, TimeSpan.FromMilliseconds(100), default);

            // Act
            var completed1 = task.IsCompleted;
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<StateMock>(new List<StateMock> { state }));
            var completed2 = task.IsCompleted;
            state.PlcTimeStamp += TimeSpan.FromMilliseconds(100);
            state.Foo = 42;
            sampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<StateMock>(new List<StateMock> { state }));

            // Assert
            completed1.Should().BeFalse();
            completed2.Should().BeFalse();
            task.IsCompleted.Should().BeTrue();
            task.Result.Should().BeFalse();
        }

        public class StateMock : IPlcState
        {
            public StateMock()
            {
            }

            public int Foo { get; set; }
            public DateTime PlcTimeStamp { get; set; }
        }
    }
}
