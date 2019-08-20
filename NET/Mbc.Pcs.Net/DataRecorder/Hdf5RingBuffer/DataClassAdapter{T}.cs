using Mbc.Common.Reflection;
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
        private static IReadOnlyDictionary<Type, (Type Type, Func<object, object> Converter)> _typeConverter = new Dictionary<Type, (Type Type, Func<object, object> Converter)>
        {
            [typeof(DateTime)] = (typeof(long), x => ((DateTime)x).ToFileTime()),
            [typeof(bool)] = (typeof(byte), x => ((bool)x) ? (byte)1 : (byte)0),
        };

        private readonly List<(string Name, Type Type, Func<T, object> Getter, Action<T, object> Setter, Func<object, object> Converter, Type ConverterType)> _properties;

        public DataClassAdapter()
        {
            _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite)
                .Select(x =>
                {
                    Func<object, object> converter = null;
                    Type converterType = null;
                    if (_typeConverter.ContainsKey(x.PropertyType))
                    {
                        converter = _typeConverter[x.PropertyType].Converter;
                        converterType = _typeConverter[x.PropertyType].Type;
                    }
                    else if (x.PropertyType.IsEnum)
                    {
                        converterType = x.PropertyType.GetEnumUnderlyingType();
                        converter = y => Convert.ChangeType(y, converterType);
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
                .Where(x => !x.Type.IsArray)
                .Select(x =>
                {
                    return new ChannelInfo(x.Name, x.ConverterType ?? x.Type);
                });
        }

        /// <summary>
        /// Schreibt die Liste der übergebenen Daten in den Ring-Buffer.
        /// </summary>
        public void WriteData(List<T> dataList, RingBuffer ringBuffer)
        {
            var length = dataList.Count;

            foreach (var prop in _properties)
            {
                if (!prop.Type.IsArray)
                {
                    var valueType = prop.ConverterType ?? prop.Type;
                    var data = Array.CreateInstance(valueType, length);

                    for (int i = 0; i < length; i++)
                    {
                        if (prop.Converter != null)
                        {
                            data.SetValue(prop.Converter(prop.Getter(dataList[i])), i);
                        }
                        else
                        {
                            data.SetValue(prop.Getter(dataList[i]), i);
                        }
                    }

                    ringBuffer.WriteChannel(prop.Name, data);
                }
                else if (prop.Type.GetArrayRank() == 1)
                {
                    //var data = Array.CreateInstance(prop.Type.GetElementType(), length);
                    //var chIdx = 0;
                    //var exit = false;
                    //while (!exit)
                    //{
                    //    for (int i = 0; i < length; i++)
                    //    {
                    //        var values = (Array)prop.Getter(dataList[i]);
                    //        if (chIdx >= values.GetLength(0))
                    //        {
                    //            exit = true;
                    //        }
                    //        else
                    //        {
                    //            data.SetValue(values.GetValue(chIdx), i);
                    //        }
                    //    }

                    //    if (!exit)
                    //    {
                    //        ringBuffer.WriteChannel($"{prop.Name}[{chIdx}]", data);
                    //    }
                    //}
                }
                else if (prop.Type.GetArrayRank() == 2)
                {
                    // TODO oversampled multi-channel
                }
            }
        }
    }
}
