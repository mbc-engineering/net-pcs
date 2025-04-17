//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
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

#if NET5_0_OR_GREATER
        protected PlcAdsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
#pragma warning disable SYSLIB0051 // Type or member is obsolete
            : base(info, context)
#pragma warning restore SYSLIB0051 // Type or member is obsolete
        {
        }
#else
        protected PlcAdsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}
