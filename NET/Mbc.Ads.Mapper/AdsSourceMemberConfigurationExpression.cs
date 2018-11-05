namespace Mbc.Ads.Mapper
{
    internal class AdsSourceMemberConfigurationExpression
        : AdsAllSourceMemberConfigurationExpression, IAdsSourceMemberConfigurationExpression, ISourceMemberConfiguration
    {
        public AdsSourceMemberConfigurationExpression(string symbolName)
        {
            SymbolName = symbolName;
        }

        /// <summary>
        /// Gets the member symbol name of the Source (PLC) type who is mapped with destination member
        /// </summary>
        public string SymbolName { get; internal set; }

        /// <summary>
        /// Gets the member symbol name of the Source (PLC) who is cleaned Name
        /// </summary>
        public string SymbolNameClean => SymbolName.TrimStart(SymbolNamePrefixChars.ToArray());

        public void Override(AdsAllSourceMemberConfigurationExpression allSourceMemberConfigurations)
        {
            // Or
            if (allSourceMemberConfigurations.IsIgnore && !IsIgnore)
            {
                Ignore();
            }

            // Or
            if (allSourceMemberConfigurations.IsRequired && !IsRequired)
            {
                Require();
            }

            // add prefixes
            SymbolNamePrefixChars.AddRange(allSourceMemberConfigurations.SymbolNamePrefixChars);
        }
    }
}
