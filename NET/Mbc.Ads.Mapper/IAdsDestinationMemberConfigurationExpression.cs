using System;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Destination Member configuration options
    /// </summary>
    /// <typeparam name="TDestination">The Destination type for this member</typeparam>
    public interface IAdsDestinationMemberConfigurationExpression<TDestination>
    {
        /// <summary>
        /// Ignore this Destination member for configuration validation and skip during mapping
        /// </summary>
        void Ignore();

        /// <summary>
        /// This member is required and must be present and set during mapping
        /// </summary>
        void Require();

        /// <summary>
        /// Define the source PLC Symbol member to map (case insensitive)
        /// </summary>
        /// <param name="sourceSymbolName">The Name of the Symbol to map from.</param>
        void MapFrom(string sourceSymbolName);

        /// <summary>
        /// Convert the source value to the destination member value with given function
        /// </summary>
        /// <param name="convertionFunction">The caling function to convert</param>
        /// <typeparam name="TMember">the destination type</typeparam>
        void ConvertUsing<TMember>(Func<object, TMember> convertionFunction);
    }
}
