//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils;
using System.Collections.Generic;

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
                return (T)AdsConvert.ChangeType<T>(value);
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
