using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils.Prototype
{
    [Collection("HDF5")]
    public class SingleChannelWrite
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public SingleChannelWrite(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void WritePerformanceTest()
        {
            var file = GetFile();
            using (var h5File = new H5File(file, H5File.Flags.CreateOnly))
            {
                var fileAttr = new H5Attribute(h5File);
                fileAttr.Write("testCycleName", "Abnahmezyklus");
                fileAttr.Write("measurementName", "Warmup 50%");

                var channelNames = new[] 
                {
                    "ActualSpeed", "SetSpeed", "MotorCurrent", "MotorTorque", "OutputVoltage",
                    "BearingTemp[1]", "BearingTemp[2]", "BearingTemp[3]", "BearingTemp[4]", "BearingTemp[5]",
                    "MotorTemp[3]", "MotorTemp[4]",
                };

                var startTime = DateTime.UtcNow;

                var dataSets = new H5DataSet[channelNames.Length];
                for (int i = 0; i < channelNames.Length; i++)
                {
                    var dataSet = new H5DataSet.Builder()
                        .WithName($"/{channelNames[i]}")
                        .WithType<float>()
                        .WithDimension(0)
                        .WithChunking(300)
                        .WithDeflate(5)
                        .Create(h5File);

                    var attr = new H5Attribute(dataSet);
                    attr.Write("sampleRate", 5);
                    attr.Write("startTimeTc", startTime.ToString("o"));

                    dataSets[i] = dataSet;
                }

                // Simuliertes Logging
                var rnd = new Random();
                var watch = Stopwatch.StartNew();

                for (var sample = 0; sample < 1500; sample++)
                {
                    var sampleValues = Enumerable.Range(0, channelNames.Length)
                        .Select(x => (float)rnd.NextDouble())
                        .ToArray();

                    for (int i = 0; i < channelNames.Length; i++)
                    {
                        dataSets[i].Write(new[] { sampleValues[i] });
                    }
                }

                watch.Stop();

                foreach (var dataSet in dataSets)
                {
                    dataSet.Dispose();
                }

                _testOutputHelper.WriteLine($"Laufzeit: {watch.Elapsed}");
            }

            _testOutputHelper.WriteLine(file);
        }

        private string GetFile()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            return file;
        }
    }
}
