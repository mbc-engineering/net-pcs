using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net
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
                    catch (InvalidCastException)
                    {
                        throw new InvalidCastException($"Symbol {name} is not from the required type symbol to {typeof(T)}");
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
