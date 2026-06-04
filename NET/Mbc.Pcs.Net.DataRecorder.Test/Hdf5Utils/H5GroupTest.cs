using System;
using System.IO;
using AwesomeAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils.Utils;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils
{
    [Collection("HDF5")]
    public class H5GroupTest : IDisposable
    {
        private readonly H5File _h5File;

        public H5GroupTest(Hdf5LibFixture hdf5Lib)
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
        public void CreateGroupRelativ()
        {
            // Arrange
            var fooGroup = _h5File.CreateGroup("/foo");

            // Act
            var subGroup = fooGroup.CreateGroup("bar");

            // Assert
            subGroup.Should().NotBeNull();
            fooGroup.OpenGroup("bar").Should().NotBeNull("because group bar should exist relativ to foo");
            _h5File.OpenGroup("/foo/bar").Should().NotBeNull("because /foo/bar should exist");
        }

        [Fact]
        public void CreateGroupAbsolute()
        {
            // Arrange
            var fooGroup = _h5File.CreateGroup("/foo");

            // Act
            var subGroup = fooGroup.CreateGroup("/bar");

            // Assert
            subGroup.Should().NotBeNull();
            _h5File.OpenGroup("/bar").Should().NotBeNull("because /bar should exist");
        }

        [Fact]
        public void ListAllNamesInGroup()
        {
            // Arrange
            var fooGroup = _h5File.CreateGroup("/foo");
            fooGroup.CreateGroup("bar").Dispose();
            fooGroup.CreateGroup("baz").Dispose();

            // Act
            var names = fooGroup.GetNames();

            // Assert
            names.Should().BeEquivalentTo("bar", "baz");
        }
    }
}
