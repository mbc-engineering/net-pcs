using BenchmarkDotNet.Attributes;
using System.Buffers.Binary;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper.Test
{
    /// <summary>
    /// Benchmark different ways to unmarshal binary values to managed type.
    /// 
    /// |                         Method |       Mean |     Error |    StdDev |
    /// |------------------------------- |-----------:|----------:|----------:|
    /// | TwinCAT_PrimitiveTypeMarshaler | 177.543 ns | 0.4423 ns | 0.3693 ns |
    /// |        DotNet_BinaryPrimitives |  15.457 ns | 0.3524 ns | 0.7199 ns |
    /// |            Custom_UnsafeReader |   2.017 ns | 0.0911 ns | 0.1619 ns |
    /// 
    /// </summary>
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net47)]
    public class MarshallingPerformanceTest
    {
        private readonly byte[] _data = new byte[4];
        private int _value;

        [Benchmark]
        public void TwinCAT_PrimitiveTypeMarshaler()
        {
            PrimitiveTypeMarshaler.Default.Unmarshal<int>(_data, out _value);
        }

        [Benchmark]
        public void DotNet_BinaryPrimitives()
        {
            _value = BinaryPrimitives.ReadInt32LittleEndian(_data);
        }

        [Benchmark]
        public unsafe void Custom_UnsafeReader()
        {
            fixed (byte* p = _data)
            {
                _value = *(int*)p;
            }
        }
    }
}
