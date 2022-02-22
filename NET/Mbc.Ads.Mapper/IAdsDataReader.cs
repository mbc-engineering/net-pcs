using System;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Reads data from an ADS <see cref="ReadOnlySpan{T}"/>
    /// </summary>
    public interface IAdsDataReader
    {
        object Read(ReadOnlySpan<byte> buffer);
    }
}
