using FakeItEasy;
using FluentAssertions;
using Mbc.Ads.Mapper;
using TwinCAT.Ads;
using Xunit;

namespace Mbc.Ads.Mapper.Test
{
    public class AdsSymbolReaderTest
    {
        [Fact]
        public void CouldNotReadAdsSymbolThrowsExeption()
        {
            // Arrange
            var connection = A.Fake<IAdsSymbolicAccess>();
            string variablePath = "PLC.VariablesStruct";
            A.CallTo(() => connection.ReadSymbolInfo(A<string>.That.Matches(s => s == variablePath)))
                .Returns(null);

            // Act
            var exception = Record.Exception(() => AdsSymbolReader.Read(connection, variablePath));

            // Assert
            exception.Should().BeOfType<AdsMapperException>();
            exception.Message.Should().Contain(variablePath);
        }
    }
}
