using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    public sealed class H5File : IDisposable
    {
        [Flags]
        public enum Flags : uint
        {
            ReadOnly = H5F.ACC_RDONLY,
            ReadWrite = H5F.ACC_RDWR,
            Overwrite = H5F.ACC_TRUNC,
            CreateOnly = H5F.ACC_EXCL,
            OpenOrCreate = H5F.ACC_CREAT,
        }

        private bool _disposed;

        public H5File(string filename, Flags flags)
        {
            lock (H5GlobalLock.Sync)
            {
                if ((flags & (Flags.CreateOnly | Flags.OpenOrCreate)) != 0)
                {
                    Id = H5Error.CheckH5Result(H5F.create(filename, (uint)flags));
                }
                else
                {
                    Id = H5Error.CheckH5Result(H5F.open(filename, (uint)flags));
                }
            }
        }

        ~H5File()
        {
            Dispose(false);
        }

        public string GetName()
        {
            lock (H5GlobalLock.Sync)
            {
                var len = H5Error.CheckH5Result(H5F.get_name(Id, null, IntPtr.Zero).ToInt32());
                var name = new StringBuilder(len + 1);
                var ret = H5F.get_name(Id, name, new IntPtr(name.Capacity));
                H5Error.CheckH5Result(ret.ToInt32());

                return name.ToString();
            }
        }

        internal long Id { get; }

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

            lock (H5GlobalLock.Sync)
            {
                var ret = H5F.close(Id);

                if (disposing)
                {
                    H5Error.CheckH5Result(ret);
                }
            }
        }

        public void Flush()
        {
            lock (H5GlobalLock.Sync)
            {
                var ret = H5F.flush(Id, H5F.scope_t.GLOBAL);
                H5Error.CheckH5Result(ret);
            }
        }

        public H5Group OpenGroup(string name)
        {
            lock (H5GlobalLock.Sync)
            {
                var groupId = H5Error.CheckH5Result(H5G.open(Id, name));
                return new H5Group(groupId);
            }
        }

        public H5Group CreateGroup(string name)
        {
            H5Group.CreateMissingGroups(Id, name);
            return OpenGroup(name);
        }

        public void MakeGroups(string name)
        {
            H5Group.CreateMissingGroups(Id, name);
        }

        public IEnumerable<string> GetNames()
        {
            return new H5LinkIterator(Id).Names;
        }
    }
}
