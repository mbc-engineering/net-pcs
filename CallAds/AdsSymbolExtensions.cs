using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;

namespace CallAds
{
    // ToDo: desicion: Generic type musst be new interface???
    // ToDo: Remove dictionary
    public static class AdsSymbolExtensions
    {
        public static void AddSymbolsFlatedRecursive(this IDictionary<string, ITcAdsDataType> symbols, ITcAdsDataType adsDataType, string parentVariablePath)
        {
            foreach (ITcAdsSubItem item in adsDataType.SubItems)
            {
                string variablePath = $"{parentVariablePath}.{item.SubItemName}";
                symbols.Add(variablePath, item as ITcAdsDataType);
                if (item.SubItems.Count > 0)
                {
                    symbols.AddSymbolsFlatedRecursive(item, variablePath);
                }
            }
        }

        /// <summary>
        /// Get a symbol collection of object <paramref name="subject"/>, only if it contained in <paramref name="plcSymbols"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject"></param>
        /// <param name="adsCommandVariablePrefix"></param>
        /// <param name="plcSymbols"></param>
        /// <returns></returns>
        public static PcsSymbolCollection GetSymbols<T>(this T subject, string adsCommandVariablePrefix, IDictionary<string, ITcAdsDataType> plcSymbols)
        {
            var symbols = new PcsSymbolCollection();
            foreach (var prop in typeof(T).GetProperties())
            {
                string symbolFullPath = $"{adsCommandVariablePrefix}.{prop.Name}";
                if (plcSymbols.ContainsKey(symbolFullPath))
                {
                    symbols.Add(
                        new PcsSymbol()
                        {
                            Name = prop.Name,
                            FullPath = symbolFullPath,
                            Value = prop.GetValue(subject),
                            TcAdsDataType = plcSymbols[symbolFullPath]
                        });
                }
            }

            return symbols;
        }

        public static void SetSymbols<T>(this T subject, PcsSymbolCollection symbols)
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                PcsSymbol symbol = symbols.FirstOrDefault(s => string.Equals(s.Name, prop.Name));

                if(symbol != null)
                {
                    prop.SetValue(subject, symbol.Value);
                }                
            }
        }
    }
}
