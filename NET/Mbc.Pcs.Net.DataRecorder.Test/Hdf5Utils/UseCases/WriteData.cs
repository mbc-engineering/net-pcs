using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using System;
using System.IO;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils.UseCases
{
    [Collection("HDF5")]
    public class WriteData
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public WriteData(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private string GetFile()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            return file;
        }

        [Fact]
        public void WriteOneDimensionalDataFull()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<double>()
                    .WithDimension(10);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    var data = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                    dataSet.Write(data);
                }
            }

            _testOutputHelper.WriteLine(file);
        }


        [Fact]
        public void WriteOneDimensionalDataPartial()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<double>()
                    .WithDimension(10);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    Array data = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                    using (var srcDataSpace = H5DataSpace.CreateSimpleFixed(new [] { (ulong) data.Length }))
                    using (var fileDataSpace = dataSet.GetSpace())
                    {
                        H5DataSpace.CreateSelectionBuilder()
                            .Start(5).Count(5).ApplyTo(srcDataSpace);
                        H5DataSpace.CreateSelectionBuilder()
                            .Start(0).Count(5).ApplyTo(fileDataSpace);
                        dataSet.Write(typeof(double), data, fileDataSpace, srcDataSpace);
                    }
                }
            }

            _testOutputHelper.WriteLine(file);
        }

        [Fact]
        public void WriteOneDimensionalDataFullWithExplicitType()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<double>()
                    .WithDimension(10);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    Array data = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                    dataSet.Write(typeof(double), data);
                }
            }

            _testOutputHelper.WriteLine(file);
        }

        [Fact]
        public void WriteOneDimensionalDataIncrementelFixed()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<double>()
                    .WithDimension(10);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    using (var space = dataSet.GetSpace())
                    {
                        H5DataSpace.CreateSelectionBuilder()
                            .Start(0).Count(5).ApplyTo(space);
                        dataSet.Write(new double[] { 1, 2, 3, 4, 5 }, space);
                    }

                    using (var space = dataSet.GetSpace())
                    {
                        H5DataSpace.CreateSelectionBuilder()
                            .Start(5).Count(5).ApplyTo(space);
                        dataSet.Write(new double[] { 6, 7, 8, 9, 10 }, space);
                    }
                }
            }

            _testOutputHelper.WriteLine(file);
        }

        [Fact]
        public void WriteOneDimensionalDataIncrementelGrow()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<double>()
                    .WithDimension(0)
                    .WithChunking(5);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    dataSet.Write(new double[] { 1, 2, 3, 4, 5 });
                    dataSet.Write(new double[] { 6, 7, 8, 9, 10 });
                }
            }

            _testOutputHelper.WriteLine(file);
        }

        [Fact]
        public void WriteTwoDimensionalDataIncrementelGrow()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<int>()
                    .WithDimension(0, 5)
                    .WithChunking(2, 5);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    dataSet.Write(new[,] { { 1, 2, 3, 4, 5 }, { 11, 12, 13, 14, 15 } });
                    dataSet.Write(new[,] { { 6, 7, 8, 9, 10 }, { 16, 17, 18, 19, 20 } });
                }
            }

            _testOutputHelper.WriteLine(file);
        }

        [Fact]
        public void AppendOneDimensionalData()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var dataSetBuilder = new H5DataSet.Builder()
                    .WithName("/data/set")
                    .WithType<int>()
                    .WithDimension(0)
                    .WithChunking(5);

                using (var dataSet = dataSetBuilder.Create(h5File))
                {
                    dataSet.Attributes().Write("Timestamp", DateTime.UtcNow.ToString("o"));

                    dataSet.AppendAll(new[] { 1, 2, 3, 4, 5 });
                    dataSet.AppendAll(new[] { 6, 7, 8, 9, 10 });
                }
            }

            _testOutputHelper.WriteLine(file);
        }
    }
}
