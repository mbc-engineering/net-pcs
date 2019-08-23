using Mbc.Common.Reflection;
using Optional.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    /// <summary>
    /// Adaptiert einen Datenklasse (Klasse mit get/set-Properties) für
    /// die Benutzung mit einen RingBuffer.
    /// </summary>
    public class DataClassAdapter<T>
        where T : new()
    {
        /// <summary>
        /// Converter für spezielle Datentypen, die HDF5 nicht direkt unterstützt.
        /// </summary>
        private static IReadOnlyDictionary<Type, (Type Type, Func<object, object> Converter)> _typeConverter = new Dictionary<Type, (Type Type, Func<object, object> Converter)>
        {
            [typeof(DateTime)] = (typeof(long), x => ((DateTime)x).ToFileTime()),
            [typeof(bool)] = (typeof(byte), x => ((bool)x) ? (byte)1 : (byte)0),
        };

        private readonly ChannelOpts _channelOpts = new ChannelOpts();
        private readonly List<(string Name, Type Type, Func<T, object> Getter, Action<T, object> Setter, Func<object, object> Converter, Type ConverterType)> _properties;

        public DataClassAdapter()
            : this(x => { })
        {
        }

        public DataClassAdapter(Action<ChannelOpts> channelOptsFn)
        {
            channelOptsFn(_channelOpts);

            _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite)
                .Where(x => !_channelOpts.IgnoredProperties.Contains(x.Name))
                .Select(x =>
                {
                    Func<object, object> converter = null;
                    Type converterType = null;
                    if (_typeConverter.ContainsKey(x.PropertyType))
                    {
                        converterType = _typeConverter[x.PropertyType].Type;
                        converter = _typeConverter[x.PropertyType].Converter;
                    }
                    else if (x.PropertyType.IsEnum)
                    {
                        converterType = x.PropertyType.GetEnumUnderlyingType();
                        converter = y => Convert.ChangeType(y, converterType);
                    }
                    else if (x.PropertyType.IsArray)
                    {
                        converterType = x.PropertyType.GetElementType();
                    }

                    return (x.Name, x.PropertyType, FastInvoke.BuildUntypedGetter<T>(x), FastInvoke.BuildUntypedSetter<T>(x), converter, converterType);
                })
                .ToList();
        }

        /// <summary>
        /// Erzeugt ChannelInfos für das typisierte Objekt.
        /// </summary>
        public IEnumerable<ChannelInfo> CreateChannelInfos()
        {
            return _properties
                .Where(x => !x.Type.IsArray
                            || (_channelOpts.OversamplingChannels.ContainsKey(x.Name) && x.Type.GetArrayRank() == 1))
                .Select(x =>
                {
                    return new ChannelInfo(
                        x.Name,
                        x.ConverterType ?? x.Type,
                        _channelOpts.OversamplingChannels.GetValueOrNone(x.Name).ValueOr(1));
                });
        }

        /// <summary>
        /// Schreibt die Liste der übergebenen Daten in den Channel-Writer (z.B. RingBuffer).
        /// </summary>
        public void WriteData(IReadOnlyList<T> dataList, IDataChannelWriter channelWriter)
        {
            var length = dataList.Count;

            foreach (var prop in _properties)
            {
                if (!prop.Type.IsArray)
                {
                    Array data = CreateScalarData(dataList, length, prop);
                    channelWriter.WriteChannel(prop.Name, data);
                }
                else if (prop.Type.GetArrayRank() == 1 && _channelOpts.OversamplingChannels.ContainsKey(prop.Name))
                {
                    var oversamplingFactor = _channelOpts.OversamplingChannels[prop.Name];
                    Array data = CreateOversamplingData(dataList, length, prop, oversamplingFactor);
                    channelWriter.WriteChannel(prop.Name, data);
                }
                else
                {
                    throw new InvalidOperationException($"Unhandled property type {prop.Type}.");
                }
            }
        }

        private static Array CreateOversamplingData(IReadOnlyList<T> dataList, int length, (string Name, Type Type, Func<T, object> Getter, Action<T, object> Setter, Func<object, object> Converter, Type ConverterType) prop, int oversamplingFactor)
        {
            var data = Array.CreateInstance(prop.ConverterType ?? prop.Type.GetElementType(), length * oversamplingFactor);

            if (prop.Converter == null)
            {
                for (int i = 0; i < length; i++)
                {
                    var values = (Array)prop.Getter(dataList[i]);
                    if (values.Length != oversamplingFactor)
                        throw new InvalidOperationException($"Oversamplingfactor does not match. Expected: {oversamplingFactor}, Got: {values.Length}");

                    Array.Copy(values, 0, data, i * oversamplingFactor, oversamplingFactor);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    var values = (Array)prop.Getter(dataList[i]);
                    if (values.Length != oversamplingFactor)
                        throw new InvalidOperationException($"Oversamplingfactor does not match. Expected: {oversamplingFactor}, Got: {values.Length}");

                    for (int j = 0; j < values.Length; j++)
                    {
                        values.SetValue(prop.Converter(values.GetValue(j)), (i * oversamplingFactor) + j);
                    }
                }
            }

            return data;
        }

        private static Array CreateScalarData(IReadOnlyList<T> dataList, int length, (string Name, Type Type, Func<T, object> Getter, Action<T, object> Setter, Func<object, object> Converter, Type ConverterType) prop)
        {
            var valueType = prop.ConverterType ?? prop.Type;
            var data = Array.CreateInstance(valueType, length);

            if (prop.Converter == null)
            {
                for (int i = 0; i < length; i++)
                {
                    data.SetValue(prop.Getter(dataList[i]), i);
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    data.SetValue(prop.Converter(prop.Getter(dataList[i])), i);
                }
            }

            return data;
        }
    }
}
