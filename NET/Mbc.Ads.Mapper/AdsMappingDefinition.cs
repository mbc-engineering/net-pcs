//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TwinCAT.Ads;

namespace Mbc.Ads.Mapper
{
    internal class AdsMappingDefinition<TDestination>
    {
        internal AdsMappingDefinition()
        {
        }

        internal Func<AdsBinaryReader, object> StreamReadFunction { get; set; }

        internal Action<AdsBinaryWriter, object> StreamWriterFunction { get; set; }

        internal Action<TDestination, object> DataObjectValueSetter { get; set; }

        internal IDestinationMemberConfiguration DestinationMemberConfiguration { get; set; }
    }
}
