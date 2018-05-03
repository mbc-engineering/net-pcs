using System;
using TwinCAT.Ads;

namespace CallAds
{
    public class PcsSymbol
    {
        public string Name { get; set; }

        public string FullPath { get; set; }

        public object Value { get; set; }

        public ITcAdsDataType TcAdsDataType { get; set; }

        /// <summary>
        /// Returns the .Net type
        /// </summary>
        public Type ManagedType
        {
            get
            {
                return TcAdsDataType?.BaseType?.ManagedType ?? null;
            }
        }        
    }
}
