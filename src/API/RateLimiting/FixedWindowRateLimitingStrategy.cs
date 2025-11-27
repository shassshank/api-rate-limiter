using System.Text.Json;
using API.Configuration;
using API.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace API.RateLimiting
{
    public class FixedWindowRateLimitingStrategy : IRateLimitingStrategy
    {
        public string Name => "FixedWindow";

        private readonly IDistributedCache _cache;
        private readonly ILogger<FixedWindowRateLimitingStrategy> _logger;
        private readonly IRateLimitIdentityResolver _identityResolver;

        private readonly int _windowSeconds;
        private readonly int _maxRequests;

        public FixedWindowRateLimitingStrategy(
            IDistributedCache cache,
            ILogger<FixedWindowRateLimitingStrategy> logger,
            IRateLimitIdentityResolver identityResolver,
            IOptions<RateLimitingOptions> options)
        {
            _cache = cache;
            _logger = logger;
            _identityResolver = identityResolver;

            _windowSeconds = options.Value.WindowSeconds;
            _maxRequests   = options.Value.MaxRequests;
        }

        public async Task<RateLimitDecision> ShouldAllowAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            //var ip   = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var identity = _identityResolver.GetIdentity(context);

            var path = context.Request.Path.Value ?? string.Empty;

            //var cacheKey = $"fixed:{ip}";
            var cacheKey = $"fixed:{identity}";

            var now      = DateTime.UtcNow;

            var entryJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            FixedWindowEntry entry;

            // FIRST REQUEST IN WINDOW
            if (entryJson == null)
            {
                entry = new FixedWindowEntry
                {
                    Count          = 1,
                    WindowStartUtc = now
                };

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_windowSeconds)
                };

                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(entry),
                    cacheOptions,
                    cancellationToken);

                var remainingFirst = Math.Max(0, _maxRequests - 1);

                return new RateLimitDecision(
                    Allowed:   true,
                    Reason:    null,
                    Limit:     _maxRequests,
                    Remaining: remainingFirst);
            }

            // EXISTING WINDOW
            entry = JsonSerializer.Deserialize<FixedWindowEntry>(entryJson)
                    ?? new FixedWindowEntry { Count = 0, WindowStartUtc = now };

            entry.Count++;

            var elapsedSeconds   = (now - entry.WindowStartUtc).TotalSeconds;
            var remainingSeconds = _windowSeconds - elapsedSeconds;
            if (remainingSeconds < 1)
                remainingSeconds = 1;

            // OVER LIMIT?
            if (entry.Count > _maxRequests)
            {
                // _logger.LogWarning(
                //     "Fixed-window limit exceeded for IP {Ip} on path {Path}. Count={Count}, Limit={Limit}",
                //     ip, path, entry.Count, _maxRequests);
                _logger.LogWarning(
                    "Fixed-window limit exceeded for Identity {Identity} on path {Path}. Count={Count}, Limit={Limit}",
                    identity, path, entry.Count, _maxRequests);
                

                return new RateLimitDecision(
                    Allowed:   false,
                    Reason:    "Fixed window limit exceeded.",
                    Limit:     _maxRequests,
                    Remaining: 0,
                    RetryAfterSeconds:  (int)Math.Ceiling(remainingSeconds));
            }

            // STILL WITHIN LIMIT → keep key alive until window end
            var updateOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(remainingSeconds)
            };

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(entry),
                updateOptions,
                cancellationToken);

            var remainingRequests = Math.Max(0, _maxRequests - entry.Count);

            return new RateLimitDecision(
                Allowed:   true,
                Reason:    null,
                Limit:     _maxRequests,
                Remaining: remainingRequests);
        }

        private class FixedWindowEntry
        {
            public int Count { get; set; }
            public DateTime WindowStartUtc { get; set; }
        }
    }
}
