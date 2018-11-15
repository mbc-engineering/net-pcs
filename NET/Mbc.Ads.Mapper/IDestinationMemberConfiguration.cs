//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

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
        /// Gets a value indicating if this member is required for mapping.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Gets if this configuration has an converter function
        /// (<see cref="Convert(object)"/>.
        /// </summary>
        bool HasConverter { get; }

        /// <summary>
        /// Converts a source value to the destination value
        /// </summary>
        /// <param name="value">the source value</param>
        /// <returns>the converted destination value</returns>
        object Convert(object value);
    }
}
