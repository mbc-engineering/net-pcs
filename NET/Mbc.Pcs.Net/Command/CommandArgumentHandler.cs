using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TwinCAT.Ads;
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
        /// <param name="attributeName">The attribute flag to filter (Case Insensitive)</param>
        protected static IReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)> ReadFbSymbols(IAdsConnection adsConnection, string adsCommandFbPath, string attributeName)
        {
            ITcAdsSymbol commandSymbol = adsConnection.ReadSymbolInfo(adsCommandFbPath);
            if (commandSymbol == null)
            {
                // command Symbol not found
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_CommandNotFound, adsCommandFbPath));
            }

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };

            var fbSymbolNames = ((ITcAdsSymbol5)commandSymbol)
                .DataType.SubItems
                .Where(item =>
                    validTypeCategories.Contains(item.BaseType.Category)
                    && item.Attributes.Any(a => string.Equals(a.Name, attributeName, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(
                    item => item.SubItemName,
                    item => (variablePath: adsCommandFbPath + "." + item.SubItemName, type: GetManagedTypeForSubItem(item), byteSize: item.ByteSize));

            return new ReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)>(fbSymbolNames);
        }

        private static Type GetManagedTypeForSubItem(ITcAdsSubItem subitem)
        {
            if (subitem.BaseType.ManagedType != null)
                return subitem.BaseType.ManagedType;

            return subitem.BaseType.BaseType.ManagedType;
        }

    }
}
