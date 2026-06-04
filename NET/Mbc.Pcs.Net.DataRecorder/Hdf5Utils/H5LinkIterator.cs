using System;
using System.Collections.Generic;
using HDF.PInvoke;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    /// <summary>
    /// Implementiert die H5Literate-Funktion, die alle Links einer Gruppe (oder File)
    /// auflistet.
    /// </summary>
    internal class H5LinkIterator
    {
        private readonly List<string> _names = new List<string>();

        public H5LinkIterator(long groupId)
        {
            ulong idx = 0;
            lock (H5GlobalLock.Sync)
            {
                var ret = H5L.iterate(groupId, H5.index_t.NAME, H5.iter_order_t.NATIVE, ref idx, IterateCallback, IntPtr.Zero);
                H5Error.CheckH5Result(ret);
            }
        }

        private int IterateCallback(long group, IntPtr name, ref H5L.info_t info, IntPtr op_data)
        {
            string linkName;
            switch (info.cset)
            {
                case H5T.cset_t.ASCII:
                    linkName = UnmanagedUtils.PtrToStringASCII(name, -1);
                    break;
                case H5T.cset_t.UTF8:
                    linkName = UnmanagedUtils.PtrToStringUTF8(name, -1);
                    break;
                default:
                    throw new H5Error($"Invalid character set {info.cset}.");
            }

            _names.Add(linkName);

            return 0;
        }

        public IReadOnlyList<string> Names => _names.AsReadOnly();
    }
}
