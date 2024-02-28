using FluentAssertions;
using System;
using Xunit;

namespace Mbc.Ads.Utils.Test
{
    public class AdsConvertTest
    {
        [Fact]
        public void ConvertionFromIntToUInt32()
        {
            // Arrange
            int value = 42;

            // Act
            object res = AdsConvert.ChangeType<UInt32>(value);

            // Assert
            res.Should().BeOfType<UInt32>();
        }

        [Fact]
        public void ConvertionFromTimeToTime()
        {
            // Arrange
            var ts = TimeSpan.FromMilliseconds(55234);
            var value = new TwinCAT.PlcOpen.TIME(ts);

            // Act
            var res = AdsConvert.ChangeType<TwinCAT.PlcOpen.TIME>(value);

            // Assert
            res.Should().Be(value);
        }

        [Fact]
        public void ConvertionFromTimeToTimeSpan()
        {
            // Arrange
            var ts = TimeSpan.FromMilliseconds(55234);
            var value = new TwinCAT.PlcOpen.TIME(ts);

            // Act
            TimeSpan res = AdsConvert.ChangeType<TimeSpan>(value);

            // Assert
            res.Should().Be(ts);
        }

        [Fact]
        public void ConvertionFromTimeSpanToTime()
        {
            // Arrange
            var value = TimeSpan.FromMilliseconds(55234);

            // Act
            var res = AdsConvert.ChangeType<TwinCAT.PlcOpen.TIME>(value);

            // Assert
            res.Time.Should().Be(value);
        }

        [Fact]
        public void ConvertionFromDateTimeToDate()
        {
            // Arrange
            var value = DateTime.Parse("28.02.2024 09:57:32 +01:00");

            // Act
            var res = AdsConvert.ChangeType<TwinCAT.PlcOpen.DATE>(value);

            // Assert
            res.Date.Should().Be(value.Date);
        }

        [Fact]
        public void ConvertionFromDateOffsetTimeToDate()
        {
            // Arrange
            var value = DateTimeOffset.Parse("28.02.2024 09:57:32 +01:00");

            // Act
            var res = AdsConvert.ChangeType<TwinCAT.PlcOpen.DATE>(value);

            // Assert
            res.Date.Should().Be(value.Date);
        }

        [Fact]
        public void ConvertionFromDateToDateTimeOffset()
        {
            // Arrange
            var dt = DateTimeOffset.Parse("28.02.2024 09:57:32 +01:00");
            var value = new TwinCAT.PlcOpen.DATE(dt);

            // Act
            DateTimeOffset res = AdsConvert.ChangeType<DateTimeOffset>(value);

            // Assert
            res.Should().Be(dt.Date);
        }

        [Fact]
        public void ConvertionFromDateToDateTime()
        {
            // Arrange
            var dt = DateTime.Parse("28.02.2024 09:57:32 +01:00");
            var value = new TwinCAT.PlcOpen.DATE(dt);

            // Act
            DateTime res = AdsConvert.ChangeType<DateTime>(value);

            // Assert
            res.Should().Be(dt.Date);
        }

        [Theory]
        [InlineData("foo", typeof(string), "foo")]
        [InlineData(123, typeof(byte), 123)]
        public void ChangeTypeTests(object value, Type conversionType, object expectedValue)
        {
            // Arrange
            // Act
            object res = AdsConvert.ChangeType(value, conversionType);

            // Assert
            res.Should().NotBeNull();
            res.Should().BeOfType(conversionType);
            res.Should().Be(expectedValue);
        }
    }
}
