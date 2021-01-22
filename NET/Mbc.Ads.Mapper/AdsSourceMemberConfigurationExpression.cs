//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System.Linq;

namespace Mbc.Ads.Mapper
{
    internal class AdsSourceMemberConfigurationExpression
        : AdsAllSourceMemberConfigurationExpression, IAdsSourceMemberConfigurationExpression, ISourceMemberConfiguration
    {
        public AdsSourceMemberConfigurationExpression(string symbolName)
        {
            SymbolName = symbolName;
        }

        /// <summary>
        /// Gets the member symbol name of the Source (PLC) type who is mapped with destination member
        /// </summary>
        public string SymbolName { get; internal set; }

        /// <summary>
        /// Gets the member symbol name of the Source (PLC) who is cleaned Name
        /// </summary>
        public string SymbolNameClean
        {
            get
            {
                // Longest prefix
                var prefix = SymbolNamePrefix
                    .Where(x => SymbolName.StartsWith(x))
                    .OrderByDescending(x => x.Length)
                    .FirstOrDefault();
                return prefix == null ? SymbolName : SymbolName.Substring(prefix.Length);
            }
        }

        public void Override(AdsAllSourceMemberConfigurationExpression allSourceMemberConfigurations)
        {
            // Or
            if (allSourceMemberConfigurations.IsIgnore && !IsIgnore)
            {
                Ignore();
            }

            // Or
            if (allSourceMemberConfigurations.IsRequired && !IsRequired)
            {
                Require();
            }

            // add prefixes
            SymbolNamePrefix.AddRange(allSourceMemberConfigurations.SymbolNamePrefix);
        }
    }
}
