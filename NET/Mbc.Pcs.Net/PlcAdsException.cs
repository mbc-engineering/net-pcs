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
