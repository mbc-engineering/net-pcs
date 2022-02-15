using System;
using TwinCAT.TypeSystem;

#nullable enable
namespace Mbc.Ads.Utils
{
    public static class DataTypeExtensions
    {
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
