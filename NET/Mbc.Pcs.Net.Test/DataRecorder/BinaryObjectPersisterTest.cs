using FluentAssertions;
using Mbc.Pcs.Net.DataRecorder;
using System;
using System.IO;
using Xunit;

namespace Mbc.Pcs.Net.Test.DataRecorder
{
    public class BinaryObjectPersisterTest
    {
        [Fact]
        public void SerializeTypes()
        {
            // Arrange
            var value = new Values
            {
                DateTime = new DateTime(1234),
                Bool = true,
                Float = 42,
                UShort = 4,
                Int = -1,
                Enum = EnumTest.Value2,
                FloatArray = new float[] { 10, 11 },
            };
            var persister = new BinaryObjectPersister<Values>();
            var stream = new MemoryStream();

            // Act
            persister.Serialize(value, stream);
            stream.Position = 0;

            // Assert
            stream.ToArray().Should().BeEquivalentTo(new byte[]
            {
                0xd2, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // DateTime
                0x01, // Bool,
                0x00, 0x00, 0x28, 0x42, // Float
                0x04, 0x00, // UShort
                0xff, 0xff, 0xff, 0xff, // Int
                0x01, 0x00, 0x00, 0x00, // Enum
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x30, 0x41, // Float-Array
            });
        }

        [Fact]
        public void DeserializeTypes()
        {
            // Arrange
            var stream = new MemoryStream(new byte[]
            {
                0xd2, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // DateTime
                0x01, // Bool,
                0x00, 0x00, 0x28, 0x42, // Float
                0x04, 0x00, // UShort
                0xff, 0xff, 0xff, 0xff, // Int
                0x01, 0x00, 0x00, 0x00, // Enum
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x30, 0x41, // Float-Array
            });
            var persister = new BinaryObjectPersister<Values>();

            // Act
            var value = (Values)persister.Deserialize(stream);

            // Assert
            value.DateTime.Should().Be(new DateTime(1234));
            value.Bool.Should().BeTrue();
            value.Float.Should().Be(42);
            value.UShort.Should().Be(4);
            value.Int.Should().Be(-1);
            value.Enum.Should().Be(EnumTest.Value2);
            value.FloatArray.Should().BeEquivalentTo(10, 11);
        }

        internal class Values
        {
            public DateTime DateTime { get; set; }

            public bool Bool { get; set; }

            public float Float { get; set; }

            public ushort UShort { get; set; }

            public int Int { get; set; }

            public EnumTest Enum { get; set; }

            public float[] FloatArray { get; set; }
        }

        public enum EnumTest
        {
            Value1,
            Value2,
        }
    }
}
