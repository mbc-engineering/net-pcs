//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net.Command
{
    public class CommandOutputBuilder
    {

        public static ICommandOutput FromDictionary(IDictionary<string, object> value)
        {
            return new DictionaryCommandOutputAdapter(value);
        }

        public CommandOutputBuilder()
        {
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
                else
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (InvalidCastException e)
                    {
                        throw new InvalidCastException($"Symbol {name} is not from the required type symbol to {typeof(T)}", e);
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
        }
    }
}
