﻿//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Source Member configuration options who is valid vor all sources with the type <see cref="TwinCAT.TypeSystem.IStructType"/>
    /// </summary>
    public interface IAdsAllSourceMemberConfigurationExpression
    {
        /// <summary>
        /// Ignore this Source member for configuration validation and skip during mapping
        /// </summary>
        void Ignore();

        /// <summary>
        /// This member is required and must be present and set during mapping
        /// </summary>
        void Require();

        /// <summary>
        /// Removes the chars defined in <paramref name="prefixChars"/> from the PLC symbol name
        /// </summary>
        /// <param name="prefixChars">An array of Unicode characters to remove, or null. </param>
        void RemovePrefix(params char[] prefixChars);

        /// <summary>
        /// Removes the strings defined in <paramref name="prefix"/> from the PLC symbol name.
        /// </summary>
        /// <param name="prefix">An array of strings to remove, or null. </param>
        void RemovePrefix(params string[] prefix);
    }
}
