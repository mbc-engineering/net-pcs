namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Provides information about the source (the PLC) member configuration.
    /// </summary>
    internal interface ISourceMemberConfiguration
    {
        /// <summary>
        /// Gets the member symbol name of the source type (the PLC) which is mapped to the destination member.
        /// </summary>
        string SymbolName { get; }

        /// <summary>
        /// Gets a value indicating if this source member should be ignored.
        /// </summary>
        bool IsIgnore { get; }

        /// <summary>
        /// Gets a value indicating if this member is required for mapping.
        /// </summary>
        bool IsRequired { get; }
    }
}
