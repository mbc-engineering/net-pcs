using FakeItEasy;
using FluentAssertions;
using System.IO;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test
{
    public class EofStreamTest
    {
        [Fact]
        public void SimpleTest()
        {
            // Arrange
            var stream = new MemoryStream(new byte[] { 1, 2 });
            var eofStream = new EofStream(stream);

            // Act
            var b1 = eofStream.ReadByte();
            var b2 = eofStream.ReadByte();
            var b3 = eofStream.ReadByte();

            // Assert
            b1.Should().Be(1);
            b2.Should().Be(2);
            b3.Should().Be(-1);
            eofStream.IsEof.Should().BeTrue();
        }

        [Fact]
        public void NoNEmptySreamShouldNotReturnEof()
        {
            // Arrange
            var eofStream = new EofStream(new MemoryStream(new byte[] { 1 }));

            // Act
            bool eof = eofStream.IsEof;

            // Assert
            eof.Should().BeFalse();
        }

        [Fact]
        public void EmptySreamShouldReturnEof()
        {
            // Arrange
            var eofStream = new EofStream(new MemoryStream(new byte[0]));

            // Act
            bool eof = eofStream.IsEof;

            // Assert
            eof.Should().BeTrue();
        }

        [Fact]
        public void DisposeShouldBeDispatchedToUnderlyingStream()
        {
            // Arrange
            var stream = A.Fake<Stream>();
            A.CallTo(() => stream.CanRead).Returns(true);
            var eofStream = new EofStream(stream);

            // Act
            eofStream.Dispose();

            // Assert
            A.CallTo(() => stream.Close()).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void StreamShouldHaveOnlyReadCapabilities()
        {
            // Arrange
            var eofStream = new EofStream(new MemoryStream(new byte[0]));

            // Act
            var canRead = eofStream.CanRead;
            var canSeek = eofStream.CanSeek;
            var canWrite = eofStream.CanWrite;

            // Assert
            canRead.Should().BeTrue();
            canSeek.Should().BeFalse();
            canWrite.Should().BeFalse();
        }

        [Fact]
        public void LengthShouldBeDispatchedToUnderlyingStream()
        {
            // Arrange
            var eofStream = new EofStream(new MemoryStream(new byte[] { 1 }));

            // Act
            long length = eofStream.Length;

            // Assert
            length.Should().Be(1);
        }
    }
}
