using System.Collections.Generic;

namespace Mbc.Pcs.Net.Command
{
    public class CommandResource
    {
        private Dictionary<ushort, string> _customResultCodeTranslations = new Dictionary<ushort, string>();

        /// <summary>
        /// Adds a Knowing Result Code Plain Text result
        /// </summary>
        /// <param name="code">the Status Code Value</param>
        /// <param name="plainText">The Readable Text for the status</param>
        public void AddCustomResultCodeText(ushort resultCode, string redableText)
        {
            _customResultCodeTranslations.Add(resultCode, redableText);
        }

        public string GetResultCodeString(ushort resultCode)
        {
            // Fallback Value
            string plainText = string.Empty;

            // First search in Custom Translations
            if (_customResultCodeTranslations.ContainsKey(resultCode))
            {
                plainText = _customResultCodeTranslations[resultCode];
            }
            else
            {
                // Then search in Resources
                try
                {
                    plainText = CommandResources.ResourceManager.GetString($"ERR_ResultCode_{resultCode}");
                }
                catch
                {                
                }

                // set fallback
                if (string.IsNullOrWhiteSpace(plainText))
                {                    
                    plainText = string.Format(CommandResources.ERR_ResultCode, resultCode);
                }
            }

            return plainText;
        }
    }
}
