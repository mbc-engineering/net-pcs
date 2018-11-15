//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Mbc.Ads.Mapper
{
    internal static class DataObjectAccessor
    {

        internal static Func<TDataObject, object> CreateValueGetter<TDataObject>(IDestinationMemberConfiguration destinationMemberConfiguration, IDictionary<object, string> enumValues = null, int arrayIndex = -1)
        {
            return (dataObject) =>
            {
                if (arrayIndex < 0)
                {
                    return destinationMemberConfiguration.Member.GetValue(dataObject);
                }
                else
                {
                    Array array = (Array)destinationMemberConfiguration.Member.GetValue(dataObject);
                    if (array.Rank == 1)
                    {
                        return array.GetValue(arrayIndex);
                    }
                    else if (array.Rank == 2)
                    {
                        return array.GetValue(
                            arrayIndex / array.GetLength(1),
                            arrayIndex % array.GetLength(1));
                    }
                    else
                    {
                        throw new AdsMapperException("Only 1 or 2 dimensional arrays are currently supported.");
                    }
                }
            };
        }

        /// <summary>
        /// Returns a function which sets a primitive value to an object.
        /// </summary>
        internal static Action<TDataObject, object> CreateValueSetter<TDataObject>(IDestinationMemberConfiguration destinationMemberConfiguration, IDictionary<object, string> enumValues = null, int arrayIndex = -1)
        {
            return (dataObject, value) =>
            {
                if (arrayIndex < 0)
                {
                    destinationMemberConfiguration.Member.SetValue(dataObject, value);
                }
                else
                {
                    Array array = (Array)destinationMemberConfiguration.Member.GetValue(dataObject);
                    if (array.Rank == 1)
                    {
                        array.SetValue(value, arrayIndex);
                    }
                    else if (array.Rank == 2)
                    {
                        array.SetValue(
                            value,
                            arrayIndex / array.GetLength(1),
                            arrayIndex % array.GetLength(1));
                    }
                    else
                    {
                        throw new AdsMapperException("Only 1 or 2 dimensional arrays are currently supported.");
                    }
                }
            };
        }
    }
}
