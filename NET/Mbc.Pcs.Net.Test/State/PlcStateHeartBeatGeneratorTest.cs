using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.Connection;
using Mbc.Pcs.Net.State;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test.State
{
    public class PlcStateHeartBeatGeneratorTest : IDisposable
    {
        private readonly PlcStateHeartBeatGenerator<PlcStateDummy> _testee;
        private readonly IPlcAdsConnectionService _adsConnection;
        private IPlcStateSampler<PlcStateDummy> _plcStateSampler;

        public PlcStateHeartBeatGeneratorTest()
        {
            _adsConnection = A.Fake<IPlcAdsConnectionService>();
            _plcStateSampler = A.Fake<IPlcStateSampler<PlcStateDummy>>();

            _testee = new PlcStateHeartBeatGenerator<PlcStateDummy>(TimeSpan.FromSeconds(1), _adsConnection, _plcStateSampler);
        }

        public void Dispose()
        {
            _testee.Dispose();
        }

        [Fact]
        public void CheckMinimalHeartBeatIntervalValue()
        {
            // Arrange
            A.CallTo(() => _plcStateSampler.SampleRate)
                .Returns(1U);

            // Act
            var ex = Record.Exception(() => _testee.HeartBeatInterval = TimeSpan.FromMilliseconds(0));

            // Assert
            ex.Should().NotBeNull();
            ex.Should().BeOfType<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void BeatEventShouldTriggerAfterConnection()
        {
            // Arrange
            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                // #1 connection is needed
                _adsConnection.ConnectionStateChanged += Raise.With(new PlcConnectionChangeArgs(true, null));
                // #2 new state
                _plcStateSampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<PlcStateDummy>(new List<PlcStateDummy> { { new PlcStateDummy { PlcTimeStamp = DateTime.FromFileTime(10) } } }));

                // Assert
                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartBeats))
                    .WithSender(_testee)
                    .WithArgs<HeartBeatEventArgs>(args => args.BeatTime == DateTime.FromFileTime(10));

                monitoredTestee
                    .Should().NotRaise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartDied));
            }
        }

        [Fact]
        public void BeatEventShouldTriggerOnceInTheInverval()
        {
            // Arrange
            int heartBeatCounter = 0;
            _testee.HeartBeats += (s, args) => heartBeatCounter++;
            _testee.HeartBeatInterval = TimeSpan.FromMilliseconds(100);
            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                // #1 connection is needed
                _adsConnection.ConnectionStateChanged += Raise.With(new PlcConnectionChangeArgs(true, null));
                // #2 fire states until 2 and a half invervall time frames elapsed
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed <= TimeSpan.FromMilliseconds(290))
                {
                    _plcStateSampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<PlcStateDummy>(new List<PlcStateDummy> { { new PlcStateDummy { PlcTimeStamp = DateTime.MinValue.Add(sw.Elapsed) } } }));
                }

                // Assert
                heartBeatCounter.Should().Be(3, "A initial heart beat and also 2 interval heart beats");
                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartBeats))
                    .WithSender(_testee)
                    .WithArgs<HeartBeatEventArgs>(args => args.BeatTime > DateTime.MinValue);

                monitoredTestee
                    .Should().NotRaise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartDied));
            }
        }

        [Fact]
        public async Task HeardDiedEventShouldTriggerAfterConnectionWithoutBeat()
        {
            // Arrange
            _testee.TimeUntilDie = TimeSpan.FromMilliseconds(100);
            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                // #1 connection is needed
                _adsConnection.ConnectionStateChanged += Raise.With(new PlcConnectionChangeArgs(true, null));

                // wait for timeout
                await Task.Delay(TimeSpan.FromMilliseconds(200));

                // Assert
                monitoredTestee
                    .Should().NotRaise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartBeats));

                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartDied))
                    .WithSender(_testee)
                    .WithArgs<HeartBeatDiedEventArgs>(args => args.LastHeartBeat == DateTime.MinValue, args => args.DiedTime == args.LastHeartBeat.Add(_testee.TimeUntilDie));
            }
        }

        [Fact]
        public async Task HeardDiedEventShouldTriggerBeatsLost()
        {
            // Arrange
            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                // #1 connection is needed
                _adsConnection.ConnectionStateChanged += Raise.With(new PlcConnectionChangeArgs(true, null));
                // #2 one state raise then nomore
                _plcStateSampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<PlcStateDummy>(new List<PlcStateDummy> { { new PlcStateDummy { PlcTimeStamp = DateTime.FromFileTime(10) } } }));

                // wait for timeout
                await Task.Delay(_testee.TimeUntilDie);

                // Assert
                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartBeats))
                    .WithSender(_testee)
                    .WithArgs<HeartBeatEventArgs>(args => args.BeatTime == DateTime.FromFileTime(10));

                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartDied))
                    .WithSender(_testee)
                    .WithArgs<HeartBeatDiedEventArgs>(args => args.LastHeartBeat == DateTime.FromFileTime(10), args => args.DiedTime == args.LastHeartBeat.Add(_testee.TimeUntilDie));
            }
        }

        [Fact]
        public async Task HearDiedEventDoesTriggerWhenConnectionLost()
        {
            // Arrange
            _testee.TimeUntilDie = TimeSpan.FromMilliseconds(100);
            using (var monitoredTestee = _testee.Monitor())
            {
                // Act
                // #1 connection is needed
                _adsConnection.ConnectionStateChanged += Raise.With(new PlcConnectionChangeArgs(true, null));
                // #2 new data recived
                _plcStateSampler.StatesChanged += Raise.With(new PlcMultiStateChangedEventArgs<PlcStateDummy>(new List<PlcStateDummy> { { new PlcStateDummy { PlcTimeStamp = DateTime.FromFileTime(10) } } }));
                // #3 connection lost
                _adsConnection.ConnectionStateChanged += Raise.With(new PlcConnectionChangeArgs(false, null));
                // wait for timeout
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                // Assert
                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartBeats))
                    .WithSender(_testee)
                    .WithArgs<HeartBeatEventArgs>(args => args.BeatTime == DateTime.FromFileTime(10));

                monitoredTestee
                    .Should().Raise(nameof(PlcStateHeartBeatGenerator<PlcStateDummy>.HeartDied))
                    .WithArgs<HeartBeatDiedEventArgs>(args => args.LastHeartBeat == DateTime.FromFileTime(10))
                    .WithArgs<HeartBeatDiedEventArgs>(args => args.DiedTime == DateTime.FromFileTime(10));
            }
        }

        public class PlcStateDummy : IPlcState
        {
            public DateTime PlcTimeStamp { get; set; }
            public PlcDataQuality PlcDataQuality { get; set; }
        }
    }
}
