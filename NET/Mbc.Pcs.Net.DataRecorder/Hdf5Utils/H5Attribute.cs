using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    /// <summary>
    /// Stellt den Zugriff auf HDF5-Attribute zur Verfügung.
    /// </summary>
    public class H5Attribute
    {
        private readonly long _locId;

        public H5Attribute(H5File file)
        {
            _locId = file.Id;
        }

        public H5Attribute(H5DataSet dataSet)
        {
            _locId = dataSet.Id;
        }

        /// <summary>
        /// Liefert alle Attribut-Namen zurück.
        /// </summary>
        public IEnumerable<string> GetAttributeNames()
        {
            var objectInfo = default(H5O.info_t);

            lock (H5GlobalLock.Sync)
            {
                var ret = H5O.get_info(_locId, ref objectInfo);
                H5Error.CheckH5Result(ret);
            }

            for (ulong index = 0; index < objectInfo.num_attrs; index++)
            {
                var name = new StringBuilder();

                lock (H5GlobalLock.Sync)
                {
                    var ret = H5A.get_name_by_idx(_locId, ".", H5.index_t.NAME, H5.iter_order_t.NATIVE, index, name, new IntPtr(1024)).ToInt32();
                    H5Error.CheckH5Result(ret);
                }

                yield return name.ToString();
            }
        }

        /// <summary>
        /// Schreibt ein Attribut mit dem Namen <paramref name="name"/> und
        /// den Wert <paramref name="value"/>.
        /// </summary>
        public void Write(string name, string value)
        {
            var valueBuffer = Encoding.UTF8.GetBytes(value);

            var valuePtr = Marshal.AllocHGlobal(valueBuffer.Length);
            try
            {
                Marshal.Copy(valueBuffer, 0, valuePtr, valueBuffer.Length);

                lock (H5GlobalLock.Sync)
                {
                    using (H5Id type = new H5Id(H5T.create(H5T.class_t.STRING, new IntPtr(valueBuffer.Length)), H5T.close),
                        dspace = new H5Id(H5S.create(H5S.class_t.SCALAR), H5S.close))
                    {
                        var ret = H5T.set_cset(type, H5T.cset_t.UTF8);
                        H5Error.CheckH5Result(ret);

                        using (var attribute = new H5Id(H5A.create(_locId, name, type, dspace), H5A.close))
                        {
                            ret = H5A.write(attribute, type, valuePtr);
                            H5Error.CheckH5Result(ret);
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(valuePtr);
            }
        }

        public void Write(string name, sbyte value) =>
            WriteScalar(H5T.NATIVE_INT8, name, 1, ptr => Marshal.WriteByte(ptr, (byte)value));

        public void Write(string name, short value) =>
            WriteScalar(H5T.NATIVE_INT16, name, 2, ptr => Marshal.WriteInt16(ptr, value));

        public void Write(string name, int value) =>
            WriteScalar(H5T.NATIVE_INT32, name, 4, ptr => Marshal.WriteInt32(ptr, value));

        public void Write(string name, long value) =>
            WriteScalar(H5T.NATIVE_INT64, name, 8, ptr => Marshal.WriteInt64(ptr, value));

        public void Write(string name, byte value) =>
            WriteScalar(H5T.NATIVE_UINT8, name, 1, ptr => Marshal.WriteByte(ptr, value));

        public void Write(string name, ushort value) =>
            WriteScalar(H5T.NATIVE_UINT16, name, 2, ptr => Marshal.WriteInt16(ptr, (short)value));

        public void Write(string name, uint value) =>
            WriteScalar(H5T.NATIVE_UINT32, name, 4, ptr => Marshal.WriteInt32(ptr, (int)value));

        public void Write(string name, ulong value) =>
            WriteScalar(H5T.NATIVE_UINT64, name, 8, ptr => Marshal.WriteInt64(ptr, (long)value));

        public void Write(string name, float value) =>
            WriteScalar(H5T.NATIVE_FLOAT, name, sizeof(float), ptr => UnmanagedUtils.SingleToPtr(value, ptr));

        public void Write(string name, double value) =>
            WriteScalar(H5T.NATIVE_DOUBLE, name, sizeof(double), ptr => UnmanagedUtils.DoubleToPtr(value, ptr));

        private void WriteScalar(long h5NativeTypeId, string name, int size, Action<IntPtr> writePtr)
        {
            var valuePtr = Marshal.AllocHGlobal(size);
            try
            {
                writePtr(valuePtr);

                lock (H5GlobalLock.Sync)
                {
                    using (H5Id dspace = new H5Id(H5S.create(H5S.class_t.SCALAR), H5S.close))
                    {
                        H5Id attribute = null;
                        try
                        {
                            if (H5Error.CheckH5Result(H5A.exists(_locId, name)) > 0)
                            {
                                attribute = new H5Id(H5A.open(_locId, name), H5A.close);
                            }
                            else
                            {
                                attribute = new H5Id(H5A.create(_locId, name, h5NativeTypeId, dspace), H5A.close);
                            }

                            var ret = H5A.write(attribute, h5NativeTypeId, valuePtr);
                            H5Error.CheckH5Result(ret);
                        }
                        finally
                        {
                            attribute?.Dispose();
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(valuePtr);
            }
        }

        /// <summary>
        /// Liest das Attribut mit dem Namen <paramref name="name"/> als
        /// String.
        /// </summary>
        public string ReadString(string name)
        {
            return (string)Read(name);
        }

        public byte ReadByte(string name) =>
            ReadScalar(H5T.NATIVE_UINT8, name, sizeof(byte), (ptr) => Marshal.ReadByte(ptr));
        public ushort ReadUShort(string name) =>
            ReadScalar(H5T.NATIVE_UINT16, name, sizeof(ushort), (ptr) => (ushort)Marshal.ReadInt16(ptr));
        public uint ReadUInt(string name) =>
            ReadScalar(H5T.NATIVE_UINT32, name, sizeof(uint), (ptr) => (uint)Marshal.ReadInt32(ptr));
        public ulong ReadULong(string name) =>
            ReadScalar(H5T.NATIVE_UINT64, name, sizeof(ulong), (ptr) => (ulong)Marshal.ReadInt64(ptr));
        public sbyte ReadSByte(string name) =>
            ReadScalar(H5T.NATIVE_INT8, name, sizeof(sbyte), (ptr) => (sbyte)Marshal.ReadByte(ptr));
        public short ReadShort(string name) =>
            ReadScalar(H5T.NATIVE_INT16, name, sizeof(short), (ptr) => Marshal.ReadInt16(ptr));
        public int ReadInt(string name) =>
            ReadScalar(H5T.NATIVE_INT32, name, sizeof(int), (ptr) => Marshal.ReadInt32(ptr));
        public long ReadLong(string name) =>
            ReadScalar(H5T.NATIVE_INT64, name, sizeof(long), (ptr) => Marshal.ReadInt64(ptr));
        public float ReadFloat(string name) =>
            ReadScalar(H5T.NATIVE_FLOAT, name, sizeof(float), (ptr) => UnmanagedUtils.PtrToSingle(ptr));
        public double ReadDouble(string name) =>
            ReadScalar(H5T.NATIVE_DOUBLE, name, sizeof(double), (ptr) => UnmanagedUtils.PtrToDouble(ptr));

        private T ReadScalar<T>(long h5NativeTypeId, string name, int size, Func<IntPtr, T> readPtr)
        {
            var bufferPtr = Marshal.AllocHGlobal(size);
            try
            {
                lock (H5GlobalLock.Sync)
                {
                    using (H5Id attribute = new H5Id(H5A.open(_locId, name), H5A.close))
                    {
                        var ret = H5A.read(attribute, h5NativeTypeId, bufferPtr);
                        H5Error.CheckH5Result(ret);
                    }
                }

                return readPtr(bufferPtr);
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public object Read(string name)
        {
            lock (H5GlobalLock.Sync)
            {
                using (H5Id attribute = new H5Id(H5A.open(_locId, name), H5A.close),
                type = new H5Id(H5A.get_type(attribute), H5T.close),
                typeMem = new H5Id(H5T.get_native_type(type, H5T.direction_t.ASCEND), H5T.close))
                {
                    var size = H5T.get_size(type).ToInt32();

                    var bufferPtr = Marshal.AllocHGlobal(size);
                    try
                    {
                        var ret = H5A.read(attribute, type, bufferPtr);
                        H5Error.CheckH5Result(ret);
                        return ConvertData(bufferPtr, type, size);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(bufferPtr);
                    }
                }
            }
        }

        private object ConvertData(IntPtr bufferPtr, long typeId, int size)
        {
            H5T.class_t typeClass;
            lock (H5GlobalLock.Sync)
            {
                typeClass = H5T.get_class(typeId);
            }

            switch (typeClass)
            {
                case H5T.class_t.STRING:
                    H5T.cset_t cset;
                    lock (H5GlobalLock.Sync)
                    {
                        cset = H5T.get_cset(typeId);
                    }

                    switch (cset)
                    {
                        case H5T.cset_t.ASCII:
                            return UnmanagedUtils.PtrToStringASCII(bufferPtr, size);
                        case H5T.cset_t.UTF8:
                            return UnmanagedUtils.PtrToStringUTF8(bufferPtr, size);
                        default:
                            throw new H5Error($"Invalid character set {cset}.");
                    }

                case H5T.class_t.INTEGER:
                    if (typeId == H5T.NATIVE_CHAR || typeId == H5T.NATIVE_B8 || typeId == H5T.NATIVE_INT8)
                        return (sbyte)Marshal.ReadByte(bufferPtr);
                    if (typeId == H5T.NATIVE_SHORT || typeId == H5T.NATIVE_B16 || typeId == H5T.NATIVE_INT16)
                        return Marshal.ReadInt16(bufferPtr);
                    if (typeId == H5T.NATIVE_INT || typeId == H5T.NATIVE_B32 || typeId == H5T.NATIVE_INT32)
                        return Marshal.ReadInt32(bufferPtr);
                    if (typeId == H5T.NATIVE_LONG || typeId == H5T.NATIVE_B64 || typeId == H5T.NATIVE_INT64)
                        return Marshal.ReadInt64(bufferPtr);
                    if (typeId == H5T.NATIVE_UCHAR || typeId == H5T.NATIVE_UINT8)
                        return Marshal.ReadByte(bufferPtr);
                    if (typeId == H5T.NATIVE_USHORT || typeId == H5T.NATIVE_UINT16)
                        return (ushort)Marshal.ReadInt16(bufferPtr);
                    if (typeId == H5T.NATIVE_UINT || typeId == H5T.NATIVE_UINT32)
                        return (uint)Marshal.ReadInt32(bufferPtr);
                    if (typeId == H5T.NATIVE_ULONG || typeId == H5T.NATIVE_UINT64)
                        return (ulong)Marshal.ReadInt64(bufferPtr);

                    throw new H5Error($"Unknown type id: {typeId}");

                case H5T.class_t.FLOAT:
                    if (typeId == H5T.NATIVE_FLOAT)
                        return UnmanagedUtils.PtrToSingle(bufferPtr);
                    if (typeId == H5T.NATIVE_DOUBLE)
                        return UnmanagedUtils.PtrToDouble(bufferPtr);

                    throw new H5Error($"Unknown type id: {typeId}");

                default:
                    throw new H5Error($"Unhandled class: {typeClass}.");
            }
        }
    }
}
