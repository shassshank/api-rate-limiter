using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace API.RateLimiting.Tests.TestHelpers
{
    public static class DistributedCacheFactory
    {
        public static IDistributedCache CreateMemoryCache()
        {
            var options = Options.Create(new MemoryDistributedCacheOptions());
            return new MemoryDistributedCache(options);
        }
    }
}
