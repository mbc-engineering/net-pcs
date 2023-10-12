using System;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Writes data to an ADS <see cref="Span{T}"/>
    /// </summary>
    public interface IAdsDataWriter
    {
        void Write(object value, Span<byte> buffer);
    }
}
