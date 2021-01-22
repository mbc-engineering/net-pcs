//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Destination Member configuration options
    /// </summary>
    /// <typeparam name="TDestination">The destination type for this member.</typeparam>
    /// <typeparam name="TMember">The type of the member to configure.</typeparam>
    public interface IAdsDestinationMemberConfigurationExpression<TDestination, TMember>
    {
        /// <summary>
        /// Ignore this Destination member for configuration validation and skip during mapping
        /// </summary>
        void Ignore();

        /// <summary>
        /// This member is required and must be present and set during mapping
        /// </summary>
        void Require();

        /// <summary>
        /// Define the source PLC symbol member to map (case insensitive)
        /// </summary>
        /// <param name="sourceSymbolName">The Name of the PLC symbol to map from.</param>
        void MapFrom(string sourceSymbolName);

        /// <summary>
        /// Custom conversion from source value to destination value.
        /// </summary>
        /// <param name="conversionFunction">The conversion function.</param>
        void ConvertFromSourceUsing(Func<object, TMember> conversionFunction);

        /// <summary>
        /// Custom conversion from destination value to source value.
        /// </summary>
        /// <param name="conversionFunction">The conversion function.</param>
        void ConvertToSourceUsing(Func<TMember, Type, object> conversionFunction);
    }
}
