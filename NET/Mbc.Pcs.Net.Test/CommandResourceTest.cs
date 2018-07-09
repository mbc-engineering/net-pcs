using FluentAssertions;
using Mbc.Pcs.Net.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Mbc.Pcs.Net.Test
{
    public class CommandResourceTest
    {
        [Fact]
        public void AddCustomResultCodeText()
        {
            // Arrange
            var subject = new CommandResource(new Dictionary<ushort, string>()
                {
                    [101] = "custom text 101",
                    [102] = "custom text 102"
                });            

            // Act
            subject.AddCustomResultCodeText(103, "custom text 103");

            // Assert
            subject.GetResultCodeString(101).Should().Be("custom text 101");
            subject.GetResultCodeString(102).Should().Be("custom text 102");
            subject.GetResultCodeString(103).Should().Be("custom text 103");
        }

        [Fact]
        public void AddNoCustomResultCodeText_CheckFallbacks()
        {
            // Arrange
            var subject = new CommandResource();

            // Act
            //

            // Assert
            subject.GetResultCodeString(101).Should().Be(string.Format(CommandResources.ERR_ResultCode, 101));
            subject.GetResultCodeString(3).Should().Be(CommandResources.ERR_ResultCode_3);
        }
    }
}
