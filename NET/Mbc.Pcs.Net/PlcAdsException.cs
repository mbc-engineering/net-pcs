//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net
{
    [Serializable]
    public class PlcAdsException : Exception
    {
        public PlcAdsException()
        {
        }

        public PlcAdsException(string message)
            : base(message)
        {
        }

        public PlcAdsException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected PlcAdsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
