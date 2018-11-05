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
