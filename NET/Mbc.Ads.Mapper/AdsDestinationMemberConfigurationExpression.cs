//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Implementation of the <see cref="IAdsDestinationMemberConfigurationExpression{TDestination, TMember}"/>
    /// which saves the configuration in an <see cref="AdsDestinationMemberConfiguration{TDestination}"/>
    /// instance.
    /// </summary>
    internal class AdsDestinationMemberConfigurationExpression<TDestination, TMember>
        : IAdsDestinationMemberConfigurationExpression<TDestination, TMember>
    {
        private readonly AdsDestinationMemberConfiguration<TDestination> _destinationMemberConfiguration;

        public AdsDestinationMemberConfigurationExpression(AdsDestinationMemberConfiguration<TDestination> destinationMemberConfiguration)
        {
            _destinationMemberConfiguration = destinationMemberConfiguration;
        }

        public void ConvertFromSourceUsing(Func<object, TMember> conversionFunction)
        {
            _destinationMemberConfiguration.ConvertSourceToDestinationFunction =
                (value) => (TMember)conversionFunction(value);
        }

        public void ConvertToSourceUsing(Func<TMember, Type, object> conversionFunction)
        {
            _destinationMemberConfiguration.ConvertDestinationToSourceFunction =
                (value, type) => conversionFunction((TMember)value, type);
        }

        public void Ignore()
        {
            throw new NotImplementedException();
        }

        public void MapFrom(string sourceSymbolName)
        {
            _destinationMemberConfiguration.MapFromSourceSymbolName = sourceSymbolName;
        }

        public void Require()
        {
            throw new NotImplementedException();
        }
    }
}
