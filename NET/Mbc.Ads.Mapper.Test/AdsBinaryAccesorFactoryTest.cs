using FakeItEasy;
using FluentAssertions;
using System;
using System.Text;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;
using Xunit;

namespace Mbc.Ads.Mapper.Test
{
    public class AdsBinaryAccesorFactoryTest
    {
        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithBool_CanReadBoolean()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(bool), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0 })).Should().BeOfType<bool>().Which.Should().BeFalse();
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 1 })).Should().BeOfType<bool>().Which.Should().BeTrue();
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithByte_CanReadBytes()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(byte), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0 })).Should().BeOfType<byte>().Which.Should().Be(0);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255 })).Should().BeOfType<byte>().Which.Should().Be(255);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithSByte_CanReadSBytes()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(sbyte), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 128 })).Should().BeOfType<sbyte>().Which.Should().Be(-128);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 127 })).Should().BeOfType<sbyte>().Which.Should().Be(127);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithUShort_CanReadUShort()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(ushort), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0 })).Should().BeOfType<ushort>().Which.Should().Be(ushort.MinValue);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 255 })).Should().BeOfType<ushort>().Which.Should().Be(ushort.MaxValue);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithShort_CanReadShort()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(short), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 128 })).Should().BeOfType<short>().Which.Should().Be(-32768);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 127 })).Should().BeOfType<short>().Which.Should().Be(32767);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithUInt_CanReadUInt()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(uint), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0 })).Should().BeOfType<uint>().Which.Should().Be(0);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 255, 255, 255 })).Should().BeOfType<uint>().Which.Should().Be(4294967295);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithInt_CanReadInt()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(int), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 128 })).Should().BeOfType<int>().Which.Should().Be(-2147483648);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 255, 255, 127 })).Should().BeOfType<int>().Which.Should().Be(2147483647);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithFloat_CanReadFloat()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(float), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0 }))
                .Should().BeOfType<float>().Which.Should().Be(0);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0xFF, 0xFF, 0x7F, 0x7F }))
                .Should().BeOfType<float>().Which.Should().Be(float.MaxValue);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0xFF, 0xFF, 0x7F, 0xFF }))
                .Should().BeOfType<float>().Which.Should().Be(float.MinValue);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0x01, 0, 0, 0 }))
                .Should().BeOfType<float>().Which.Should().Be(float.Epsilon);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0x01, 0, 0, 0x80 }))
                .Should().BeOfType<float>().Which.Should().Be(-float.Epsilon);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0x80, 0x7f }))
                .Should().BeOfType<float>().Which.Should().Be(float.PositiveInfinity);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0x80, 0xff }))
                .Should().BeOfType<float>().Which.Should().Be(float.NegativeInfinity);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0xFF, 0xFF, 0xff, 0x7f }))
                .Should().BeOfType<float>().Which.Should().Be(float.NaN);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithDouble_CanReadDouble()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(double), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }))
                .Should().BeOfType<double>().Which.Should().Be(0);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F }))
                .Should().BeOfType<double>().Which.Should().Be(double.MaxValue);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0xFF }))
                .Should().BeOfType<double>().Which.Should().Be(double.MinValue);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0x01, 0, 0, 0, 0, 0, 0, 0 }))
                .Should().BeOfType<double>().Which.Should().Be(double.Epsilon);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0x01, 0, 0, 0, 0, 0, 0, 0x80 }))
                .Should().BeOfType<double>().Which.Should().Be(-double.Epsilon);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0xF0, 0x7f }))
                .Should().BeOfType<double>().Which.Should().Be(double.PositiveInfinity);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0xF0, 0xff }))
                .Should().BeOfType<double>().Which.Should().Be(double.NegativeInfinity);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0xff, 0, 0, 0, 0, 0, 0xf0, 0x7f }))
                .Should().BeOfType<double>().Which.Should().Be(double.NaN);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithTime_CanReadTime()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(TIME), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0 }))
                .Should().BeOfType<TimeSpan>().Which.Should().Be(new TIME(0).Time);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 255, 255, 255 }))
                .Should().BeOfType<TimeSpan>().Which.Should().Be(new TIME(uint.MaxValue).Time);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithDate_CanReadDate()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(DATE), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0 }))
                .Should().BeOfType<DateTime>().Which.Should().Be(new DATE(0).Date);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 255, 255, 255 }))
                .Should().BeOfType<DateTime>().Which.Should().Be(new DATE(uint.MaxValue).Date);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithDT_CanReadDT()
        {
            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(DT), 1, null);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0 }))
                .Should().BeOfType<DateTime>().Which.Should().Be(new DT(0).DateTime);
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 255, 255, 255, 255 }))
                .Should().BeOfType<DateTime>().Which.Should().Be(new DT(uint.MaxValue).DateTime);
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithASCIIString_CanReadString()
        {
            var fakeStringType = A.Fake<IStringType>();
            A.CallTo(() => fakeStringType.Encoding).Returns(Encoding.ASCII);
            A.CallTo(() => fakeStringType.Length).Returns(5);
            A.CallTo(() => fakeStringType.ByteSize).Returns(6);
            A.CallTo(() => fakeStringType.IsFixedLength).Returns(true);

            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(string), 1, fakeStringType);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 65, 66, 67, 0, 0, 0 }))
                .Should().BeOfType<string>().Which.Should().Be("ABC");
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0 }))
                .Should().BeOfType<string>().Which.Should().BeEmpty();
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 65, 66, 67, 68, 69, 0 }))
                .Should().BeOfType<string>().Which.Should().Be("ABCDE");
        }

        [Fact]
        public void CreatePrimitiveTypeReadFunction_WithUTF16String_CanReadString()
        {
            var fakeStringType = A.Fake<IStringType>();
            A.CallTo(() => fakeStringType.Encoding).Returns(Encoding.Unicode);
            A.CallTo(() => fakeStringType.Length).Returns(5);
            A.CallTo(() => fakeStringType.ByteSize).Returns(10);
            A.CallTo(() => fakeStringType.IsFixedLength).Returns(true);

            IAdsDataReader reader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(typeof(string), 1, fakeStringType);

            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0x41, 0, 0x42, 0, 0x43, 0, 0, 0, 0xFF, 0xFF }))
                .Should().BeOfType<string>().Which.Should().Be("ABC");
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }))
                .Should().BeOfType<string>().Which.Should().BeEmpty();
            reader.Read(new ReadOnlySpan<byte>(new byte[] { 0, 0x41, 0, 0x42, 0, 0x43, 0, 0x44, 0, 0x45, 0 }))
                .Should().BeOfType<string>().Which.Should().Be("ABCDE");
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithBool_CanWriteBoolean()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(bool), 1, null);

            WriteAndReturn<bool>(writer, true, 2).Should().Equal(0, 1);
            WriteAndReturn<bool>(writer, false, 2).Should().Equal(0, 0);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithByte_CanWriteByte()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(byte), 1, null);

            WriteAndReturn<byte>(writer, 0, 2).Should().Equal(0, 0);
            WriteAndReturn<byte>(writer, 255, 2).Should().Equal(0, 255);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithSByte_CanWriteSByte()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(sbyte), 1, null);

            WriteAndReturn<sbyte>(writer, -128, 2).Should().Equal(0, 128);
            WriteAndReturn<sbyte>(writer, 127, 2).Should().Equal(0, 127);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithUShort_CanWriteUShort()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(ushort), 1, null);

            WriteAndReturn<ushort>(writer, ushort.MinValue, 3).Should().Equal(0, 0, 0);
            WriteAndReturn<ushort>(writer, ushort.MaxValue, 3).Should().Equal(0, 255, 255);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithShort_CanWriteShort()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(short), 1, null);

            WriteAndReturn<short>(writer, short.MinValue, 3).Should().Equal(0, 0, 128);
            WriteAndReturn<short>(writer, short.MaxValue, 3).Should().Equal(0, 255, 127);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithUInt_CanWriteUInt()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(uint), 1, null);

            WriteAndReturn<uint>(writer, uint.MinValue, 5).Should().Equal(0, 0, 0, 0, 0);
            WriteAndReturn<uint>(writer, uint.MaxValue, 5).Should().Equal(0, 255, 255, 255, 255);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithInt_CanWriteInt()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(int), 1, null);

            WriteAndReturn<int>(writer, int.MinValue, 5).Should().Equal(0, 0, 0, 0, 128);
            WriteAndReturn<int>(writer, int.MaxValue, 5).Should().Equal(0, 255, 255, 255, 127);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithFloat_CanWriteFloat()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(float), 1, null);

            WriteAndReturn<float>(writer, 0, 5).Should().Equal(0, 0, 0, 0, 0);
            WriteAndReturn<float>(writer, float.MaxValue, 5).Should().Equal(0, 0xFF, 0xFF, 0x7F, 0x7F);
            WriteAndReturn<float>(writer, float.MinValue, 5).Should().Equal(0, 0xFF, 0xFF, 0x7F, 0xFF);
            WriteAndReturn<float>(writer, float.Epsilon, 5).Should().Equal(0, 0x01, 0, 0, 0);
            WriteAndReturn<float>(writer, -float.Epsilon, 5).Should().Equal(0, 0x01, 0, 0, 0x80);
            WriteAndReturn<float>(writer, float.PositiveInfinity, 5).Should().Equal(0, 0, 0, 0x80, 0x7f);
            WriteAndReturn<float>(writer, float.NegativeInfinity, 5).Should().Equal(0, 0, 0, 0x80, 0xff);
            WriteAndReturn<float>(writer, float.NaN, 5).Should().Equal(0, 0, 0, 0xc0, 0xff);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithDouble_CanWriteDouble()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(double), 1, null);

            WriteAndReturn<double>(writer, 0, 9).Should().Equal(0, 0, 0, 0, 0, 0, 0, 0, 0);
            WriteAndReturn<double>(writer, double.MaxValue, 9).Should().Equal(0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0x7F);
            WriteAndReturn<double>(writer, double.MinValue, 9).Should().Equal(0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xEF, 0xFF);
            WriteAndReturn<double>(writer, double.Epsilon, 9).Should().Equal(0, 0x01, 0, 0, 0, 0, 0, 0, 0);
            WriteAndReturn<double>(writer, -double.Epsilon, 9).Should().Equal(0, 0x01, 0, 0, 0, 0, 0, 0, 0x80);
            WriteAndReturn<double>(writer, double.PositiveInfinity, 9).Should().Equal(0, 0, 0, 0, 0, 0, 0, 0xF0, 0x7f);
            WriteAndReturn<double>(writer, double.NegativeInfinity, 9).Should().Equal(0, 0, 0, 0, 0, 0, 0, 0xF0, 0xff);
            WriteAndReturn<double>(writer, double.NaN, 9).Should().Equal(0, 0, 0, 0, 0, 0, 0, 0xf8, 0xff);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithTime_CanWriteTime()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(TIME), 1, null);

            WriteAndReturn<TimeSpan>(writer, new TIME(uint.MinValue).Time, 5).Should().Equal(0, 0, 0, 0, 0);
            WriteAndReturn<TimeSpan>(writer, new TIME(uint.MaxValue).Time, 5).Should().Equal(0, 255, 255, 255, 255);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithDate_CanWriteDate()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(DATE), 1, null);

            WriteAndReturn<DateTime>(writer, new DATE(uint.MinValue).Date, 5).Should().Equal(0, 0, 0, 0, 0);
            // DATE contains date without time, so the serialization of max value truncates to the date
            WriteAndReturn<DateTime>(writer, new DATE(uint.MaxValue).Date, 5).Should().Equal(0, 0x00, 0xA5, 255, 255);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithDT_CanWriteDT()
        {
            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(DT), 1, null);

            WriteAndReturn<DateTime>(writer, new DT(uint.MinValue).DateTime, 5).Should().Equal(0, 0, 0, 0, 0);
            WriteAndReturn<DateTime>(writer, new DT(uint.MaxValue).DateTime, 5).Should().Equal(0, 255, 255, 255, 255);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithASCIIString_CanWriteASCIIString()
        {
            var fakeStringType = A.Fake<IStringType>();
            A.CallTo(() => fakeStringType.Encoding).Returns(Encoding.ASCII);
            A.CallTo(() => fakeStringType.Length).Returns(5);
            A.CallTo(() => fakeStringType.ByteSize).Returns(6);
            A.CallTo(() => fakeStringType.IsFixedLength).Returns(true);

            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(string), 1, fakeStringType);

            WriteAndReturn<string>(writer, "ABC", 7).Should().Equal(0, 65, 66, 67, 0, 0, 0);
            WriteAndReturn<string>(writer, string.Empty, 7).Should().Equal(0, 0, 0, 0, 0, 0, 0);
            WriteAndReturn<string>(writer, "ABCDE", 7).Should().Equal(0, 65, 66, 67, 68, 69, 0);
        }

        [Fact]
        public void CreatePrimitiveTypeWriteFunction_WithUTF16String_CanWriteUTF16String()
        {
            var fakeStringType = A.Fake<IStringType>();
            A.CallTo(() => fakeStringType.Encoding).Returns(Encoding.Unicode);
            A.CallTo(() => fakeStringType.Length).Returns(5);
            A.CallTo(() => fakeStringType.ByteSize).Returns(12);
            A.CallTo(() => fakeStringType.IsFixedLength).Returns(true);

            IAdsDataWriter writer = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(typeof(string), 1, fakeStringType);

            WriteAndReturn<string>(writer, "ABC", 13).Should().Equal(0, 65, 0, 66, 0, 67, 0, 0, 0, 0, 0, 0, 0);
            WriteAndReturn<string>(writer, string.Empty, 13).Should().Equal(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            WriteAndReturn<string>(writer, "ABCDE", 13).Should().Equal(0, 65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 0, 0);
        }

        private byte[] WriteAndReturn<T>(IAdsDataWriter writer, T writeValue, int length)
        {
            var buffer = new byte[length];
            writer.Write((T)writeValue, new Span<byte>(buffer));
            return buffer;
        }
    }
}
