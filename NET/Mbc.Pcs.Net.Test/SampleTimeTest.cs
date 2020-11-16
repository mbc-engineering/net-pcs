using FluentAssertions;
using System;
using Xunit;

namespace Mbc.Pcs.Net.Test
{
    public class SampleTimeTest
    {
        public SampleTimeTest()
        {
        }

        [Fact]
        public void CreateSampleTime()
        {
            // Act
            var result = new SampleTime(DateTime.FromFileTime(10000000), 500);

            // Assert
            result.SampleRate.Should().Be(500);
            result.SampleRateTimeSpan.Should().Be(TimeSpan.FromMilliseconds(2));
            result.ToDateTime().Should().Be(DateTime.FromFileTime(10000000));
        }

        [Fact]
        public void InvalidSampleRate()
        {
            // Act
            Func<SampleTime> act = () => new SampleTime(DateTime.Now, 3);

            // Assert
            act.Should().Throw<ArgumentException>().Which.Message.Contains("sampleRate");
        }

        [Fact]
        public void EqualsTo()
        {
            // Arrange
            var sampleTime = SampleTime.FromFileTime(1000000, 500);

            // Assert
            sampleTime.Equals(SampleTime.FromFileTime(1000000, 500)).Should().BeTrue();
            sampleTime.Equals((object)SampleTime.FromFileTime(1000000, 500)).Should().BeTrue();
            sampleTime.Equals(SampleTime.FromFileTime(1000000, 100)).Should().BeFalse();
            sampleTime.Equals((object)SampleTime.FromFileTime(1000000, 100)).Should().BeFalse();
            sampleTime.Equals(SampleTime.FromFileTime(2000000, 500)).Should().BeFalse();
            sampleTime.Equals((object)SampleTime.FromFileTime(2000000, 500)).Should().BeFalse();
        }

        [Fact]
        public void HashCode()
        {
            // Arrange
            var sampleTime = SampleTime.FromFileTime(1000000, 500);

            // Assert
            sampleTime.GetHashCode().Should().Be(SampleTime.FromFileTime(1000000, 500).GetHashCode());
        }

        [Fact]
        public void OperatorEquals()
        {
            // Arrange
            var sampleTime = SampleTime.FromFileTime(1000000, 500);

            // Assert
            (sampleTime == SampleTime.FromFileTime(1000000, 500)).Should().BeTrue();
            (sampleTime == SampleTime.FromFileTime(1000000, 100)).Should().BeFalse();
            (sampleTime == SampleTime.FromFileTime(2000000, 500)).Should().BeFalse();
        }

        [Fact]
        public void OperatorNotEquals()
        {
            // Arrange
            var sampleTime = SampleTime.FromFileTime(1000000, 500);

            // Assert
            (sampleTime != SampleTime.FromFileTime(1000000, 500)).Should().BeFalse();
            (sampleTime != SampleTime.FromFileTime(1000000, 100)).Should().BeTrue();
            (sampleTime != SampleTime.FromFileTime(2000000, 500)).Should().BeTrue();
        }

        [Fact]
        public void CompareTo()
        {
            // Arrange
            var sampleTime = SampleTime.FromFileTime(1000000, 500);

            // Assert
            sampleTime.CompareTo(SampleTime.FromFileTime(1000000, 500)).Should().Be(0);
            sampleTime.CompareTo(SampleTime.FromFileTime(2000000, 500)).Should().Be(-1);
            sampleTime.CompareTo(SampleTime.FromFileTime(0, 500)).Should().Be(1);
            sampleTime.CompareTo(SampleTime.FromFileTime(1000000, 50)).Should().Be(0);
            sampleTime.CompareTo(SampleTime.FromFileTime(2000000, 50)).Should().Be(-1);
            sampleTime.CompareTo(SampleTime.FromFileTime(0, 50)).Should().Be(1);
        }

