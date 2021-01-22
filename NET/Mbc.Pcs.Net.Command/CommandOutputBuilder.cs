//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using TwinCAT.PlcOpen;

namespace Mbc.Pcs.Net.Command
{
    public static class CommandOutputBuilder
    {
        public static ICommandOutput FromDictionary(IDictionary<string, object> value)
        {
            return new DictionaryCommandOutputAdapter(value);
        }

        private class DictionaryCommandOutputAdapter : ICommandOutput
        {
            private readonly IDictionary<string, object> _dictonary;

            public DictionaryCommandOutputAdapter(IDictionary<string, object> dictionary)
            {
                _dictonary = dictionary;
            }

            public T GetOutputData<T>(string name)
            {
                object value = _dictonary[name];
                if (value is T tValue)
                {
                    return tValue;
                }
                else if (typeof(T) == typeof(TimeSpan))
                {
                    if (value is TIME plcTime)
                    {
                        return (T)(object)plcTime.Time;
                    }

                    throw new InvalidCastException($"Symbol {name} requires TIME-plc type for TimeSpan.");
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    if (value is DATE plcDate)
                    {
                        return (T)(object)plcDate.Date;
                    }

                    throw new InvalidCastException($"Symbol {name} requires DATE-plc type for DateTime.");
                }
                else
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (InvalidCastException e)
                    {
                        throw new InvalidCastException($"Symbol {name} is not from the required type symbol to {typeof(T)}. Actual type = {value?.GetType()}", e);
                    }
                }
            }

            public IEnumerable<string> GetOutputNames()
            {
                return _dictonary.Keys;
            }

            public void SetOutputData<T>(string name, T value)
            {
                _dictonary[name] = value;
            }

            public bool HasOutputName(string name)
            {
                return _dictonary.ContainsKey(name);
            }
        }
    }
}
