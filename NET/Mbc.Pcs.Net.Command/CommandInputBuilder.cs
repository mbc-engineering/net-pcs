//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mbc.Pcs.Net.Command
{
    public static class CommandInputBuilder
    {
        public static ICommandInput FromDictionary(IDictionary<string, object> value)
        {
            return new DictionaryCommandInputAdapter(new ReadOnlyDictionary<string, object>(value));
        }

        private class DictionaryCommandInputAdapter : ICommandInput
        {
            private readonly IDictionary<string, object> _dictonary;

            public DictionaryCommandInputAdapter(IDictionary<string, object> dictionary)
            {
                _dictonary = dictionary;
            }

            public IDictionary<string, object> GetInputData()
            {
                return _dictonary;
            }
        }
    }
}
