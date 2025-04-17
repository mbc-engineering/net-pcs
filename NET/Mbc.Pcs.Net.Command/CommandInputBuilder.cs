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
        /// <summary>
        /// It is important to to set the right value of each input parameter.
        /// The .NET values must much with compatible types of the PLC. In the 
        /// Commmand handler the conversion is happend with the <see cref="TwinCAT.TypeSystem.PrimitiveTypeMarshaler"/>
        /// and is bytewise marshalled.
        /// </summary>
        /// <param name="value">The Value in the right type to transfer to PLC that 
        /// matching PLC attribute decoration like <see cref="PlcAttributeNames.PlcCommandInput"/> 
        /// or <see cref="PlcAttributeNames.PlcCommandInputOptional"></see></param>
        /// <returns>A mapping configuration of the requested plc output Parameters /></returns>
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
