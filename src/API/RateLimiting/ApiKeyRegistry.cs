using API.Configuration;
using Microsoft.Extensions.Options;

namespace API.RateLimiting
{
    public class ApiKeyRegistry : IApiKeyRegistry
    {
        private readonly Dictionary<string, ApiKeyEntry> _keys;

        public ApiKeyRegistry(IOptions<ApiKeyOptions> options)
        {
            // Build a lookup dictionary for fast access
            _keys = options.Value.Keys
                .Where(k => !string.IsNullOrWhiteSpace(k.Key))
                .ToDictionary(
                    k => k.Key,
                    k => k,
                    StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGet(string key, out ApiKeyEntry? entry)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                entry = null;
                return false;
            }

            if (_keys.TryGetValue(key, out var found))
            {
                entry = found;
                return true;
            }

            entry = null;
            return false;
        }
    }
}
