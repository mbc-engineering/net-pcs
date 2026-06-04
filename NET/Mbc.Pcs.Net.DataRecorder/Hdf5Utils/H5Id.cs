using System;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    public class H5Id : IDisposable
    {
        private readonly Func<long, int> _closer;
        private bool _disposed;

        public H5Id(long id, Func<long, int> closer)
        {
            Id = id;
            _closer = closer;

            lock (H5GlobalLock.Sync)
            {
                if (Id < 0)
                    throw H5Error.GetExceptionFromHdf5Stack();
            }
        }

        ~H5Id()
        {
            Dispose(false);
        }

        public long Id { get; }

        public static implicit operator long(H5Id h5Id)
        {
            return h5Id.Id;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;

            // See H5Group.Dispose: skip native HDF5 calls from the finalizer.
            if (!disposing)
                return;

            lock (H5GlobalLock.Sync)
            {
                var ret = _closer(Id);
                H5Error.CheckH5Result(ret);
            }
        }
    }
}
