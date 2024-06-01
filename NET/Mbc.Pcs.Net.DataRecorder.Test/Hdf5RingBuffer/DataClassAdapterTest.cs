using FakeItEasy;
using FluentAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Mbc.Pcs.Net.Test.DataRecorder.Hdf5RingBuffer
{
    public class DataClassAdapterTest
    {
        private readonly ITestOutputHelper _testOutput;

        public DataClassAdapterTest(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        [Fact]
        public void ChannelInfoTest()
        {
            // Arrange
            var adapter = new DataClassAdapter<MockDataClass>(opts => opts
                .WithOversampling(nameof(MockDataClass.FloatPropOversampling), 2)
                .WithMulti(nameof(MockDataClass.MultiProp), 1, 2)
                .WithOversampling(nameof(MockDataClass.FloatMultiOversampling), 4)
                .WithMulti(nameof(MockDataClass.FloatMultiOversampling), 1, 2)
                .WithMulti(nameof(MockDataClass.MultiBoolProp), 1, 2));

            // Act
            var channelInfos = adapter.CreateChannelInfos();

            // Assert
            channelInfos.Should().HaveCount(11).And.BeEquivalentTo([
                new ChannelInfo("IntProp", typeof(int)),
                new ChannelInfo("FloatProp", typeof(float)),
                new ChannelInfo("DateTimeProp", typeof(long)),
                new ChannelInfo("BoolProp", typeof(byte)),
                new ChannelInfo("FloatPropOversampling", typeof(float), 2),
                new ChannelInfo("MultiProp[1]", typeof(int)),
                new ChannelInfo("MultiProp[2]", typeof(int)),
                new ChannelInfo("FloatMultiOversampling[1]", typeof(float), 4),
                new ChannelInfo("FloatMultiOversampling[2]", typeof(float), 4),
                new ChannelInfo("MultiBoolProp[1]", typeof(byte)),
                new ChannelInfo("MultiBoolProp[2]", typeof(byte)),
                ]);
        }

#pragma warning disable CA1814
        [Fact]
        public void WriteDataTest()
        {
            // Arrange
            var adapter = new DataClassAdapter<MockDataClass>(opts => opts
                .WithOversampling(nameof(MockDataClass.FloatPropOversampling), 2)
                .WithMulti(nameof(MockDataClass.MultiProp), 1, 2)
                .WithOversampling(nameof(MockDataClass.FloatMultiOversampling), 4)
                .WithMulti(nameof(MockDataClass.FloatMultiOversampling), 1, 2)
                .WithMulti(nameof(MockDataClass.MultiBoolProp), 1, 2));
            var dataChannelWriter = A.Fake<IDataChannelWriter>();
            var dataList = new List<MockDataClass>
            {
                new MockDataClass
                {
                    IntProp = 42,
                    FloatProp = 0.42F,
                    DateTimeProp = DateTime.FromFileTime(1000000),
                    BoolProp = true,
                    FloatPropOversampling = new[] { 1F, 2F },
                    MultiProp = new[] { 10, 20 },
                    FloatMultiOversampling = new[,]
                    {
                        { 1F, 2F, 3F, 4F },
                        { 10F, 20F, 30F, 40F },
                    },
                    MultiBoolProp = new[] { true, false },
                },
                new MockDataClass
                {
                    IntProp = 100,
                    FloatProp = 4.2F,
                    DateTimeProp = DateTime.FromFileTime(2000000),
                    BoolProp = false, FloatPropOversampling = new[] { 3F, 4F },
                    MultiProp = new[] { 11, 21 },
                    FloatMultiOversampling = new[,]
                    {
                        { 5F, 6F, 7F, 8F },
                        { 50F, 60F, 70F, 80F },
                    },
                    MultiBoolProp = new[] { false, true },
                },
            };

            // Act
            var watch = Stopwatch.StartNew();
            adapter.WriteData(dataList, dataChannelWriter);
            watch.Stop();
            _testOutput.WriteLine("Zeit: " + watch.Elapsed);

            // Assert
            A.CallTo(() => dataChannelWriter.WriteChannel("IntProp", A<Array>.That.IsSameSequenceAs(42, 100))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("FloatProp", A<Array>.That.IsSameSequenceAs(0.42F, 4.2F))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("DateTimeProp", A<Array>.That.IsSameSequenceAs(1000000L, 2000000L))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("BoolProp", A<Array>.That.IsSameSequenceAs((byte)1, (byte)0))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("FloatPropOversampling", A<Array>.That.IsSameSequenceAs(1F, 2F, 3F, 4F))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("MultiProp[1]", A<Array>.That.IsSameSequenceAs(10, 11))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("MultiProp[2]", A<Array>.That.IsSameSequenceAs(20, 21))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("FloatMultiOversampling[1]", A<Array>.That.IsSameSequenceAs(1F, 2F, 3F, 4F, 5F, 6F, 7F, 8F))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("FloatMultiOversampling[2]", A<Array>.That.IsSameSequenceAs(10F, 20F, 30F, 40F, 50F, 60F, 70F, 80F))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("MultiBoolProp[1]", A<Array>.That.IsSameSequenceAs((byte)1, (byte)0))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("MultiBoolProp[2]", A<Array>.That.IsSameSequenceAs((byte)0, (byte)1))).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void WriteLotOfData()
        {
            // Arrange
            var adapter = new DataClassAdapter<MockDataClass>(opt => opt
                .IgnoreProperty(nameof(MockDataClass.FloatPropOversampling))
                .IgnoreProperty(nameof(MockDataClass.MultiProp))
                .IgnoreProperty(nameof(MockDataClass.FloatMultiOversampling))
                .IgnoreProperty(nameof(MockDataClass.MultiBoolProp)));
            var dataChannelWriter = A.Fake<IDataChannelWriter>();
            var dataList = Enumerable.Range(0, 100).Select(x => new MockDataClass()).ToList();

            // Act
            var watch = Stopwatch.StartNew();
            adapter.WriteData(dataList, dataChannelWriter);
            watch.Stop();
            _testOutput.WriteLine("Zeit: " + watch.Elapsed);

            // Assert
            A.CallTo(() => dataChannelWriter.WriteChannel("IntProp", A<Array>.That.Matches(x => x.Length == 100))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("FloatProp", A<Array>.That.Matches(x => x.Length == 100))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("DateTimeProp", A<Array>.That.Matches(x => x.Length == 100))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("BoolProp", A<Array>.That.Matches(x => x.Length == 100))).MustHaveHappenedOnceExactly();
            A.CallTo(() => dataChannelWriter.WriteChannel("IntProp", A<Array>.That.Matches(x => x.Length == 100))).MustHaveHappenedOnceExactly();
        }

        [Fact(Skip = "not implemented")]
        public void ReadDataTest()
        {
            // Arrange
            var adapter = new DataClassAdapter<MockDataClass>(opts => opts
                .WithOversampling(nameof(MockDataClass.FloatPropOversampling), 2)
                .WithMulti(nameof(MockDataClass.MultiProp), 1, 2)
                .WithOversampling(nameof(MockDataClass.FloatMultiOversampling), 4)
                .WithMulti(nameof(MockDataClass.FloatMultiOversampling), 1, 2)
                .WithMulti(nameof(MockDataClass.MultiBoolProp), 1, 2));
            var dataChannelWriter = A.Fake<IDataChannelWriter>();

            // Act
            var result = adapter.ReadData(dataChannelWriter, 1, 2);

            // Assert
            result.Should().BeEquivalentTo([
                new MockDataClass
                {
                    IntProp = 42,
                    FloatProp = 0.42F,
                    DateTimeProp = DateTime.FromFileTime(1000000),
                    BoolProp = true,
                    FloatPropOversampling = new[] { 1F, 2F },
                    MultiProp = new[] { 10, 20 },
                    FloatMultiOversampling = new[,]
                    {
                        { 1F, 2F, 3F, 4F },
                        { 10F, 20F, 30F, 40F },
                    },
                    MultiBoolProp = new[] { true, false },
                },
                new MockDataClass
                {
                    IntProp = 42,
                    FloatProp = 0.42F,
                    DateTimeProp = DateTime.FromFileTime(1000000),
                    BoolProp = true,
                    FloatPropOversampling = new[] { 1F, 2F },
                    MultiProp = new[] { 10, 20 },
                    FloatMultiOversampling = new[,]
                    {
                        { 1F, 2F, 3F, 4F },
                        { 10F, 20F, 30F, 40F },
                    },
                    MultiBoolProp = new[] { true, false },
                },
                ]);
        }

        private class MockDataClass
        {
            public int IntProp { get; set; }

            public float FloatProp { get; set; }

            public DateTime DateTimeProp { get; set; } = DateTime.FromFileTime(0);

            public bool BoolProp { get; set; }

            public float[] FloatPropOversampling { get; set; }

            public int[] MultiProp { get; set; } = new int[2];

            public float[,] FloatMultiOversampling { get; set; } = new float[2, 5];

            public bool[] MultiBoolProp { get; set; } = new bool[2];
        }
    }
}
