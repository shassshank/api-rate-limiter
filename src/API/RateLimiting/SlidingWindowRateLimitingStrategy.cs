using System.Text.Json;
using API.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace API.RateLimiting
{
    public class SlidingWindowRateLimitingStrategy : IRateLimitingStrategy
    {
        public string Name => "SlidingWindow";

        private readonly IDistributedCache _cache;
        private readonly ILogger<SlidingWindowRateLimitingStrategy> _logger;
        private readonly IRateLimitIdentityResolver _identityResolver;

        private readonly int _windowSeconds;
        private readonly int _maxRequests;

        public SlidingWindowRateLimitingStrategy(
            IDistributedCache cache,
            ILogger<SlidingWindowRateLimitingStrategy> logger,
            IRateLimitIdentityResolver identityResolver,
            IOptions<RateLimitingOptions> options)
        {
            _cache = cache;
            _logger = logger;
            _identityResolver = identityResolver;

            var opts = options.Value;
            _windowSeconds = options.Value.WindowSeconds;
            _maxRequests = options.Value.MaxRequests;
        }

        public async Task<RateLimitDecision> ShouldAllowAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            //var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var identity = _identityResolver.GetIdentity(context);

            var path = context.Request.Path.Value ?? string.Empty;

            //var cacheKey = $"sliding:{ip}";
            var cacheKey   = $"sliding:{identity}";
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-_windowSeconds);

            var entryJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            SlidingWindowEntry entry;

            if (entryJson == null)
            {
                entry = new SlidingWindowEntry
                {
                    Timestamps = new List<DateTime> { now }
                };

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_windowSeconds)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(entry), options, cancellationToken);
                return new RateLimitDecision(
                    Allowed: true,
                    Reason: null,
                    Limit: _maxRequests,
                    Remaining: Math.Max(0, _maxRequests - 1));

            }

            entry = JsonSerializer.Deserialize<SlidingWindowEntry>(entryJson)
                    ?? new SlidingWindowEntry { Timestamps = new List<DateTime>() };

            // Keep only timestamps within sliding window
            entry.Timestamps = entry.Timestamps
                .Where(ts => ts >= windowStart)
                .ToList();

            // Add current request
            entry.Timestamps.Add(now);

            

            if (entry.Timestamps.Count > _maxRequests)
            {
                // _logger.LogWarning("Sliding-window limit exceeded for IP {Ip} on {Path}", ip, path);
                _logger.LogWarning(
                    "Sliding-window limit exceeded for Identity {Identity} on {Path}. Count={Count}, Limit={Limit}",
                    identity, path, entry.Timestamps.Count, _maxRequests);

                // TTL until oldest timestamp leaves the window
                var oldest = entry.Timestamps.Min();
                var secondsUntilOldestExpires = _windowSeconds - (now - oldest).TotalSeconds;
                if (secondsUntilOldestExpires < 1)
                    secondsUntilOldestExpires = 1;

                return new RateLimitDecision(
                    Allowed: false,
                    Reason: "Sliding window limit exceeded.",
                    Limit: _maxRequests,
                    Remaining: 0,
                    RetryAfterSeconds: (int)Math.Ceiling(secondsUntilOldestExpires));

            }

            var secondsUntilExpires = _windowSeconds;
            var updateOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(secondsUntilExpires)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(entry), updateOptions, cancellationToken);
            // After entry.Timestamps.Add(now); and updating Redis TTL

            var remainingRequests = Math.Max(0, _maxRequests - entry.Timestamps.Count);

            return new RateLimitDecision(
                Allowed: true,
                Reason: null,
                Limit: _maxRequests,
                Remaining: remainingRequests);

        }

        private class SlidingWindowEntry
        {
            public List<DateTime> Timestamps { get; set; } = new();
        }
    }
}
