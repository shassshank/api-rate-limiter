using API.Configuration;

namespace API.RateLimiting
{
    public interface IApiKeyRegistry
    {
        bool TryGet(string key, out ApiKeyEntry? entry);
    }
}
