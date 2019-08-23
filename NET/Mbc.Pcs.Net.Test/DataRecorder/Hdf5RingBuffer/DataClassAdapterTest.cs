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
            var adapter = new DataClassAdapter<MockDataClass>(opts => opts.WithOversampling(nameof(MockDataClass.FloatPropOversampling), 2));

            // Act
            var channelInfos = adapter.CreateChannelInfos();

            // Assert
            channelInfos.Should().HaveCount(5).And.BeEquivalentTo(
                new ChannelInfo("IntProp", typeof(int)),
                new ChannelInfo("FloatProp", typeof(float)),
                new ChannelInfo("DateTimeProp", typeof(long)),
                new ChannelInfo("BoolProp", typeof(byte)),
                new ChannelInfo("FloatPropOversampling", typeof(float), 2));
        }

        [Fact]
        public void WriteDataTest()
        {
            // Arrange
            var adapter = new DataClassAdapter<MockDataClass>(opts => opts.WithOversampling(nameof(MockDataClass.FloatPropOversampling), 2));
            var dataChannelWriter = A.Fake<IDataChannelWriter>();
            var dataList = new List<MockDataClass>
            {
                new MockDataClass { IntProp = 42, FloatProp = 0.42F, DateTimeProp = DateTime.FromFileTime(1000000), BoolProp = true, FloatPropOversampling = new[] { 1F, 2F } },
                new MockDataClass { IntProp = 100, FloatProp = 4.2F, DateTimeProp = DateTime.FromFileTime(2000000), BoolProp = false, FloatPropOversampling = new[] { 3F, 4F } },
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
            A.CallTo(() => dataChannelWriter.WriteChannel("FloatPropOversampling", A<Array>.That.IsSameSequenceAs(1F, 2F, 3F, 4F ))).MustHaveHappenedOnceExactly();
        }


        [Fact]
        public void WriteLotOfData()
        {
            // Arrange
            var adapter = new DataClassAdapter<MockDataClass>(opt => opt.IgnoreProperty(nameof(MockDataClass.FloatPropOversampling)));
            var dataChannelWriter = A.Fake<IDataChannelWriter>();
            var dataList = Enumerable.Range(0, 100).Select(x => new MockDataClass { DateTimeProp = DateTime.Now }).ToList();

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

        private class MockDataClass
        {
            public int IntProp { get; set; }

            public float FloatProp { get; set; }

            public DateTime DateTimeProp { get; set; }

            public bool BoolProp { get; set; }

            public float[] FloatPropOversampling { get; set; }
        }
    }
}
