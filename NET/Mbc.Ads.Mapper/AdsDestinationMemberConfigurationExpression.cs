//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// A configuration for a destination member.
    /// </summary>
    /// <typeparam name="TDestination">The type of the destination member.</typeparam>
    internal class AdsDestinationMemberConfigurationExpression<TDestination>
        : IAdsDestinationMemberConfigurationExpression<TDestination>, IDestinationMemberConfiguration
    {
        internal AdsDestinationMemberConfigurationExpression(MemberInfo destinationMember)
        {
            Member = destinationMember;
            var type = destinationMember.GetSettableDataType();
            if (type.IsArray)
            {
                type = type.GetElementType();
            }
            MemberElementType = type;
        }

        public string MapFromSourceSymbolName { get; private set; } = string.Empty;

        public bool HasMapFrom => MapFromSourceSymbolName.Length > 0;

        #region "IAdsDestinationMemberConfigurationExpression<TDestination>"

        public void ConvertFromSourceUsing<TMember>(Func<object, TMember> conversionFunction)
        {
            ConvertSourceToDestinationFunction = (value) => conversionFunction(value);
        }
        public void ConvertToSourceUsing<TMember>(Func<TMember, Type, object> conversionFunction)
        {
            ConvertDestinationToSourceFunction = (value, type) => conversionFunction((TMember)value, type);
        }

        public void Ignore()
        {
            // TODO
            throw new NotImplementedException();
        }

        public void MapFrom(string sourceSymbolName)
        {
            MapFromSourceSymbolName = sourceSymbolName;
        }

        public void Require()
        {
            // TODO: @MiHe Es muss irgendwo geprüft werden, ob Required erfüllt wurde, also der Destination Wert gesetzt wurde! (Required kann nur gesetzt werden wenn TDestination es enthält!)
            // if(Value was not set && memberMapping.Destination.IsRequired)
            // {
            //     throw new AdsMapperMemberMappingException($"A Requireded destination member {memberMapping.Destination.MemberName} could  not be found in object {nameof(TDestination)}.", memberMapping);
            // }
            throw new NotImplementedException();
        }

        #endregion

        #region "IDestinationMemberConfiguration"

        /// <summary>
        /// Gets the Reflection member of the Destination Type
        /// </summary>
        public MemberInfo Member { get; }

        public Type MemberElementType { get; }

        /// <summary>
        /// Gets a value if this member is required.
        /// </summary>
        public bool IsRequired { get; private set; }

        public bool HasSourceToDestinationConverter => ConvertSourceToDestinationFunction != null;

        /// <summary>
        /// Gets or sets the convertion function.
        /// </summary>
        private Func<object, object> ConvertSourceToDestinationFunction { get; set; }

        /// <summary>
        /// Converts a source value to the destination value
        /// </summary>
        /// <param name="value">the value to convert</param>
        /// <returns>the converted value</returns>
        public object ConvertSourceToDestination(object value)
        {
            if (ConvertSourceToDestinationFunction != null)
            {
                return ConvertSourceToDestinationFunction(value);
            }

            return value;
        }

        public bool HasDestinationToSourceConverter => ConvertDestinationToSourceFunction != null;

        private Func<object, Type, object> ConvertDestinationToSourceFunction { get; set; }

        public object ConvertDestinationToSource(object value, Type type)
        {
            if (ConvertDestinationToSourceFunction != null)
            {
                return ConvertDestinationToSourceFunction(value, type);
            }

            return value;
        }

        #endregion
    }
}
