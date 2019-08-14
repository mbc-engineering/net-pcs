using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Mbc.Pcs.Net.Test.DataRecorder.Hdf5RingBuffer
{
    public class RingBufferTest
    {
        private readonly ITestOutputHelper _testOutput;

        public RingBufferTest(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        [Fact]
        public void SimpleWriteNewFile()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var channelInfo2 = new ChannelInfo("c2", typeof(int));
            var ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1, channelInfo2 });

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Act
                ringBuffer.WriteChannel("c1", new float[] { 1, 2, 3 });
                ringBuffer.WriteChannel("c2", new int[] { 1, 2, 3 });
                ringBuffer.IncrementSamples(3);

                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(3);
            }
        }

        [Fact]
        public void OpenExistingFile()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var channelInfo2 = new ChannelInfo("c2", typeof(int));
            var ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1, channelInfo2 });

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 1, 2, 3 });
                ringBuffer.WriteChannel("c2", new int[] { 1, 2, 3 });
                ringBuffer.IncrementSamples(3);
            }

            // Act
            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(3);
            }
        }

        [Fact]
        public void WriteWithOverrun()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var channelInfo2 = new ChannelInfo("c2", typeof(int));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1, channelInfo2 });

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Act
                ringBuffer.WriteChannel("c1", Enumerable.Range(0, 150).Select(x => (float)x).ToArray());
                ringBuffer.WriteChannel("c2", Enumerable.Range(0, 150).Select(x => (int)x).ToArray());
                ringBuffer.IncrementSamples(150);

                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(50);
            }
        }
    }
}
