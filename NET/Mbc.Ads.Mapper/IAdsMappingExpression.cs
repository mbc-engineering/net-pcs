//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Linq.Expressions;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Mapping configuration options
    /// </summary>
    /// <typeparam name="TDestination">Destination type (Source type is everytime <see cref="TwinCAT.Ads.ITcAdsSymbol5"/>)</typeparam>
    public interface IAdsMappingExpression<TDestination>
    {
        /// <summary>
        /// Customize configuration for all members
        /// </summary>
        /// <param name="memberOptions">a action which configures all members</param>
        /// <returns>Itself</returns>
        IAdsMappingExpression<TDestination> ForAllSourceMember(Action<IAdsAllSourceMemberConfigurationExpression> memberOptions);

        /// <summary>
        /// Customize configuration for individual destination member
        /// </summary>
        /// <param name="destinationMember">Expression to the top-level destination member. This must be a member on the <typeparamref name="TDestination" />TDestination</param> type
        /// <param name="memberOptions">a action which configures the given member</param>
        /// <returns>Itself</returns>
        /// <typeparam name="TMember">the type of the member</typeparam>
        IAdsMappingExpression<TDestination> ForMember<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IAdsDestinationMemberConfigurationExpression<TDestination, TMember>> memberOptions);

        /// <summary>
        /// Customize configuration for an individual source member
        /// </summary>
        /// <param name="sourceSymbolName">PLC Symbol name of the source member.</param>
        /// <param name="memberOptions">Callback for member configuration options</param>
        /// <returns>Itself</returns>
        IAdsMappingExpression<TDestination> ForSourceMember(string sourceSymbolName, Action<IAdsSourceMemberConfigurationExpression> memberOptions);
    }
}
