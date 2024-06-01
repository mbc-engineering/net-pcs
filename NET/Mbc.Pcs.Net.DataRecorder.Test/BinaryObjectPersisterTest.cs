﻿using FluentAssertions;
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
                Byte = 15,
                UShort = 4,
                Int = -1,
                UInt = 0xFEFFFFFF,
                Enum = EnumTest.Value2,
                FloatArray = new float[] { 10, 11 },
                Float2Array = new float[,]
                {
                    { 1, 2 },
                    { 3, 4 },
                    { 5, 6 },
                },
                String = "Hallo Welt",
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
                0x0F, // Byte
                0x04, 0x00, // UShort
                0xff, 0xff, 0xff, 0xff, // Int
                0xff, 0xff, 0xff, 0xfe, // UInt
                0x01, 0x00, 0x00, 0x00, // Enum
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x30, 0x41, // Float-Array
                0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0xa0, 0x40, 0x00, 0x00, 0xc0, 0x40, // Float2-Array
                0x0a, 0x00, 0x00, 0x00, 0x48, 0x61, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x65, 0x6c, 0x74, // string
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
                0x0F, // Byte
                0x04, 0x00, // UShort
                0xff, 0xff, 0xff, 0xff, // Int
                0xff, 0xff, 0xff, 0xfe, // UInt
                0x01, 0x00, 0x00, 0x00, // Enum
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x30, 0x41, // Float-Array
                0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0xa0, 0x40, 0x00, 0x00, 0xc0, 0x40, // Float2-Array
                0x0a, 0x00, 0x00, 0x00, 0x48, 0x61, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x65, 0x6c, 0x74, // string
            });
            var persister = new BinaryObjectPersister<Values>();

            // Act
            var value = (Values)persister.Deserialize(stream);

            // Assert
            value.DateTime.Should().Be(new DateTime(1234));
            value.Bool.Should().BeTrue();
            value.Float.Should().Be(42);
            value.Byte.Should().Be(15);
            value.UShort.Should().Be(4);
            value.Int.Should().Be(-1);
            value.UInt.Should().Be(0xFEFFFFFF);
            value.Enum.Should().Be(EnumTest.Value2);
            value.FloatArray.Should().BeEquivalentTo([10, 11]);
            value.Float2Array.Should().BeEquivalentTo(new float[,]
            {
                { 1, 2 },
                { 3, 4 },
                { 5, 6 },
            });
            value.String.Should().Be("Hallo Welt");
        }

        [Fact]
        public void SerializeTypesWithNullStringValue()
        {
            // Arrange
            var value = new Values
            {
                DateTime = new DateTime(1234),
                Bool = true,
                Float = 42,
                Byte = 15,
                UShort = 4,
                Int = -1,
                UInt = 0xFEFFFFFF,
                Enum = EnumTest.Value2,
                FloatArray = new float[] { 10, 11 },
                Float2Array = new float[,]
                {
                    { 1, 2 },
                    { 3, 4 },
                    { 5, 6 },
                },
                String = null,
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
                0x0F, // Byte
                0x04, 0x00, // UShort
                0xff, 0xff, 0xff, 0xff, // Int
                0xff, 0xff, 0xff, 0xfe, // UInt
                0x01, 0x00, 0x00, 0x00, // Enum
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x30, 0x41, // Float-Array
                0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0xa0, 0x40, 0x00, 0x00, 0xc0, 0x40, // Float2-Array
                0x00, 0x00, 0x00, 0x00, // string
            });
        }

        [Fact]
        public void DeserializeTypesWithNullStringValue()
        {
            // Arrange
            var stream = new MemoryStream(new byte[]
            {
                0xd2, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // DateTime
                0x01, // Bool,
                0x00, 0x00, 0x28, 0x42, // Float
                0x0F, // Byte
                0x04, 0x00, // UShort
                0xff, 0xff, 0xff, 0xff, // Int
                0xff, 0xff, 0xff, 0xfe, // UInt
                0x01, 0x00, 0x00, 0x00, // Enum
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, 0x00, 0x00, 0x30, 0x41, // Float-Array
                0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x40, 0x40, 0x00, 0x00, 0x80, 0x40, 0x00, 0x00, 0xa0, 0x40, 0x00, 0x00, 0xc0, 0x40, // Float2-Array
                0x00, 0x00, 0x00, 0x00, // string
            });
            var persister = new BinaryObjectPersister<Values>();

            // Act
            var value = (Values)persister.Deserialize(stream);

            // Assert
            value.DateTime.Should().Be(new DateTime(1234));
            value.Bool.Should().BeTrue();
            value.Float.Should().Be(42);
            value.Byte.Should().Be(15);
            value.UShort.Should().Be(4);
            value.Int.Should().Be(-1);
            value.UInt.Should().Be(0xFEFFFFFF);
            value.Enum.Should().Be(EnumTest.Value2);
            value.FloatArray.Should().BeEquivalentTo([10, 11]);
            value.Float2Array.Should().BeEquivalentTo(new float[,]
            {
                { 1, 2 },
                { 3, 4 },
                { 5, 6 },
            });
            value.String.Should().Be(string.Empty);
        }

        internal class Values
        {
            public DateTime DateTime { get; set; }

            public bool Bool { get; set; }

            public float Float { get; set; }

            public byte Byte { get; set; }

            public ushort UShort { get; set; }

            public int Int { get; set; }

            public uint UInt { get; set; }

            public EnumTest Enum { get; set; }

            public float[] FloatArray { get; set; }

            public float[,] Float2Array { get; set; }

            public string String { get; set; }
        }

        public enum EnumTest
        {
            Value1,
            Value2,
        }
    }
}
