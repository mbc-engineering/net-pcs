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

        protected AdsMapperException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
