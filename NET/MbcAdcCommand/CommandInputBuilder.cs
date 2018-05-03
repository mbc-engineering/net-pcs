﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MbcAdcCommand
{
    public class CommandInputBuilder
    {

        public static ICommandInput FromDictionary(IDictionary<string, object> value)
        {
            return new DictionaryCommandInputAdapter(new ReadOnlyDictionary<string, object>(value));
        }

        public CommandInputBuilder()
        {
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