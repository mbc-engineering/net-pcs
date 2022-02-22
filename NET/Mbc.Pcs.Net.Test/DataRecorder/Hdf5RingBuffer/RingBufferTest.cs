using FluentAssertions;
using Mbc.Hdf5Utils;
using Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer;
using System.IO;
using System.Linq;
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
                ringBuffer.CommitWrite();

                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(3);
                ringBuffer.Count.Should().Be(3);
                ringBuffer.LastSampleIndex.Should().Be(3);
            }
        }

        [Fact(Skip = "Momentan werden keine Oversampling-Kanäle geloggt")]
        public void OversamplingWriteNewFile()
        {
            /*
             * TODO:
             *  - Konzept überlegen wie das Schreiben und Lesen mit Oversampling funktionieren soll
             */

            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(int), 5);
            var ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1 });

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Act
                ringBuffer.WriteChannel("c1", Enumerable.Range(0, 10).ToArray());
                ringBuffer.CommitWrite();

                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(2, "oversampling factor should not be included");
                ringBuffer.Count.Should().Be(2, "oversampling factor should not be included");
                ringBuffer.LastSampleIndex.Should().Be(2, "oversampling factor should not be included");
            }

            // Assert
            File.Exists(file).Should().BeTrue();
            using (var h5File = new H5File(file, H5File.Flags.ReadOnly))
            {
                h5File.GetNames().Should().BeEquivalentTo("c1");
                using (var c1DataSet = H5DataSet.Open(h5File, "c1"))
                {
                    c1DataSet.ValueType.Should().Be(typeof(int));
                    c1DataSet.GetDimensions().Should().BeEquivalentTo(new[] { 1000 * 5UL }, "oversampling factor should be included");
                    c1DataSet.Attributes().GetAttributeNames().Should().BeEquivalentTo("OversamplingFactor");
                    c1DataSet.Attributes().ReadInt("OversamplingFactor").Should().Be(5);
                }
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
                ringBuffer.CommitWrite();
            }

            // Act
            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(3);
                ringBuffer.Count.Should().Be(3);
            }
        }

        [Fact]
        public void OpenExistingFileWithNewChannels()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1 });

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 1, 2, 3 });
                ringBuffer.CommitWrite();
            }

            // Act
            channelInfo1 = new ChannelInfo("c1", typeof(float));
            var channelInfo2 = new ChannelInfo("c2", typeof(int));
            ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1, channelInfo2 });
            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(0);
                ringBuffer.Count.Should().Be(0);
            }
        }

        [Fact]
        public void OpenExistingFileWithNewChannelType()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1 });

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 1, 2, 3 });
                ringBuffer.CommitWrite();
            }

            // Act
            channelInfo1 = new ChannelInfo("c1", typeof(int));
            ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1 });
            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(0);
                ringBuffer.Count.Should().Be(0);
            }
        }

        [Fact]
        public void OpenExistingCorruptedFile()
        {
            // Arrange
            var file = Path.GetTempFileName();

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(1000, 100, new[] { channelInfo1 });

            // Act
            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(0);
                ringBuffer.Count.Should().Be(0);
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
                ringBuffer.CommitWrite();

                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(50);
                ringBuffer.Count.Should().Be(100);
                ringBuffer.LastSampleIndex.Should().Be(150);
            }
        }

        [Fact]
        public void WriteWithOverrunEnsureSize()
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
                ringBuffer.CommitWrite();
                ringBuffer.WriteChannel("c1", Enumerable.Range(0, 150).Select(x => (float)x).ToArray());
                ringBuffer.WriteChannel("c2", Enumerable.Range(0, 150).Select(x => (int)x).ToArray());
                ringBuffer.CommitWrite();

                // Assert
                File.Exists(file).Should().BeTrue();
                ringBuffer.CurrentWritePos.Should().Be(0);
                ringBuffer.Count.Should().Be(100);
                ringBuffer.LastSampleIndex.Should().Be(300);
            }
        }

        [Fact]
        public void ReadSimple()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[3];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 2, buffer);
            }

            // Assert
            result.Should().Be(3);
            buffer.Should().BeEquivalentTo(11F, 12F, 13F);
        }

        [Fact]
        public void ReadSimpleTooMany()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[3];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 0, buffer);
            }

            // Assert
            result.Should().Be(2);
            buffer.Should().BeEquivalentTo(0F, 10F, 11F);
        }

        [Fact]
        public void ReadSimpleNothing()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[3];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", -3, buffer);
            }

            // Assert
            result.Should().Be(-1);
            buffer.Should().BeEquivalentTo(0F, 0F, 0F);
        }

        [Fact]
        public void ReadSimpleOverrun()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(10, 5, new[] { channelInfo1 });

            var buffer = new float[4];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 9, buffer);
            }

            // Assert
            result.Should().Be(4);
            buffer.Should().BeEquivalentTo(18F, 19F, 20F, 21F);
        }

        [Fact]
        public void ReadSimpleOverrunFull()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(10, 5, new[] { channelInfo1 });

            var buffer = new float[10];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", ringBuffer.LastSampleIndex - 9, buffer);
            }

            // Assert
            result.Should().Be(10);
            buffer.Should().BeEquivalentTo(Enumerable.Range(13, 10).Select(x => (float)x));
        }

        [Fact]
        public void ReadSimpleWithOffsetAndCount()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[10];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 1, buffer, 4, 5);
            }

            // Assert
            result.Should().Be(5);
            buffer.Should().BeEquivalentTo(0F, 0F, 0F, 0F, 10F, 11F, 12F, 13F, 14F, 0F);
        }

        [Fact]
        public void ReadChannel_WithStride_ShouldSkipValues()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[3];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14, 15, 16, 17 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 1, buffer, 0, 3, 2);
            }

            // Assert
            result.Should().Be(3);
            buffer.Should().BeEquivalentTo(10F, 12F, 14F);
        }

        [Fact]
        public void ReadChannel_WithStrideSampleCount_ShouldRightAlignValues()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[6];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14, 15, 16, 17 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 1, buffer, 0, 6, 2);
            }

            // Assert
            result.Should().Be(4);
            buffer.Should().BeEquivalentTo(0F, 0F, 10F, 12F, 14F, 16F);
        }

        [Fact]
        public void ReadChannel_WithStrideStartSample_ShouldRightAlignValues()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[6];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 7, buffer, 0, 6, 2);
            }

            // Assert
            result.Should().Be(4);
            buffer.Should().BeEquivalentTo(0F, 0F, 16F, 18F, 20F, 22F);
        }

        [Fact]
        public void ReadChannel_WithStrideBufferSize_ShouldRightAlignValues()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(10, 5, new[] { channelInfo1 });

            var buffer = new float[6];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Ringbuffer min. 1x umrunden
                ringBuffer.WriteChannel("c1", Enumerable.Range(10, 20).Select(x => (float)x).ToArray());
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 14, buffer, 0, 6, 2);
            }

            // Assert
            result.Should().Be(4);
            buffer.Should().BeEquivalentTo(0F, 0F, 23F, 25F, 27F, 29F);
        }

        [Fact]
        public void ReadChannel_WithStride_ShouldWrap()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(10, 5, new[] { channelInfo1 });

            var buffer = new float[6];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                // Ringbuffer min. 1x umrunden
                ringBuffer.WriteChannel("c1", Enumerable.Range(10, 18).Select(x => (float)x).ToArray());
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 6, buffer, 0, 6, 3);
            }

            // Assert
            result.Should().Be(4);
            buffer.Should().BeEquivalentTo(0F, 0F, 18F, 21F, 24F, 27F);
        }

        [Fact]
        public void ReadSingleElement()
        {
            // Arrange
            var file = Path.GetTempFileName();
            File.Delete(file);
            _testOutput.WriteLine($"Test HDF5: {file}");

            var channelInfo1 = new ChannelInfo("c1", typeof(float));
            var ringBufferInfo = new RingBufferInfo(100, 10, new[] { channelInfo1 });

            var buffer = new float[1];
            int result;

            using (var ringBuffer = new RingBuffer(file, ringBufferInfo))
            {
                ringBuffer.WriteChannel("c1", new float[] { 10, 11, 12, 13, 14 });
                ringBuffer.CommitWrite();

                // Act
                result = ringBuffer.ReadChannel("c1", 3, buffer);
            }

            // Assert
            result.Should().Be(1);
            buffer.Should().BeEquivalentTo(12F);
        }
    }
}
