using AwesomeAssertions;
using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using Xunit;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils
{
    public class H5GlobalLockTest
    {
        public H5GlobalLockTest()
        {
        }

        [Fact]
        public void HasLockWithInstance()
        {
            using (new H5GlobalLock())
            {
                H5GlobalLock.HasLock.Should().BeTrue();
            }

            H5GlobalLock.HasLock.Should().BeFalse();
        }

        [Fact]
        public void HasLockWithLock()
        {
            lock (H5GlobalLock.Sync)
            {
                H5GlobalLock.HasLock.Should().BeTrue();
            }

            H5GlobalLock.HasLock.Should().BeFalse();
        }
    }
}
