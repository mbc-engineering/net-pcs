using System;
using System.Linq;
using HDF.PInvoke;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    public class H5DataSpace : IDisposable
    {
        public const ulong UNLIMITED = H5S.UNLIMITED;

        private bool _disposed;

        public static H5DataSpace CreateSimpleFixed(ulong[] dimension)
        {
            return CreateSimple(dimension, dimension);
        }

        public static H5DataSpace CreateSimple(ulong[] dimension, ulong[] maxDimension = null)
        {
            if (maxDimension != null && dimension.Length != maxDimension.Length)
                throw new ArgumentException("max must have the same length as current", nameof(maxDimension));

            var rank = dimension.Length;

            if (maxDimension == null)
            {
                maxDimension = Enumerable.Repeat(UNLIMITED, rank).ToArray();
            }

            lock (H5GlobalLock.Sync)
            {
                var dataSpaceId = H5S.create_simple(rank, dimension, maxDimension);
                if (dataSpaceId < 0)
                    throw H5Error.GetExceptionFromHdf5Stack();

                return new H5DataSpace(dataSpaceId);
            }
        }

        public static HyperslabSelectionBuilder CreateSelectionBuilder() => new HyperslabSelectionBuilder();

        internal H5DataSpace(long dataSpaceId)
        {
            Id = dataSpaceId;
        }

        ~H5DataSpace()
        {
            Dispose(false);
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
                var ret = H5S.close(Id);
                H5Error.CheckH5Result(ret);
            }
        }

        internal long Id { get; }

        public int Rank
        {
            get
            {
                lock (H5GlobalLock.Sync)
                {
                    var rank = H5S.get_simple_extent_ndims(Id);
                    if (rank < 0)
                        throw H5Error.GetExceptionFromHdf5Stack();

                    return rank;
                }
            }
        }

        public ulong[] CurrentDimensions
        {
            get
            {
                var rank = Rank;
                var dims = new ulong[rank];

                lock (H5GlobalLock.Sync)
                {
                    var ret = H5S.get_simple_extent_dims(Id, dims, null);
                    H5Error.CheckH5Result(ret);
                    return dims;
                }
            }
        }

        public long ElementCount
        {
            get
            {
                lock (H5GlobalLock.Sync)
                {
                    return H5Error.CheckH5Result(H5S.get_simple_extent_npoints(Id));
                }
            }
        }

        public HyperslabSelector Select(ulong[] start, ulong[] count, ulong[] stride = null, ulong[] block = null)
        {
            return new HyperslabSelector(Id, start, stride, count, block);
        }

        public class HyperslabSelector
        {
            private readonly long _dataSetId;

            internal HyperslabSelector(long dataSetId, ulong[] start, ulong[] stride, ulong[] count, ulong[] block)
            {
                _dataSetId = dataSetId;
                AddSelect(H5S.seloper_t.SET, start, stride, count, block);
            }

            private void AddSelect(H5S.seloper_t op, ulong[] start, ulong[] stride, ulong[] count, ulong[] block)
            {
                lock (H5GlobalLock.Sync)
                {
                    var ret = H5S.select_hyperslab(_dataSetId, op, start, stride, count, block);
                    H5Error.CheckH5Result(ret);
                }
            }

            public HyperslabSelector Or(ulong[] start, ulong[] count, ulong[] stride = null, ulong[] block = null)
            {
                AddSelect(H5S.seloper_t.OR, start, stride, count, block);
                return this;
            }

            public HyperslabSelector And(ulong[] start, ulong[] count, ulong[] stride = null, ulong[] block = null)
            {
                AddSelect(H5S.seloper_t.AND, start, stride, count, block);
                return this;
            }
        }

        public class HyperslabSelectionBuilder
        {
            private ulong[] _start;
            private ulong[] _count;

            internal HyperslabSelectionBuilder()
            {
            }

            public HyperslabSelectionBuilder Start(ulong dim0, params ulong[] dimx)
            {
                _start = new ulong[dimx.Length + 1];
                _start[0] = dim0;
                for (var i = 1; i < _start.Length; i++)
                {
                    _start[i] = dimx[i - 1];
                }

                return this;
            }

            public HyperslabSelectionBuilder Count(ulong dim0, params ulong[] dimx)
            {
                _count = new ulong[dimx.Length + 1];
                _count[0] = dim0;
                for (var i = 1; i < _count.Length; i++)
                {
                    _count[i] = dimx[i - 1];
                }

                return this;
            }

            public void ApplyTo(H5DataSpace dataSpace)
            {
                new HyperslabSelector(dataSpace.Id, _start, null, _count, null);
            }
        }
    }
}
