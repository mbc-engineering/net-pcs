using AwesomeAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils
{
    public class H5DataSetTest
    {
        public H5DataSetTest()
        {
        }

        [Fact]
        public void OpenDataSet()
        {
            // Arrange
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                new H5DataSet.Builder()
                    .WithName("foo")
                    .WithType<float>()
                    .WithDimension(100)
                    .WithChunking(10)
                    .Create(h5file);
            }

            // Act
            using (var h5file = new H5File(file, H5File.Flags.ReadOnly))
            {
                var dataSet = H5DataSet.Open(h5file, "foo");

                // Assert
                dataSet.Should().NotBeNull();
                dataSet.ValueType.Should().Be(typeof(float));
                dataSet.GetDimensions().Should().BeEquivalentTo(new ulong[] { 100 });
                dataSet.GetMaxDimensions().Should().BeEquivalentTo(new ulong[] { 100 });
                dataSet.GetChunkSize().Should().BeEquivalentTo(new ulong[] { 10 });
            }
        }

        [Fact]
        public void WriteAndReadAgain()
        {
            // Arrange
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSet = new H5DataSet.Builder()
                    .WithName("foo")
                    .WithType<float>()
                    .WithDimension(100)
                    .WithChunking(10)
                    .Create(h5file);

                Array values = Enumerable.Range(0, 100).Select(i => (float)i).ToArray();
                dataSet.Write(values);
            }

            // Act
            Array readValues = new float[50];
            using (var h5file = new H5File(file, H5File.Flags.ReadOnly))
            {
                var dataSet = H5DataSet.Open(h5file, "foo");
                using (var fileDataSpace = dataSet.GetSpace())
                {
                    H5DataSpace.CreateSelectionBuilder()
                        .Start(10).Count(50).ApplyTo(fileDataSpace);

                    dataSet.Read(readValues, fileDataSpace);
                }
            }

            // Assert
            readValues.Should().BeEquivalentTo(Enumerable.Range(10, 50).Select(i => (float)i));
        }
    }
}
