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
        }

        public string MapFromSourceSymbolName { get; private set; } = string.Empty;

        public bool HasMapFrom => MapFromSourceSymbolName.Length > 0;

        #region "IAdsDestinationMemberConfigurationExpression<TDestination>"

        public void ConvertUsing<TMember>(Func<object, TMember> convertionFunction)
        {
            ConvertionFunction = (value) => convertionFunction(value);
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
        public MemberInfo Member { get; private set; }

        /// <summary>
        /// Gets a value if this member is required.
        /// </summary>
        public bool IsRequired { get; private set; }

        public bool HasConverter => ConvertionFunction != null;

        /// <summary>
        /// Gets or sets the convertion function.
        /// </summary>
        private Func<object, object> ConvertionFunction { get; set; }

        /// <summary>
        /// Converts a source value to the destination value
        /// </summary>
        /// <param name="value">the value to convert</param>
        /// <returns>the converted value</returns>
        public object Convert(object value)
        {
            if (ConvertionFunction != null)
            {
                return ConvertionFunction(value);
            }

            return value;
        }

        #endregion
    }
}
