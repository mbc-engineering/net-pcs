//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Common;
using Mbc.Common.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mbc.Pcs.Net.DataRecorder
{
    /// <summary>
    /// Eine Implementierung des <see cref="IObjectPersister"/>-Interface, der die Daten eines
    /// Objekt-Typs <typeparamref name="T"/> über <see cref="BinaryReader"/> bzw.
    /// <see cref="BinaryWriter"/> liest und schreibt.
    /// <para>Gegenüber dem <see cref="SerializationObjectPersister"/> braucht diese Implementiert
    /// deutlich weniger Platz.</para>
    /// </summary>
    /// <typeparam name="T">Der Objekt-Typ, der persistiert werden soll.</typeparam>
    public class BinaryObjectPersister<T> : IObjectPersister
        where T : new()
    {
        private readonly List<(Func<T, object> getter, Action<T, object> setter, Action<object, BinaryWriter> writeDispatch, Func<BinaryReader, object> readDispatch)> _converter;

        public BinaryObjectPersister()
        {
            _converter = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite)
                .Select(x => (FastInvoke.BuildUntypedGetter<T>(x), FastInvoke.BuildUntypedSetter<T>(x), GetWriteDispatcher(x.PropertyType), GetReadDispatcher(x.PropertyType)))
                .ToList();
        }

        private static Action<object, BinaryWriter> GetWriteDispatcher(Type type)
        {
            if (type.IsArray)
            {
                var elemntDispatcher = GetWriteDispatcher(type.GetElementType());
                return (v, w) =>
                {
                    WriteDispatchArray(elemntDispatcher, v, w);
                };
            }

            if (type == typeof(DateTime))
            {
                return WriteDispatchDateTime;
            }

            if (type == typeof(bool))
            {
                return WriteDispatchBool;
            }

            if (type == typeof(float))
            {
                return WriteDispatchFloat;
            }

            if (type == typeof(byte))
            {
                return WriteDispatchByte;
            }

            if (type == typeof(ushort))
            {
                return WriteDispatchUShort;
            }

            if (type == typeof(int))
            {
                return WriteDispatchInt;
            }

            if (type == typeof(uint))
            {
                return WriteDispatchUInt;
            }

            if (type == typeof(string))
            {
                return WriteDispatchString;
            }

            if (type.IsEnum)
            {
                return GetWriteDispatcher(type.GetEnumUnderlyingType());
            }

            throw new InvalidOperationException($"Unsupported type {type}.");
        }

        private static void WriteDispatchDateTime(object value, BinaryWriter writer)
        {
            writer.Write(((DateTime)value).Ticks);
        }

        private static void WriteDispatchBool(object value, BinaryWriter writer)
        {
            writer.Write((bool)value);
        }

        private static void WriteDispatchFloat(object value, BinaryWriter writer)
        {
            writer.Write((float)value);
        }

        private static void WriteDispatchByte(object value, BinaryWriter writer)
        {
            writer.Write((byte)value);
        }

        private static void WriteDispatchUShort(object value, BinaryWriter writer)
        {
            writer.Write((ushort)value);
        }

        private static void WriteDispatchInt(object value, BinaryWriter writer)
        {
            writer.Write((int)value);
        }

        private static void WriteDispatchUInt(object value, BinaryWriter writer)
        {
            writer.Write((uint)value);
        }

        /// <summary>
        /// Prefix the binary stream with the size 4-byte long.
        /// </summary>
        private static void WriteDispatchString(object value, BinaryWriter writer)
        {
            string text = (string)value ?? string.Empty;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text);
            Int32 size = text.Length;
            writer.Write(size);
            writer.Write(bytes);
        }

        private static void WriteDispatchArray(Action<object, BinaryWriter> elementDispatcher, object value, BinaryWriter writer)
        {
            Array array = (Array)value;

            // für multi-dimensionale Arrays werden alle Dimensionen ausgeschrieben
            foreach (int dimension in Enumerable.Range(0, array.Rank))
            {
                writer.Write(array.GetLength(dimension));
            }

            foreach (object element in array)
            {
                elementDispatcher(element, writer);
            }
        }

        private static Func<BinaryReader, object> GetReadDispatcher(Type type)
        {
            if (type.IsArray)
            {
                var elementDispatcher = GetReadDispatcher(type.GetElementType());
                return (BinaryReader r) => ReadDispatchArray(elementDispatcher, type, r);
            }

            if (type == typeof(DateTime))
            {
                return ReadDispatchDateTime;
            }

            if (type == typeof(bool))
            {
                return ReadDispatchBool;
            }

            if (type == typeof(float))
            {
                return ReadDispatchFloat;
            }

            if (type == typeof(byte))
            {
                return ReadDispatchByte;
            }

            if (type == typeof(ushort))
            {
                return ReadDispatchUShort;
            }

            if (type == typeof(int))
            {
                return ReadDispatchInt;
            }

            if (type == typeof(uint))
            {
                return ReadDispatchUInt;
            }

            if (type.IsEnum)
            {
                var primitivReadDispatch = GetReadDispatcher(type.GetEnumUnderlyingType());
                return (BinaryReader r) => ReadDispatchEnum(primitivReadDispatch, type, r);
            }

            if (type == typeof(string))
            {
                return ReadDispatchString;
            }

            throw new InvalidOperationException($"Unsupported type {type}.");
        }

        private static object ReadDispatchDateTime(BinaryReader reader)
        {
            return new DateTime(reader.ReadInt64());
        }

        private static object ReadDispatchFloat(BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        private static object ReadDispatchBool(BinaryReader reader)
        {
            return reader.ReadBoolean();
        }

        private static object ReadDispatchByte(BinaryReader reader)
        {
            return reader.ReadByte();
        }

        private static object ReadDispatchUShort(BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        private static object ReadDispatchInt(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        private static object ReadDispatchUInt(BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        /// <summary>
        /// the binary stream must be prefixed with the size 4-byte long.
        /// </summary>
        private static object ReadDispatchString(BinaryReader reader)
        {
            var size = reader.ReadInt32();
            var bytes = reader.ReadBytes(size);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        private static object ReadDispatchEnum(Func<BinaryReader, object> primitivReadDispatch, Type type, BinaryReader reader)
        {
            var primitiveValue = primitivReadDispatch(reader);
            return Enum.ToObject(type, primitiveValue);
        }

        private static object ReadDispatchArray(Func<BinaryReader, object> elementReadDispatch, Type type, BinaryReader reader)
        {
            // für multi-dimensionale Arrays werden alle Dimensionen eingelesen
            var dimension = Enumerable.Range(0, type.GetArrayRank())
                .Select(x => reader.ReadInt32())
                .ToArray();

            var array = Array.CreateInstance(type.GetElementType(), dimension);

            void SetArrayValue(int[] index)
            {
                if (index.Length == dimension.Length)
                {
                    array.SetValue(elementReadDispatch(reader), index);
                }
                else
                {
                    for (int i = 0; i < dimension[index.Length]; i++)
                    {
                        SetArrayValue(index.Concat(Enumerables.Yield(i)).ToArray());
                    }
                }
            }

            SetArrayValue(new int[0]);

            return array;
        }

        public object Deserialize(Stream stream)
        {
            var data = new T();

            var reader = new BinaryReader(stream);
            foreach (var converter in _converter)
            {
                var value = converter.readDispatch(reader);
                converter.setter(data, value);
            }

            return data;
        }

        public void Serialize(object data, Stream stream)
        {
            var writer = new BinaryWriter(stream);
            try
            {
                foreach (var converter in _converter)
                {
                    var value = converter.getter((T)data);
                    converter.writeDispatch(value, writer);
                }
            }
            finally
            {
                writer.Flush();
            }
        }
    }
}
