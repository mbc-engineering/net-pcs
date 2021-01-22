//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Provides information about the destination member.
    /// </summary>
    internal interface IDestinationMemberConfiguration
    {
        /// <summary>
        /// Gets the reflection member of the destination type.
        /// </summary>
        MemberInfo Member { get; }

        /// <summary>
        /// Gets the element type of the destination type.
        /// </summary>
        Type MemberElementType { get; }

        /// <summary>
        /// Gets a value indicating if this member is required for mapping.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Gets a value indicating if a custom source to destination
        /// value converter exists.
        /// </summary>
        bool HasSourceToDestinationConverter { get; }

        object ConvertSourceToDestination(object value);

        /// <summary>
        /// Gets a value indicating if a custom destination to source
        /// value converter exists.
        /// </summary>
        bool HasDestinationToSourceConverter { get; }

        object ConvertDestinationToSource(object value, Type type);
    }
}
