//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// A configuration for a destination member.
    /// </summary>
    /// <typeparam name="TDestination">The type of the destination member.</typeparam>
    internal class AdsDestinationMemberConfiguration<TDestination>
        : IDestinationMemberConfiguration
    {
        internal AdsDestinationMemberConfiguration(MemberInfo destinationMember)
        {
            Member = destinationMember;
            MemberElementType = destinationMember.GetElementType();
        }

        public string MapFromSourceSymbolName { get; set; } = string.Empty;

        public bool HasMapFrom => MapFromSourceSymbolName.Length > 0;

        public Func<object, object> ConvertSourceToDestinationFunction { get; set; }

        public Func<object, Type, object> ConvertDestinationToSourceFunction { get; set; }

        #region "IDestinationMemberConfiguration"

        /// <summary>
        /// Gets the Reflection member of the Destination Type
        /// </summary>
        public MemberInfo Member { get; }

        public Type MemberElementType { get; }

        /// <summary>
        /// Gets a value if this member is required.
        /// </summary>
        public bool IsRequired { get; private set; }

        public bool HasSourceToDestinationConverter => ConvertSourceToDestinationFunction != null;

        /// <summary>
        /// Converts a source value to the destination value
        /// </summary>
        /// <param name="value">the value to convert</param>
        /// <returns>the converted value</returns>
        public object ConvertSourceToDestination(object value)
        {
            if (ConvertSourceToDestinationFunction != null)
            {
                return ConvertSourceToDestinationFunction(value);
            }

            return value;
        }

        public bool HasDestinationToSourceConverter => ConvertDestinationToSourceFunction != null;

        public object ConvertDestinationToSource(object value, Type type)
        {
            if (ConvertDestinationToSourceFunction != null)
            {
                return ConvertDestinationToSourceFunction(value, type);
            }

            return value;
        }

        #endregion
    }
}
