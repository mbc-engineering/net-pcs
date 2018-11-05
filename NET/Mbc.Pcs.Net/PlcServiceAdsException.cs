using System;

namespace Mbc.Pcs.Net
{
    [Serializable]
    public class PlcServiceAdsException : Exception
    {
        public PlcServiceAdsException()
        {
        }

        public PlcServiceAdsException(string message)
            : base(message)
        {
        }

        public PlcServiceAdsException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected PlcServiceAdsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
