using FluentAssertions;
using Mbc.Pcs.Net.Command;
using System;
using System.Collections.Generic;
using TwinCAT.PlcOpen;
using Xunit;

namespace Mbc.Pcs.Net.Test.Command
{
    public class CommandOutputBuilderTest
    {
        [Fact]
        public void ConvertTimeToTimeSpan()
        {
            // Arrange
            var builder = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "time", new TIME(TimeSpan.FromMinutes(2)) },
            });

            // Act
            var value = builder.GetOutputData<TimeSpan>("time");

            // Assert
            value.Should().Be(TimeSpan.FromMinutes(2));
        }

        [Fact]
        public void ConvertDateToDateTime()
        {
            // Arrange
            var builder = CommandOutputBuilder.FromDictionary(new Dictionary<string, object>
            {
                { "dt", new DATE(new DateTime(2019, 02, 15, 12, 43, 00)) },
            });

            // Act
            var value = builder.GetOutputData<DateTime>("dt");

            // Assert
            value.Should().Be(new DateTime(2019, 02, 15));
        }
    }
}
