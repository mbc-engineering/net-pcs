//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils;
using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net.Command
{
    public static class CommandOutputBuilder
    {
        /// <summary>
        /// It is important to to set the right initial value of each output parameter.
        /// The .NET values must much with compatible types of the PLC. In the 
        /// Commmand handler the conversion is happend with the <see cref="TwinCAT.TypeSystem.PrimitiveTypeMarshaler"/>
        /// and is bytewise marshalled.
        /// </summary>
        /// <param name="value">Initial Value in the right type</param>
        /// <returns>A mapping configuration of the requested plc output Parameters 
        /// that matching PLC attribute decoration like <see cref="PlcAttributeNames.PlcCommandOutput"/> 
        /// or <see cref="PlcAttributeNames.PlcCommandOutputOptional"/> </returns>
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

            public Type GetOutputDataType(string name)
            {
                return _dictonary[name].GetType();
            }
        }
    }
}
