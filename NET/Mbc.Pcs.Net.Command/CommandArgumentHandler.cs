//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Base class for handler reading or writing arguments of commands.
    /// </summary>
    public abstract class CommandArgumentHandler
    {
        public abstract void WriteInputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandInput input);

        public abstract void ReadOutputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandOutput output);

        /// <summary>
        /// Reads all symbols in the same hierarchy as the function block they are flaged with the Attribute
        /// named in <para>attributeName</para>.
        /// </summary>
        protected static IDictionary<string, IMember> ReadFbSymbols(IAdsConnection adsConnection, string adsCommandFbPath, string attributeName)
        {
            return ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { attributeName });
        }

        /// <summary>
        /// Reads all symbols in the same hierarchy as the function block they are flaged with one of the Attribute
        /// named in <para>attributeNames</para>.
        /// </summary>
        protected static IDictionary<string, IMember> ReadFbSymbols(IAdsConnection adsConnection, string adsCommandFbPath, IEnumerable<string> attributeNames)
        {
            IAdsSymbol commandSymbol = adsConnection.ReadSymbol(adsCommandFbPath);
            if (commandSymbol == null)
            {
                // command Symbol not found
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_CommandNotFound, adsCommandFbPath));
            }

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };

            if (!(commandSymbol is IStructInstance structInstance))
            {
                throw new PlcCommandException(adsCommandFbPath, $"Variable '{adsCommandFbPath}' is not of type struct.");
            }

            // TODO this is a workaround because `structType.Members.[x].Category` contains zero otherwise
            _ = structInstance.MemberInstances;

            return ((IStructType)structInstance.DataType)
                .Members
                .Where(x => x.Attributes.Any(y => attributeNames.Contains(y.Name, StringComparer.OrdinalIgnoreCase)))
                .ToDictionary(
                    x => x.InstanceName,
                    x => x);
        }

        protected static Type GetManagedTypeForSubItem(IDataType subitem)
        {
            // TODO maybe factor out in utils
            return ((IManagedMappableType)subitem).ManagedType;
        }
    }
}
