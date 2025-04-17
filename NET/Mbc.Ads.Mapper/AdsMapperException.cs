//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Mbc.Ads.Mapper
{
    [Serializable]
    public class AdsMapperException : Exception
    {
        public AdsMapperException()
        {
        }

        public AdsMapperException(string message)
            : base(message)
        {
        }

        public AdsMapperException(string message, Exception inner)
            : base(message, inner)
        {
        }

#if NET5_0_OR_GREATER
        protected AdsMapperException(SerializationInfo info, StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
            : base(info, context)
#pragma warning restore SYSLIB0051 // Type or member is obsolete
        {
        }
#else
        protected AdsMapperException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
        
    }
}
