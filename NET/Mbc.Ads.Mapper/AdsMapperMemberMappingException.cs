//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Mbc.Ads.Mapper
{
    [Serializable]
    public class AdsMapperMemberMappingException<TDestination> : AdsMapperException
    {
        public AdsMapperMemberMappingException()
        {
        }

        public AdsMapperMemberMappingException(string message)
            : base(message)
        {
        }

        public AdsMapperMemberMappingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AdsMapperMemberMappingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
