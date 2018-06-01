using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test
{
    public class PlcCommandTests
    {
        /// <summary>
        /// A command has a default timeout of 5 seconds
        /// </summary>
        [Fact]
        public void CheckDefaultTimeOut()
        {
            // Arrange            
            var subject = new PlcCommand(null, "fbXyz");

            // Act
            ;

            // Assert
            subject.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        }
    }
}
