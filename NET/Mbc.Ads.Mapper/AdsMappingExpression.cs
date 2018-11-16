//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Optional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    public class AdsMappingExpression<TDestination> : IAdsMappingExpression<TDestination>
    {
        private readonly List<AdsSourceMemberConfigurationExpression> _sourceMemberConfigurations = new List<AdsSourceMemberConfigurationExpression>();
        private readonly AdsAllSourceMemberConfigurationExpression _allSourceMemberConfigurations = new AdsAllSourceMemberConfigurationExpression();
        private readonly List<AdsDestinationMemberConfiguration<TDestination>> _destinationMemberConfigurations = new List<AdsDestinationMemberConfiguration<TDestination>>();

        public IAdsMappingExpression<TDestination> ForAllSourceMember(Action<IAdsAllSourceMemberConfigurationExpression> memberOptions)
        {
            memberOptions(_allSourceMemberConfigurations);
            return this;
        }

        public IAdsMappingExpression<TDestination> ForMember<TMember>(Expression<Func<TDestination, TMember>> destinationMember, Action<IAdsDestinationMemberConfigurationExpression<TDestination, TMember>> memberOptions)
        {
            MemberInfo destinationProperty = ReflectionHelper.FindProperty(destinationMember);

            var configuration = _destinationMemberConfigurations.FirstOrDefault(item => item.Member == destinationProperty);

            // create new if not exist
            if (configuration == null)
            {
                configuration = AddNewDestinationMemberConfigurationExpression(destinationProperty);
            }

            memberOptions(new AdsDestinationMemberConfigurationExpression<TDestination, TMember>(configuration));

            return this;
        }

        public IAdsMappingExpression<TDestination> ForSourceMember(string sourceSymbolName, Action<IAdsSourceMemberConfigurationExpression> memberOptions)
        {
            var expression = _sourceMemberConfigurations.FirstOrDefault(item => string.Equals(item.SymbolName, sourceSymbolName, StringComparison.Ordinal));

            // create new if not exist
            if (expression == null)
            {
                expression = AddNewSourceMemberConfigurationExpression(sourceSymbolName);
            }

            memberOptions(expression);

            return this;
        }

        /// <summary>
        /// Get Mapping configuration from source member name as master.
        /// It returns the mapping between source and destintion with all configured characteristics
        /// </summary>
        /// <param name="sourceSymbolName">the ADS symbol name</param>
        /// <returns>a <see cref="MemberMappingConfiguration{TDestination}"/></returns>
        internal MemberMappingConfiguration GetMappingFromSource(string sourceSymbolName)
        {
            AdsSourceMemberConfigurationExpression sourceExpression = GetOrCreateSourceExpression(sourceSymbolName);
            Option<IDestinationMemberConfiguration> destinationConfiguration = GetOrCreateDestinationExpression(sourceExpression);

            var memberMapping = new MemberMappingConfiguration(sourceExpression as ISourceMemberConfiguration, destinationConfiguration);

            return memberMapping;
        }

        private Option<IDestinationMemberConfiguration> GetOrCreateDestinationExpression(AdsSourceMemberConfigurationExpression sourceExpression)
        {
            // First try to use by Maping name configuration
            var destExpression = _destinationMemberConfigurations.FirstOrDefault(item => item.HasMapFrom && string.Equals(sourceExpression.SymbolName, item.MapFromSourceSymbolName, StringComparison.OrdinalIgnoreCase));
            if (destExpression != null)
            {
                return Option.Some<IDestinationMemberConfiguration>(destExpression);
            }

            // Second try to use by naming
            destExpression = _destinationMemberConfigurations.FirstOrDefault(item => string.Equals(sourceExpression.SymbolNameClean, item.Member.Name, StringComparison.OrdinalIgnoreCase));
            if (destExpression != null)
            {
                return Option.Some<IDestinationMemberConfiguration>(destExpression);
            }

            // Third check the TDestination type has the member and then create a new one
            var propertyInfo = typeof(TDestination).GetProperty(sourceExpression.SymbolNameClean);
            if (propertyInfo != null)
            {
                return Option.Some<IDestinationMemberConfiguration>(AddNewDestinationMemberConfigurationExpression(propertyInfo));
            }

            // Does not exist
            return Option.None<IDestinationMemberConfiguration>();
        }

        private AdsDestinationMemberConfiguration<TDestination> AddNewDestinationMemberConfigurationExpression(MemberInfo destinationProperty)
        {
            AdsDestinationMemberConfiguration<TDestination> expression = new AdsDestinationMemberConfiguration<TDestination>(destinationProperty);
            _destinationMemberConfigurations.Add(expression);
            return expression;
        }

        private AdsSourceMemberConfigurationExpression GetOrCreateSourceExpression(string sourceSymbolName)
        {
            var sourceExpression = _sourceMemberConfigurations.FirstOrDefault(item => string.Equals(item.SymbolName, sourceSymbolName, StringComparison.Ordinal));

            if (sourceExpression == null)
            {
                // Create Default
                sourceExpression = AddNewSourceMemberConfigurationExpression(sourceSymbolName);
            }

            // Applay configuration from AllSources
            sourceExpression.Override(_allSourceMemberConfigurations);
            return sourceExpression;
        }

        private AdsSourceMemberConfigurationExpression AddNewSourceMemberConfigurationExpression(string sourceSymbolName)
        {
            var expression = new AdsSourceMemberConfigurationExpression(sourceSymbolName);
            _sourceMemberConfigurations.Add(expression);
            return expression;
        }
    }
}
