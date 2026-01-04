using HDF.PInvoke;
using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    public class H5Group : IDisposable
    {
        private bool _disposed;

        internal H5Group(long groupId)
        {
            Id = groupId;
        }

        ~H5Group()
        {
            Dispose(false);
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
                var ret = H5G.close(Id);
                if (disposing)
                {
                    H5Error.CheckH5Result(ret);
                }
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

        public IEnumerable<string> GetNames()
        {
            return new H5LinkIterator(Id).Names;
        }

        internal static void CreateMissingGroups(long id, string name)
        {
            // Mögliche Namen:
            // / -> file
            // /foo/bar -> file -> group(foo) -> group(bar)
            // foo/bar -> current -> group(foo) -> group(bar)
            // ./foo/bar -> current -> group(foo) -> group(bar)

            // Startpunkt festlegen
            var curName = string.Empty;
            var remainingName = name;
            if (remainingName.StartsWith("/"))
            {
                remainingName = remainingName.Substring(1);
            }
            else if (remainingName.StartsWith("."))
            {
                remainingName = remainingName.Substring(1);
                curName = ".";
            }
            else
            {
                curName = ".";
            }

            while (remainingName.Length > 0)
            {
                var idx = remainingName.IndexOf('/');
                if (idx > 0)
                {
                    curName += $"/{remainingName.Substring(0, idx)}";
                    remainingName = remainingName.Substring(idx + 1);
                }
                else
                {
                    curName += $"/{remainingName}";
                    remainingName = string.Empty;
                }

                lock (H5GlobalLock.Sync)
                {
                    if (H5Error.CheckH5Result(H5L.exists(id, curName)) == 0)
                    {
                        using (var newid = new H5Id(H5G.create(id, curName), H5G.close))
                        {
                            // nothing to do, only create
                        }
                    }
                }
            }
        }
    }
}
