using Mbc.Pcs.Net.DataRecorder;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Profiling
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string bufferPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(bufferPath);
            FileRingBuffer buffer = new FileRingBuffer(bufferPath, 100000, 2, persister: new CustomObjectPersister());

            for (int i = 0; i < 100000; i++)
            {
                buffer.AppendData(new ValueHolder());
            }

            buffer.Dispose();
            buffer = new FileRingBuffer(bufferPath, 100000, 2, persister: new CustomObjectPersister());
            var stopWatch = Stopwatch.StartNew();
            var bufferedData = buffer.ReadData(0).ToList();
            stopWatch.Stop();
            Console.WriteLine($"{stopWatch.Elapsed} für {bufferedData.Count}");
            Console.ReadKey();
        }

        internal class ValueHolder
        {
            public int Value1 { get; set; }
            public float Value2 { get; set; }
            public bool Value3 { get; set; }
            public int[] Value4 { get; set; } = new int[20];
            public float[] Value5 { get; set; } = new float[100];
        }

        internal class CustomObjectPersister : IObjectPersister
        {
            public object Deserialize(Stream stream)
            {
                var vh = new ValueHolder();
                var reader = new BinaryReader(stream);
                vh.Value1 = reader.ReadInt32();
                vh.Value2 = reader.ReadSingle();
                vh.Value3 = reader.ReadBoolean();
                var count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    vh.Value4[i] = reader.ReadInt32();
                }

                count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    vh.Value5[i] = reader.ReadSingle();
                }

                return vh;
            }

            public void Serialize(object data, Stream stream)
            {
                ValueHolder vh = data as ValueHolder;
                var writer = new BinaryWriter(stream);
                writer.Write(vh.Value1);
                writer.Write(vh.Value2);
                writer.Write(vh.Value3);
                writer.Write(vh.Value4.Length);
                foreach (var item in vh.Value4)
                {
                    writer.Write(item);
                }

                writer.Write(vh.Value5.Length);
                foreach (var item in vh.Value5)
                {
                    writer.Write(item);
                }

                writer.Flush();
            }
        }
    }
}
