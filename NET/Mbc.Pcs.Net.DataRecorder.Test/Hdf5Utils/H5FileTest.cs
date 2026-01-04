using System;
using System.IO;
using AwesomeAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils.Utils;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils
{
    [Collection("HDF5")]
    public class H5FileTest
    {
        public H5FileTest(Hdf5LibFixture hdf5Lib)
        {
        }

        [Fact]
        public void NewHdf5IsCreated()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            var h5file = new H5File(file, H5File.Flags.CreateOnly);
            var name = h5file.GetName();
            h5file.Dispose();

            name.Should().Be(file);
            File.Exists(file).Should().BeTrue();
        }

        [Fact]
        public void OpenFailsIfExist()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Create(file).Dispose();

            try
            {
#pragma warning disable CA1806 // Do not ignore method results
                Action act = () => new H5File(file, H5File.Flags.CreateOnly);
#pragma warning restore CA1806 // Do not ignore method results

                act.Should().Throw<H5Error>()
                    .WithMessage("H5Fcreate: unable to create file -> File accessibilty - Unable to open file");
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void OpenCurrentName()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                using (var group = h5file.OpenGroup("."))
                {
                    group.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void OpenRootName()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                using (var group = h5file.OpenGroup("/"))
                {
                    group.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void OpenNotExistingName()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                Action act = () => h5file.OpenGroup("foo");

                act.Should().Throw<H5Error>()
                    .WithMessage("H5Gopen2: unable to open group -> Symbol table - Can't open object");
            }
        }

        [Fact]
        public void CreateNotExistingName()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                using (var group = h5file.CreateGroup("/foo"))
                {
                    group.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void CreateAndOpen()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                using (var group = h5file.CreateGroup("/foo/bar"))
                {
                    group.Should().NotBeNull();
                }

                using (var group = h5file.OpenGroup("foo/bar"))
                {
                    group.Should().NotBeNull();
                }
            }
        }

        [Fact]
        public void ListAllGroupNames()
        {
            var file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Delete(file);
            using (var h5file = new H5File(file, H5File.Flags.CreateOnly))
            {
                h5file.CreateGroup("/foo").Dispose();
                h5file.CreateGroup("/bar").Dispose();

                h5file.GetNames().Should().BeEquivalentTo("foo", "bar");
            }
        }
    }
}
