using HDF.PInvoke;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    public class H5DataSet : IDisposable
    {
        private readonly bool[] _growing;
        private bool _disposed;

        public class CreateOptions
        {
            public ulong[] Chunks { get; set; }

            public uint? DeflateLevel { get; set; }
        }

        /// <summary>
        /// Öffnet ein bestehendes Dataset mit dem angegeben Namen.
        /// </summary>
        public static H5DataSet Open(H5File file, string name)
        {
            return Open(file.Id, name);
        }

        /// <summary>
        /// Öffnet ein bestehendes Dataset mit dem angegeben Namen.
        /// </summary>
        public static H5DataSet Open(H5Group group, string name)
        {
            return Open(group.Id, name);
        }

        private static H5DataSet Open(long locId, string name)
        {
            lock (H5GlobalLock.Sync)
            {
                var dataSetId = H5Error.CheckH5Result(H5D.open(locId, name));

                bool[] growing;
                using (var dataSpaceId = new H5Id(H5D.get_space(dataSetId), id => H5S.close(id)))
                {
                    var rank = H5Error.CheckH5Result(H5S.get_simple_extent_ndims(dataSpaceId));

                    var maxDims = new ulong[rank];
                    H5Error.CheckH5Result(H5S.get_simple_extent_dims(dataSpaceId, null, maxDims));

                    growing = maxDims.Select(x => x == H5S.UNLIMITED).ToArray();
                }

                return new H5DataSet(dataSetId, growing);
            }
        }

        public static H5DataSet Create(H5File file, string name, H5Type type, H5DataSpace space, CreateOptions options = null)
        {
            return Create(file.Id, name, type.Id, space.Id, options);
        }

        public static H5DataSet Create(H5Group group, string name, H5Type type, H5DataSpace space, CreateOptions options = null)
        {
            return Create(group.Id, name, type.Id, space.Id, options);
        }

        private static H5DataSet Create(long locId, string name, long typeId, long spaceId, CreateOptions options)
        {
            lock (H5GlobalLock.Sync)
            {
                var rank = H5S.get_simple_extent_ndims(spaceId);
                if (rank < 0)
                    throw H5Error.GetExceptionFromHdf5Stack();

                var maxDimensions = new ulong[rank];
                var ret = H5S.get_simple_extent_dims(spaceId, null, maxDimensions);
                if (ret < 0)
                    throw H5Error.GetExceptionFromHdf5Stack();

                var growing = maxDimensions.Select(x => x == H5S.UNLIMITED).ToArray();

                if (growing.Contains(true) && options?.Chunks == null)
                    throw new ArgumentException("Growing data sets require chunking.", nameof(options));

                using (var propList = new H5Id(H5P.create(H5P.DATASET_CREATE), (id) => H5P.close(id)))
                {
                    if (options?.Chunks != null)
                    {
                        ret = H5P.set_chunk(propList.Id, options.Chunks.Length, options.Chunks);
                        H5Error.CheckH5Result(ret);
                    }

                    if (options != null && options.DeflateLevel.HasValue)
                    {
                        ret = H5P.set_deflate(propList.Id, options.DeflateLevel.Value);
                        H5Error.CheckH5Result(ret);
                    }

                    var dataSetId = H5D.create(locId, name, typeId, spaceId, dcpl_id: propList.Id);
                    if (dataSetId < 0)
                        throw H5Error.GetExceptionFromHdf5Stack();

                    return new H5DataSet(dataSetId, growing);
                }
            }
        }

        private H5DataSet(long dataSetId, bool[] growing)
        {
            Id = dataSetId;
            _growing = growing;
        }

        ~H5DataSet()
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
                var ret = H5D.close(Id);
                H5Error.CheckH5Result(ret);
            }
        }

        internal long Id { get; }

        public bool IsGrowing => _growing.Contains(true);

        /// <summary>
        /// Liefert den Typ des Datasets zurück (sofern möglich -> ComboundType, CustomTypes).
        /// </summary>
        public Type ValueType
        {
            get
            {
                lock (H5GlobalLock.Sync)
                {
                    using (var typeId = new H5Id(H5D.get_type(Id), id => H5T.close(id)))
                    {
                        return H5Type.H5ToNative(typeId);
                    }
                }
            }
        }

        /// <summary>
        /// Liefert die Dimension des Datasets zurück.
        /// </summary>
        public ulong[] GetDimensions()
        {
            lock (H5GlobalLock.Sync)
            {
                using (var dataSpaceId = new H5Id(H5D.get_space(Id), id => H5S.close(id)))
                {
                    var rank = H5Error.CheckH5Result(H5S.get_simple_extent_ndims(dataSpaceId));

                    var dims = new ulong[rank];
                    H5Error.CheckH5Result(H5S.get_simple_extent_dims(dataSpaceId, dims, null));

                    return dims;
                }
            }
        }

        /// <summary>
        /// Liefert die max. Dimension des Datasets zurück.
        /// </summary>
        public ulong[] GetMaxDimensions()
        {
            lock (H5GlobalLock.Sync)
            {
                using (var dataSpaceId = new H5Id(H5D.get_space(Id), id => H5S.close(id)))
                {
                    var rank = H5Error.CheckH5Result(H5S.get_simple_extent_ndims(dataSpaceId));

                    var maxDims = new ulong[rank];
                    H5Error.CheckH5Result(H5S.get_simple_extent_dims(dataSpaceId, null, maxDims));

                    return maxDims;
                }
            }
        }

        /// <summary>
        /// Liefert die konfigurierte Chunk-Size zurück.
        /// </summary>
        public ulong[] GetChunkSize()
        {
            lock (H5GlobalLock.Sync)
            {
                using (var propListId = new H5Id(H5D.get_create_plist(Id), id => H5P.close(id)))
                {
                    int rank;
                    using (var dataSpaceId = new H5Id(H5D.get_space(Id), id => H5S.close(id)))
                    {
                        rank = H5Error.CheckH5Result(H5S.get_simple_extent_ndims(dataSpaceId));
                    }

                    var dims = new ulong[rank];
                    H5Error.CheckH5Result(H5P.get_chunk(propListId, rank, dims));
                    return dims;
                }
            }
        }

        public void ExtendDimensions(ulong[] newDimensions)
        {
            lock (H5GlobalLock.Sync)
            {
                var ret = H5D.set_extent(Id, newDimensions);
                H5Error.CheckH5Result(ret);
            }
        }

        public H5DataSpace GetSpace()
        {
            lock (H5GlobalLock.Sync)
            {
                var spaceId = H5D.get_space(Id);
                if (spaceId < 0)
                    throw H5Error.GetExceptionFromHdf5Stack();

                return new H5DataSpace(spaceId);
            }
        }

        public void Write<T>(T[,] data, H5DataSpace fileDataSpace = null)
        {
            Write(typeof(T), data, fileDataSpace);
        }

        public void Write<T>(T[] data, H5DataSpace fileDataSpace = null)
        {
            Write(typeof(T), data, fileDataSpace);
        }

        public void Write(Array data, H5DataSpace fileDataSpace = null, H5DataSpace memoryDataSpace = null)
        {
            Write(data.GetType().GetElementType(), data, fileDataSpace, memoryDataSpace);
        }

        public void Write(Type type, Array data, H5DataSpace fileDataSpace = null, H5DataSpace memoryDataSpace = null)
        {
            var memoryType = H5Type.NativeToH5(type);
            var memoryRank = data.Rank;
            var memoryDimension = Enumerable.Range(0, memoryRank).Select(i => (ulong)data.GetLength(i)).ToArray();

            lock (H5GlobalLock.Sync)
            {
                var memorySpace = memoryDataSpace ?? H5DataSpace.CreateSimpleFixed(memoryDimension);
                try
                {
                    if (IsGrowing && fileDataSpace == null)
                    {
                        if (memoryDataSpace != null)
                            throw new NotImplementedException("Currently sourceDataSpace is not supported with growing data sets.");

                        ulong[] start;
                        using (var space = GetSpace())
                        {
                            var cur = space.CurrentDimensions;
                            start = new ulong[cur.Length];
                            var end = new ulong[cur.Length];
                            for (var i = 0; i < cur.Length; i++)
                            {
                                if (_growing[i])
                                {
                                    start[i] = cur[i];
                                    end[i] = cur[i] + (ulong)data.GetLength(i);
                                }
                                else
                                {
                                    start[i] = 0;
                                    end[i] = cur[i];
                                }
                            }

                            ExtendDimensions(end);
                        }

                        using (var space = GetSpace())
                        {
                            space.Select(start, memoryDimension);
                            Write(memorySpace, memoryType, space, data);
                        }
                    }
                    else
                    {
                        Write(memorySpace, memoryType, fileDataSpace, data);
                    }
                }
                finally
                {
                    // nur Dispose, wenn der DataSpace auch erzeugt wurde
                    if (memorySpace != memoryDataSpace)
                    {
                        memorySpace.Dispose();
                    }
                }
            }
        }

        public void Read(Array data, H5DataSpace fileDataSpace = null, H5DataSpace memoryDataSpace = null)
        {
            Read(data.GetType().GetElementType(), data, fileDataSpace, memoryDataSpace);
        }

        public void Read(Type type, Array data, H5DataSpace fileDataSpace = null, H5DataSpace memoryDataSpace = null)
        {
            var memoryType = H5Type.NativeToH5(type);

            lock (H5GlobalLock.Sync)
            {
                var memorySpace = memoryDataSpace ?? H5DataSpace.CreateSimpleFixed(Enumerable.Range(0, data.Rank).Select(i => (ulong)data.GetLength(i)).ToArray());
                try
                {
                    // checks
                    if (data.Rank != memorySpace.Rank)
                        throw new ArgumentException($"Rank of memoryDataSpace ({memorySpace.Rank}) does not match data ({data.Rank})");

                    Read(memorySpace, memoryType, fileDataSpace, data);
                }
                finally
                {
                    // nur Dispose, wenn der DataSpace auch erzeugt wurde
                    if (memorySpace != memoryDataSpace)
                    {
                        memorySpace.Dispose();
                    }
                }
            }
        }

        public void Append<T>(T data)
        {
            Append(typeof(T), data);
        }

        public void Append(Type type, object data)
        {
            if (!IsGrowing)
                throw new InvalidOperationException("Append can only be used on growing data sets.");

            var memoryType = H5Type.NativeToH5(type);

            lock (H5GlobalLock.Sync)
            {
                using (var memorySpace = H5DataSpace.CreateSimpleFixed(new[] { 1UL }))
                {
                    ulong[] start;
                    using (var space = GetSpace())
                    {
                        start = space.CurrentDimensions;
                        if (start.Length != 1)
                            throw new InvalidOperationException("Append can only be used on one dimensional data sets.");

                        var end = new[] { start[0] + 1 };
                        ExtendDimensions(end);
                    }

                    using (var space = GetSpace())
                    {
                        space.Select(start, new[] { 1UL });
                        Write(memorySpace, memoryType, space, data);
                    }
                }
            }
        }

        public void Append(Type type, Array data)
        {
            if (!IsGrowing)
                throw new InvalidOperationException("Append can only be used on growing data sets.");

            var memoryType = H5Type.NativeToH5(type);

            var dataSpaceDim = new ulong[data.Rank + 1];
            dataSpaceDim[0] = 1UL;
            for (var i = 0; i < data.Rank; i++)
            {
                dataSpaceDim[i + 1] = (ulong)data.GetLength(i);
            }

            lock (H5GlobalLock.Sync)
            {
                using (var memorySpace = H5DataSpace.CreateSimpleFixed(dataSpaceDim))
                {
                    ulong[] start;
                    using (var space = GetSpace())
                    {
                        start = space.CurrentDimensions;
                        if ((start.Length - 1) != data.Rank)
                            throw new InvalidOperationException("Append can only be used if dimension is one less.");

                        var end = (ulong[])start.Clone();
                        end[0]++;
                        ExtendDimensions(end);
                    }

                    using (var space = GetSpace())
                    {
                        for (int i = 0; i < data.Rank; i++)
                        {
                            start[i + 1] = 0;
                        }

                        var count = (ulong[])dataSpaceDim.Clone();
                        space.Select(start, count);
                        Write(memorySpace, memoryType, space, data);
                    }
                }
            }
        }

        /// <summary>
        /// Fügt mehrere Werte auf einmal hinzu. Das DataSet darf keine fixe Grösse
        /// besitzen und darf nur aus einer Dimension bestehen.
        /// </summary>
        /// <typeparam name="T">der Datentyp der zu schreibenden Werte</typeparam>
        public void AppendAll<T>(T[] data)
        {
            AppendAll(typeof(T), data, data.Length);
        }

        public void AppendAll(Type type, Array data, int count)
        {
            /*
             * Implementierungshinweis: Die hinzufügenden Daten werden als Array mit primitiven
             * Datentyp erwartet, da diese dann ohne Kopiervorgang direkt geschrieben werden
             * können, in dem der HDF5-Lib ein unmanaged Pointer auf die Daten übergeben wird.
             */

            if (!IsGrowing)
                throw new InvalidOperationException("Append can only be used on growing data sets.");

            if (data.GetLength(0) == 0)
                throw new ArgumentException("No data to append.", nameof(data));

            if (data.GetLength(0) < count)
                throw new ArgumentOutOfRangeException(nameof(count));

            var memoryType = H5Type.NativeToH5(type);

            // 1. Dimension ist vorgegeben durch count
            var dataSpaceDim = Enumerable.Range(0, data.Rank)
                .Select(x => (ulong)(x == 0 ? count : data.GetLength(x)))
                .ToArray();

            lock (H5GlobalLock.Sync)
            {
                using (var memorySpace = H5DataSpace.CreateSimpleFixed(dataSpaceDim))
                {
                    ulong[] start;
                    using (var space = GetSpace())
                    {
                        start = space.CurrentDimensions;
                        if (start.Length != data.Rank)
                            throw new InvalidOperationException("AppendAll can only be used if dimension matches.");

                        // 1. Dimension wird erweitert (growing)
                        var end = (ulong[])start.Clone();
                        end[0] += (ulong)count;
                        ExtendDimensions(end);
                    }

                    using (var space = GetSpace())
                    {
                        // Start: alle ausser 1. Dimension auf 0 setzen
                        for (int i = 1; i < data.Rank; i++)
                        {
                            start[i] = 0;
                        }

                        space.Select(start, dataSpaceDim);
                        Write(memorySpace, memoryType, space, data);
                    }
                }
            }
        }

        /// <summary>
        /// Schreibt die übergebenen Daten in den DataSet.
        /// </summary>
        /// <param name="memoryDataSpace">Der DataSpace der übergebenen Daten <paramref name="data"/>.</param>
        /// <param name="memoryDataType">Der Typ der übergebenen Daten <paramref name="data"/>.</param>
        /// <param name="fileDataSpace">Der DataSpace wo die Daten geschrieben werden sollen.</param>
        /// <param name="data">Die zu schreibenden Daten. Es muss sich dabei entweder um
        /// primitive Datentypen oder um 1-Dimensionale Arrays mit primitive Datentypen handeln.</param>
        private void Write(H5DataSpace memoryDataSpace, H5Type memoryDataType, H5DataSpace fileDataSpace, object data)
        {
            GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var ret = H5D.write(Id, memoryDataType.Id, memoryDataSpace.Id, fileDataSpace != null ? fileDataSpace.Id : H5S.ALL, H5P.DEFAULT, gcHandle.AddrOfPinnedObject());
                H5Error.CheckH5Result(ret);
            }
            finally
            {
                gcHandle.Free();
            }
        }

        /// <summary>
        /// Liest Daten vom DataSet in die übergebene Struktur.
        /// </summary>
        private void Read(H5DataSpace memoryDataSpace, H5Type memoryDataType, H5DataSpace fileDataSpace, object data)
        {
            GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                H5Error.CheckH5Result(H5D.read(Id, memoryDataType.Id, memoryDataSpace.Id, fileDataSpace != null ? fileDataSpace.Id : H5S.ALL, H5P.DEFAULT, gcHandle.AddrOfPinnedObject()));
            }
            finally
            {
                gcHandle.Free();
            }
        }

        public class Builder
        {
            private H5Type _type;
            private string _name;
            private ulong[] _dims;
            private CreateOptions _options = new CreateOptions();

            public Builder()
            {
            }

            public Builder WithType(H5Type type)
            {
                _type = type;
                return this;
            }

            public Builder WithType(Type type)
            {
                _type = H5Type.NativeToH5(type);
                return this;
            }

            public Builder WithType<T>()
            {
                return WithType(typeof(T));
            }

            public Builder WithName(string name)
            {
                _name = name;
                return this;
            }

            public Builder WithDimension(int dim0, params int[] dimx)
            {
                _dims = new ulong[1 + dimx.Length];
                _dims[0] = (ulong)dim0;
                for (var i = 1; i < _dims.Length; i++)
                {
                    _dims[i] = (ulong)dimx[i - 1];
                }

                return this;
            }

            public Builder WithDeflate(int deflateLevel)
            {
                _options.DeflateLevel = (uint)deflateLevel;
                return this;
            }

            public Builder WithChunking(int dim0, params int[] dimx)
            {
                _options.Chunks = new ulong[1 + dimx.Length];
                _options.Chunks[0] = (ulong)dim0;
                for (var i = 1; i < _options.Chunks.Length; i++)
                {
                    _options.Chunks[i] = (ulong)dimx[i - 1];
                }

                return this;
            }

            public H5DataSet Create(H5File file)
            {
                if (_type == null)
                    throw new InvalidOperationException($"Type must be set ({nameof(WithType)}).");
                if (_name == null)
                    throw new InvalidOperationException($"Name must be set ({nameof(WithName)}).");
                if (_dims == null)
                    throw new InvalidOperationException($"At least one dimension must be set ({nameof(WithDimension)}).");
                if (_options.Chunks != null && _options.Chunks.Length != _dims.Length)
                    throw new InvalidOperationException("Dimension size must match chunk size.");

                file.MakeGroups(H5Utils.GetGroups(_name));

                var current = new ulong[_dims.Length];
                var max = new ulong[_dims.Length];
                for (var i = 0; i < _dims.Length; i++)
                {
                    if (_dims[i] == 0)
                    {
                        current[i] = 0;
                        max[i] = H5S.UNLIMITED;
                    }
                    else
                    {
                        current[i] = max[i] = _dims[i];
                    }
                }

                lock (H5GlobalLock.Sync)
                {
                    using (var space = H5DataSpace.CreateSimple(current, max))
                    {
                        return H5DataSet.Create(file, _name, _type, space, _options);
                    }
                }
            }
        }
    }
}
