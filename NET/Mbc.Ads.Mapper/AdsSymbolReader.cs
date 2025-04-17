//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;

namespace Mbc.Ads.Mapper
{
    public class AdsSymbolReader : IAdsSymbolInfo
    {
        public string SymbolPath { get; private set; }

        public IAdsSymbol Symbol { get; private set; }

        public static IAdsSymbolInfo Read(IAdsSymbolicAccess adsSymbolicAccess, string variablePath)
        {
            var symbol = ReadSymbolInfo(adsSymbolicAccess, variablePath);

            return new AdsSymbolReader()
            {
                SymbolPath = variablePath,
                Symbol = symbol,
            };
        }

        private static IAdsSymbol ReadSymbolInfo(IAdsSymbolicAccess adsSymbolicAccess, string variablePath)
        {
            IAdsSymbol symbolInfo = adsSymbolicAccess.ReadSymbol(variablePath);
            if (symbolInfo == null)
            {
                throw new AdsMapperException($"Could not read symbol infos from plc symbol {variablePath}.");
            }

            return symbolInfo;
        }
    }
}
