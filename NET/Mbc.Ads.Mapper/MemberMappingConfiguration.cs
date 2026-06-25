//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Optional;
using System;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Represents a member mapping configuration between a <see cref="ISourceMemberConfiguration"/>
    /// and a <see cref="IDestinationMemberConfiguration"/>.
    /// <para>This class is immutable.</para>
    /// </summary>
    internal class MemberMappingConfiguration
    {
        internal MemberMappingConfiguration(ISourceMemberConfiguration sourceMemberConfiguration, Option<IDestinationMemberConfiguration> destinationMemberConfiguration)
        {
            if (sourceMemberConfiguration == null) throw new ArgumentNullException(nameof(sourceMemberConfiguration));

            Source = sourceMemberConfiguration;
            Destination = destinationMemberConfiguration;
        }

        internal ISourceMemberConfiguration Source { get; }

        internal Option<IDestinationMemberConfiguration> Destination { get; }
    }
}