        [Fact]
        public void CompareOperators()
        {
            // Arrange
            var sampleTime = SampleTime.FromFileTime(1000000, 500);

            // Assert
            (sampleTime < SampleTime.FromFileTime(1000000, 500)).Should().BeFalse();
            (sampleTime <= SampleTime.FromFileTime(1000000, 500)).Should().BeTrue();
            (sampleTime > SampleTime.FromFileTime(1000000, 500)).Should().BeFalse();
            (sampleTime >= SampleTime.FromFileTime(1000000, 500)).Should().BeTrue();

            (sampleTime < SampleTime.FromFileTime(2000000, 500)).Should().BeTrue();
            (sampleTime <= SampleTime.FromFileTime(2000000, 500)).Should().BeTrue();
            (sampleTime > SampleTime.FromFileTime(2000000, 500)).Should().BeFalse();
            (sampleTime >= SampleTime.FromFileTime(2000000, 500)).Should().BeFalse();

            (sampleTime < SampleTime.FromFileTime(0, 500)).Should().BeFalse();
            (sampleTime <= SampleTime.FromFileTime(0, 500)).Should().BeFalse();
            (sampleTime > SampleTime.FromFileTime(0, 500)).Should().BeTrue();
            (sampleTime >= SampleTime.FromFileTime(0, 500)).Should().BeTrue();

            (sampleTime < SampleTime.FromFileTime(1000000, 50)).Should().BeFalse();
            (sampleTime <= SampleTime.FromFileTime(1000000, 50)).Should().BeTrue();
            (sampleTime > SampleTime.FromFileTime(1000000, 50)).Should().BeFalse();
            (sampleTime >= SampleTime.FromFileTime(1000000, 50)).Should().BeTrue();

            (sampleTime < SampleTime.FromFileTime(2000000, 50)).Should().BeTrue();
            (sampleTime <= SampleTime.FromFileTime(2000000, 50)).Should().BeTrue();
            (sampleTime > SampleTime.FromFileTime(2000000, 50)).Should().BeFalse();
            (sampleTime >= SampleTime.FromFileTime(2000000, 50)).Should().BeFalse();

            (sampleTime < SampleTime.FromFileTime(0, 50)).Should().BeFalse();
            (sampleTime <= SampleTime.FromFileTime(0, 50)).Should().BeFalse();
            (sampleTime > SampleTime.FromFileTime(0, 50)).Should().BeTrue();
            (sampleTime >= SampleTime.FromFileTime(0, 50)).Should().BeTrue();
        }

        [Fact]
        public void DifferenceOfSampleTime()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 0), 1000);

            // Assert
            (sampleTime - new SampleTime(new DateTime(2019, 8, 21, 9, 35, 0), 1000)).Should().Be(60 * 1000);
            (sampleTime - new SampleTime(new DateTime(2019, 8, 21, 9, 37, 0), 1000)).Should().Be(-(60 * 1000));
        }

        [Fact]
        public void DifferenceOfSampleTimeAndInteger()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 30), 1000);

            // Assert
            (sampleTime - 1000L).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 29), 1000));
            (sampleTime - (-1000L)).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 31), 1000));
        }

        [Fact]
        public void SumOfSampleTimeAndInteger()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 30), 1000);

            // Assert
            (sampleTime + 1000L).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 31), 1000));
            (sampleTime + -1000L).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 29), 1000));
        }

        [Fact]
        public void DifferenceOfSampleTimeAndTimeSpan()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 30), 1000);

            // Assert
            (sampleTime - TimeSpan.FromSeconds(1)).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 29), 1000));
            (sampleTime - TimeSpan.FromSeconds(-1)).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 31), 1000));
        }

        [Fact]
        public void SumOfSampleTimeAndTimeSpan()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 30), 1000);

            // Assert
            (sampleTime + TimeSpan.FromSeconds(1)).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 31), 1000));
            (sampleTime + TimeSpan.FromSeconds(-1)).Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 29), 1000));
        }

        [Fact]
        public void ExplicitCastToLong()
        {
            // Arrange
            var sampleTime = new SampleTime(DateTime.FromFileTime(10 * 1000 * 1000), 1000);

            // Assert
            ((long)sampleTime).Should().Be(1000);
        }

        [Fact]
        public void Increment()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 30), 1);

            // Act
            ++sampleTime;

            // Assert
            sampleTime.Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 31), 1));
        }

        [Fact]
        public void Decrement()
        {
            // Arrange
            var sampleTime = new SampleTime(new DateTime(2019, 8, 21, 9, 36, 30), 1);

            // Act
            sampleTime--;

            // Assert
            sampleTime.Should().Be(new SampleTime(new DateTime(2019, 8, 21, 9, 36, 29), 1));
        }

        [Fact]
        public void ToRawValue()
        {
            // Assert
            new SampleTime(new DateTime(2010, 1, 1, 0, 0, 0), 1000).ToRawValue().Should().Be(2400000000000000000UL);
            new SampleTime(new DateTime(2010, 1, 1, 0, 0, 1), 1000).ToRawValue().Should().Be(2400000000000001000UL);
            new SampleTime(new DateTime(2010, 1, 1, 0, 0, 0), 1000000).ToRawValue().Should().Be(4800000000000000000UL);
            new SampleTime(new DateTime(2050, 1, 1, 0, 0, 0), 1000000).ToRawValue().Should().Be(4801262304000000000UL);
        }

        [Fact]
        public void FromRawValue()
        {
            // Assert
            SampleTime.FromRawValue(2400000000000000000UL).Should().Be(new SampleTime(new DateTime(2010, 1, 1, 0, 0, 0), 1000));
            SampleTime.FromRawValue(2400000000000001000UL).Should().Be(new SampleTime(new DateTime(2010, 1, 1, 0, 0, 1), 1000));
            SampleTime.FromRawValue(4800000000000000000UL).Should().Be(new SampleTime(new DateTime(2010, 1, 1, 0, 0, 0), 1000));
            SampleTime.FromRawValue(4801262304000000000UL).Should().Be(new SampleTime(new DateTime(2050, 1, 1, 0, 0, 0), 1000));
        }
    }
}
