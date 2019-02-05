//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

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

            if (type == typeof(ushort))
            {
                return WriteDispatchUShort;
            }

            if (type == typeof(int))
            {
                return WriteDispatchInt;
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

        private static void WriteDispatchUShort(object value, BinaryWriter writer)
        {
            writer.Write((ushort)value);
        }

        private static void WriteDispatchInt(object value, BinaryWriter writer)
        {
            writer.Write((int)value);
        }

        private static void WriteDispatchArray(Action<object, BinaryWriter> elementDispatcher, object value, BinaryWriter writer)
        {
            Array array = (Array)value;
            writer.Write(array.Length);
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

            if (type == typeof(ushort))
            {
                return ReadDispatchUShort;
            }

            if (type == typeof(int))
            {
                return ReadDispatchInt;
            }

            if (type.IsEnum)
            {
                var primitivReadDispatch = GetReadDispatcher(type.GetEnumUnderlyingType());
                return (BinaryReader r) => ReadDispatchEnum(primitivReadDispatch, type, r);
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

        private static object ReadDispatchUShort(BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        private static object ReadDispatchInt(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        private static object ReadDispatchEnum(Func<BinaryReader, object> primitivReadDispatch, Type type, BinaryReader reader)
        {
            var primitiveValue = primitivReadDispatch(reader);
            return Enum.ToObject(type, primitiveValue);
        }

        private static object ReadDispatchArray(Func<BinaryReader, object> elementReadDispatch, Type type, BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var array = Array.CreateInstance(type.GetElementType(), length);
            for (int i = 0; i < length; i++)
            {
                array.SetValue(elementReadDispatch(reader), i);
            }

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
