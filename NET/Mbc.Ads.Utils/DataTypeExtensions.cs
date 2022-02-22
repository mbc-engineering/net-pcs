using System;
using TwinCAT.TypeSystem;

#nullable enable
namespace Mbc.Ads.Utils
{
    /// <summary>
    /// Extension methods for ADS <see cref="IDataType"/>.
    /// </summary>
    public static class DataTypeExtensions
    {
        /// <summary>
        /// Returns the managed type for a given ADS data type or
        /// <c>null</c> in none exists.
        /// </summary>
        public static Type? GetManagedType(this IDataType dataType)
        {
            if (dataType is IManagedMappableType managedMappableType)
            {
                return managedMappableType.ManagedType;
            }

            return null;
        }
    }
}
