using System;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils.Utils;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils
{
    [Collection("HDF5")]
    public class H5AttributeTest : IDisposable
    {
        private readonly H5File _h5File;

        public H5AttributeTest(Hdf5LibFixture hdf5Lib)
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            _h5File = new H5File(file, H5File.Flags.CreateOnly);
        }

        public void Dispose()
        {
            _h5File.Dispose();
        }

        [Fact]
        public void WriteAndReadStringAttribute()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);

            // Act
            attribute.Write("foo", "bar");
            var attrValue = attribute.ReadString("foo");

            // Assert
            attrValue.Should().Be("bar");
        }

        [Fact]
        public void WriteAndReadIntAttribute()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);

            // Act
            attribute.Write("foo", 42);
            var attrValue = attribute.ReadInt("foo");

            // Assert
            attrValue.Should().Be(42);
        }

        [Fact]
        public void WriteAndReadDoubleAttribute()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);

            // Act
            attribute.Write("foo", 42d);
            var attrValue = attribute.ReadDouble("foo");

            // Assert
            attrValue.Should().Be(42d);
        }

        [Fact]
        public void WriteShortAndReadIntAttribute()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);

            // Act
            attribute.Write("foo", (ushort)60000);
            var attrValue = attribute.ReadInt("foo");

            // Assert
            attrValue.Should().Be(60000);
        }

        [Fact]
        public void ReadObjectAttribute()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);
            attribute.Write("foo", "bar");

            // Act
            var attrValue = attribute.Read("foo");

            // Assert
            attrValue.Should().BeAssignableTo<string>().Which.Should().Be("bar");
        }

        [Fact]
        public void ListAttributeNames()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);
            attribute.Write("foo", "bar");
            attribute.Write("baz", "bar");

            // Act
            var names = attribute.GetAttributeNames().ToList();

            // Assert
            names.Should().BeEquivalentTo("foo", "baz");
        }

        [Fact]
        public void ReWriteAttribute()
        {
            // Arrange
            var attribute = new H5Attribute(_h5File);
            attribute.Write("foo", 1);

            // Act
            attribute.Write("foo", 2);

            // Assert
            attribute.ReadInt("foo").Should().Be(2);
        }
    }
}
