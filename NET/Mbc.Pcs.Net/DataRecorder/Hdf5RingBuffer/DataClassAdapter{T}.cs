using EnsureThat;
using Mbc.Common;
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
    /// <typeparam name="T">Der Typ der Datenklasse.</typeparam>
    public class DataClassAdapter<T>
        where T : new()
    {
        /// <summary>
        /// Converter für spezielle Datentypen, die HDF5 nicht direkt unterstützt.
        /// </summary>
        private static readonly IReadOnlyDictionary<Type, (Type Type, Func<object, object> Converter)> TypeConverter = new Dictionary<Type, (Type Type, Func<object, object> Converter)>
        {
            [typeof(DateTime)] = (typeof(long), x => ((DateTime)x).ToFileTime()),
            [typeof(bool)] = (typeof(byte), x => ((bool)x) ? (byte)1 : (byte)0),
        };

        private readonly ChannelOpts _channelOpts = new ChannelOpts();
        private readonly List<ChannelData> _channelData;

        public DataClassAdapter()
            : this(x => { })
        {
        }

        public DataClassAdapter(Action<ChannelOpts> channelOptsFn)
        {
            channelOptsFn(_channelOpts);

            _channelData = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite)
                .Where(x => !_channelOpts.IgnoredProperties.Contains(x.Name))
                .SelectMany(x =>
                {
                    Func<object, object> converter = null;
                    Type channelType = x.PropertyType;
                    var oversamplingFactor = 1;

                    if (_channelOpts.OversamplingChannels.ContainsKey(x.Name))
                    {
                        channelType = x.PropertyType.GetElementType();
                        oversamplingFactor = _channelOpts.OversamplingChannels[x.Name];
                    }

                    if (_channelOpts.MultiChannel.ContainsKey(x.Name))
                    {
                        if (!x.PropertyType.IsArray)
                            throw new InvalidOperationException($"Property {x.Name} should return a array because it is a multi channel.");

                        channelType = x.PropertyType.GetElementType();
                        if (TypeConverter.ContainsKey(channelType))
                        {
                            var converterEntry = TypeConverter[channelType];
                            converter = converterEntry.Converter;
                            channelType = converterEntry.Type;
                        }
                        else if (channelType.IsEnum)
                        {
                            channelType = x.PropertyType.GetEnumUnderlyingType();
                            converter = y => Convert.ChangeType(y, channelType);
                        }

                        var start = _channelOpts.MultiChannel[x.Name].Start;
                        var count = _channelOpts.MultiChannel[x.Name].Count;

                        if (oversamplingFactor == 1)
                        {
                            return Enumerable.Range(start, count)
                                .Select(i => new MultiChannelData($"{x.Name}[{i}]", x.PropertyType, FastInvoke.BuildUntypedGetter<T>(x), FastInvoke.BuildUntypedSetter<T>(x), converter, channelType, i - start));
                        }
                        else
                        {
                            return Enumerable.Range(start, count)
                                .Select(i => new OversamplingMultiChannelData($"{x.Name}[{i}]", x.PropertyType, FastInvoke.BuildUntypedGetter<T>(x), FastInvoke.BuildUntypedSetter<T>(x), converter, channelType, oversamplingFactor, i - start));
                        }
                    }
                    else
                    {
                        if (TypeConverter.ContainsKey(x.PropertyType))
                        {
                            channelType = TypeConverter[x.PropertyType].Type;
                            converter = TypeConverter[x.PropertyType].Converter;
                        }
                        else if (x.PropertyType.IsEnum)
                        {
                            channelType = x.PropertyType.GetEnumUnderlyingType();
                            converter = y => Convert.ChangeType(y, channelType);
                        }
                        else if (channelType.IsArray)
                        {
                            throw new InvalidOperationException($"Array type must be multi or oversampling.");
                        }

                        if (oversamplingFactor == 1)
                        {
                            return Enumerables.Yield(new ChannelData(x.Name, x.PropertyType, FastInvoke.BuildUntypedGetter<T>(x), FastInvoke.BuildUntypedSetter<T>(x), converter, channelType));
                        }
                        else
                        {
                            return Enumerables.Yield(new OversamplingChannelData(x.Name, x.PropertyType, FastInvoke.BuildUntypedGetter<T>(x), FastInvoke.BuildUntypedSetter<T>(x), converter, channelType, oversamplingFactor));
                        }
                    }
                })
                .ToList();
        }

        /// <summary>
        /// Erzeugt ChannelInfos für das typisierte Objekt.
        /// </summary>
        public IEnumerable<ChannelInfo> CreateChannelInfos()
        {
            return _channelData
                .Select(x =>
                {
                    return new ChannelInfo(
                        x.ChannelName,
                        x.ChannelType,
                        x is OversamplingChannelData ocd ? ocd.OversamplingFactor : 1);
                });
        }

        /// <summary>
        /// Schreibt die Liste der übergebenen Daten in den Channel-Writer (z.B. RingBuffer).
        /// </summary>
        public void WriteData(IReadOnlyList<T> dataList, IDataChannelWriter channelWriter)
        {
            foreach (var channel in _channelData)
            {
                var data = channel.GetValues(dataList);
                channelWriter.WriteChannel(channel.ChannelName, data);
            }
        }

        /// <summary>
        /// Liest ausgehend vom startSampleIndex die Anzahl Samples und verpackt sie wieder
        /// im Typ T.
        /// </summary>
        public List<T> ReadData(IDataChannelWriter channelWriter, long startSampleIndex, int length)
        {
            var result = new List<T>(length);

            foreach (var channel in _channelData)
            {
                var data = Array.CreateInstance(channel.PropertyType, length);
                channelWriter.ReadChannel(channel.ChannelName, startSampleIndex, data);
                channel.SetValues(result, data);
            }

            return result;
        }

        /// <summary>
        /// Definiert einen "normalen" Kanal.
        /// </summary>
        private class ChannelData
        {
            protected readonly Func<T, object> _getter;
            protected readonly Action<T, object> _setter;
            protected readonly Func<object, object> _converter;

            public ChannelData(string channelName, Type propertyType, Func<T, object> getter, Action<T, object> setter, Func<object, object> converter, Type channelType)
            {
                ChannelName = channelName;
                PropertyType = propertyType;
                _getter = getter;
                _setter = setter;
                _converter = converter;
                ChannelType = channelType;
            }

            public string ChannelName { get; }
            public Type PropertyType { get; }
            public Type ChannelType { get; }

            public virtual Array GetValues(IReadOnlyList<T> input)
            {
                var length = input.Count;

                var data = Array.CreateInstance(ChannelType, length);

                if (_converter == null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        data.SetValue(_getter(input[i]), i);
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        data.SetValue(_converter(_getter(input[i])), i);
                    }
                }

                return data;
            }

            public virtual void SetValues(List<T> output, Array data)
            {
                EnsureArg.Is(1, data.Rank, nameof(data), optsFn: x => x.WithMessage("Array must have rank 1."));

                if (output.Count != data.Length)
                    throw new ArgumentException($"{nameof(output)} must have same length as {nameof(data)}.");

                var length = output.Count;

                if (_converter == null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        _setter(output[i], data.GetValue(i));
                    }
                }
            }
        }

        /// <summary>
        /// Definiert einen Oversampling-Kanal. Bei diesen wird kein einzelner Wert über den getter/setter ermittelt
        /// sondern ein Array mit mehreren nacheinander gesampelten Werten.
        /// </summary>
        private class OversamplingChannelData : ChannelData
        {
            public OversamplingChannelData(string channelName, Type propertyType, Func<T, object> getter, Action<T, object> setter, Func<object, object> converter, Type channelType, int oversamplingFactor)
                : base(channelName, propertyType, getter, setter, converter, channelType)
            {
                OversamplingFactor = oversamplingFactor;
            }

            public int OversamplingFactor { get; }

            public override Array GetValues(IReadOnlyList<T> input)
            {
                var length = input.Count;

                var data = Array.CreateInstance(ChannelType, length * OversamplingFactor);

                if (_converter == null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var values = (Array)_getter(input[i]);
                        if (values.Length != OversamplingFactor)
                            throw new InvalidOperationException($"Oversamplingfactor does not match. Expected: {OversamplingFactor}, Got: {values.Length}");

                        Array.Copy(values, 0, data, i * OversamplingFactor, OversamplingFactor);
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        var values = (Array)_getter(input[i]);
                        if (values.Length != OversamplingFactor)
                            throw new InvalidOperationException($"Oversamplingfactor does not match. Expected: {OversamplingFactor}, Got: {values.Length}");

                        for (int j = 0; j < values.Length; j++)
                        {
                            values.SetValue(_converter(values.GetValue(j)), (i * OversamplingFactor) + j);
                            values.SetValue(_converter(values.GetValue(j)), (i * OversamplingFactor) + j);
                        }
                    }
                }

                return data;
            }
        }

        /// <summary>
        /// Definiert einen Multikanal. Bei diesen liefert der getter/setter keinen einzelen Wert,
        /// sondern ein Array, bei dem jeder Index einen Kanal zugeordnet ist.
        /// </summary>
        private class MultiChannelData : ChannelData
        {
            private readonly int _index;

            public MultiChannelData(string channelName, Type propertyType, Func<T, object> getter, Action<T, object> setter, Func<object, object> converter, Type channelType, int index)
                : base(channelName, propertyType, getter, setter, converter, channelType)
            {
                _index = index;
            }

            public override Array GetValues(IReadOnlyList<T> input)
            {
                var length = input.Count;

                var data = Array.CreateInstance(ChannelType, length);

                if (_converter == null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        data.SetValue(((Array)_getter(input[i])).GetValue(_index), i);
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        data.SetValue(_converter(((Array)_getter(input[i])).GetValue(_index)), i);
                    }
                }

                return data;
            }
        }

        /// <summary>
        /// Definiert einen Multi-/Oversampling Kanal. Dies ist die Verbindung von
        /// <see cref="MultiChannelData"/> und <see cref="OversamplingChannelData"/>.
        /// </summary>
        private class OversamplingMultiChannelData : OversamplingChannelData
        {
            private readonly int _index;

            public OversamplingMultiChannelData(string channelName, Type propertyType, Func<T, object> getter, Action<T, object> setter, Func<object, object> converter, Type channelType, int oversamplingFactor, int index)
                : base(channelName, propertyType, getter, setter, converter, channelType, oversamplingFactor)
            {
                _index = index;
            }

            public override Array GetValues(IReadOnlyList<T> input)
            {
                var length = input.Count;

                var data = Array.CreateInstance(ChannelType, length * OversamplingFactor);

                if (_converter == null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        var values = (Array)_getter(input[i]);
                        if (values.GetLength(1) != OversamplingFactor)
                            throw new InvalidOperationException($"Oversamplingfactor does not match. Expected: {OversamplingFactor}, Got: {values.GetLength(_index)}");

                        for (int j = 0; j < values.GetLength(1); j++)
                        {
                            data.SetValue(values.GetValue(_index, j), (i * OversamplingFactor) + j);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        var values = (Array)_getter(input[i]);
                        if (values.Length != OversamplingFactor)
                            throw new InvalidOperationException($"Oversamplingfactor does not match. Expected: {OversamplingFactor}, Got: {values.Length}");

                        for (int j = 0; j < values.Length; j++)
                        {
                            data.SetValue(_converter(values.GetValue(_index, j)), (i * OversamplingFactor) + j);
                        }
                    }
                }

                return data;
            }
        }
    }
}
