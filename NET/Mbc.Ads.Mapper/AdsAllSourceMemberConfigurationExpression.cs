//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mbc.Ads.Mapper
{
    internal class AdsAllSourceMemberConfigurationExpression : IAdsAllSourceMemberConfigurationExpression
    {
        /// <summary>
        /// Ignore this Source member for configuration validation and skip during mapping
        /// </summary>
        public bool IsIgnore { get; internal set; }

        /// <summary>
        /// Gets the Information this member is required for mapping, otherwise throws a error.
        /// </summary>
        public bool IsRequired { get; internal set; }

        internal List<string> SymbolNamePrefix { get; private set; } = new List<string>();

        public void Ignore()
        {
            throw new NotImplementedException();
        }

        public void Require()
        {
            throw new NotImplementedException();
        }

        public void RemovePrefix(params char[] prefixChars)
        {
            SymbolNamePrefix = prefixChars.Select(x => x.ToString()).ToList();
        }

        public void RemovePrefix(params string[] prefix)
        {
            SymbolNamePrefix = prefix.Where(x => x.Trim().Length > 0).ToList();
        }
    }
}
