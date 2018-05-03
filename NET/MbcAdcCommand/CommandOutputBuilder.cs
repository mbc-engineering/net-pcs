using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MbcAdcCommand
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

            public IEnumerable<string> GetOutputNames()
            {
                return _dictonary.Keys;
            }

            public void SetOutputData(string name, object value)
            {
                _dictonary[name] = value;
            }
        }
    }
}
