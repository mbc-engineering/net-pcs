//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using TwinCAT.Ads;

namespace Mbc.Ads.Mapper
{
    public class AdsSymbolReader : IAdsSymbolInfo
    {
        public int SymbolsSize { get; private set; }

        public string SymbolPath { get; private set; }

        public ITcAdsSymbol5 Symbol { get; private set; }

        public static IAdsSymbolInfo Read(IAdsSymbolicAccess adsSymbolicAccess, string variablePath)
        {
            var (structSize, symbol) = ReadSymbolInfo(adsSymbolicAccess, variablePath);

            return new AdsSymbolReader()
            {
                SymbolsSize = structSize,
                SymbolPath = variablePath,
                Symbol = symbol,
            };
        }

        private static (int structSize, ITcAdsSymbol5 symbol) ReadSymbolInfo(IAdsSymbolicAccess adsSymbolicAccess, string variablePath)
        {
            var symbolInfo = adsSymbolicAccess.ReadSymbolInfo(variablePath);

            if (symbolInfo is ITcAdsSymbol5 symbol5Info)
            {
                int structSize = symbolInfo.Size;

                return (structSize, symbol5Info);
            }

            throw new AdsMapperException($"Could not read symbol infos from plc symbol {variablePath}.");
        }
    }
}
