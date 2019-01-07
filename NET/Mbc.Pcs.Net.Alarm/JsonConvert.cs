using Newtonsoft.Json;

namespace Mbc.Pcs.Net.Alarm
{
    internal static class JsonConvert
    {
        public static bool TryDeserializeObject<T>(string json, out T result)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                result = default;
                return false;
            }

            bool success = true;
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    success = false;
                    args.ErrorContext.Handled = true;
                },
                MissingMemberHandling = MissingMemberHandling.Error,
            };

            result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, settings);

            return success;
        }
    }
}
