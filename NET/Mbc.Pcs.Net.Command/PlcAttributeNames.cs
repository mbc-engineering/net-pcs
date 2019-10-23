//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Command
{
    public static class PlcAttributeNames
    {
        /// <summary>
        /// PLC Attribute to define Input Data like: {attribute 'PlcCommandInput'}
        /// </summary>
        public const string PlcCommandInput = "PlcCommandInput";

        /// <summary>
        /// PLC Attribute to define Output Data like: {attribute 'PlcCommandOutput'}
        /// </summary>
        public const string PlcCommandOutput = "PlcCommandOutput";

        /// <summary>
        /// PLC Attribute to define a optional Input Data like: {attribute 'PlcCommandInputOptional'}
        /// </summary>
        public const string PlcCommandInputOptional = "PlcCommandInputOptional";
    }
}
