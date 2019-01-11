//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Common.Reflection;
using System;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    internal static class DataObjectAccessor
    {
        internal static Func<TDataObject, object> CreateValueGetter<TDataObject>(MemberInfo memberInfo, int arrayIndex = -1)
        {
            if (arrayIndex < 0)
            {
                if (memberInfo is PropertyInfo propertyInfo)
                {
                    return FastInvoke.BuildUntypedGetter<TDataObject>(propertyInfo);
                }

                if (memberInfo is FieldInfo fieldInfo)
                {
                    return FastInvoke.BuildUntypedGetter<TDataObject>(fieldInfo);
                }

                throw new ArgumentException("Must be a gettable member (property or field).", nameof(memberInfo));
            }

            return (dataObject) =>
            {
                Array array = (Array)memberInfo.GetValue(dataObject);
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
            };
        }

        /// <summary>
        /// Returns a function which sets a primitive value to an object.
        /// </summary>
        /// <typeparam name="TDataObject">The object type on which the setter is defined.</typeparam>
        internal static Action<TDataObject, object> CreateValueSetter<TDataObject>(MemberInfo memberInfo, int arrayIndex = -1)
        {
            if (arrayIndex < 0)
            {
                if (memberInfo is PropertyInfo propertyInfo)
                {
                    return FastInvoke.BuildUntypedSetter<TDataObject>(propertyInfo);
                }

                if (memberInfo is FieldInfo fieldInfo)
                {
                    return FastInvoke.BuildUntypedSetter<TDataObject>(fieldInfo);
                }

                throw new ArgumentException("Must be a settable member (property or field).", nameof(memberInfo));
            }

            return (dataObject, value) =>
            {
                Array array = (Array)memberInfo.GetValue(dataObject);
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
            };
        }
    }
}
