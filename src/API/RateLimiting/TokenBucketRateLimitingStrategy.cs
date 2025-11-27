using System.Text.Json;
using API.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace API.RateLimiting
{
    public class TokenBucketRateLimitingStrategy : IRateLimitingStrategy
    {
        public string Name => "TokenBucket";

        private readonly IDistributedCache _cache;
        private readonly ILogger<TokenBucketRateLimitingStrategy> _logger;
        private readonly IRateLimitIdentityResolver _identityResolver;

        private readonly int _capacity;
        private readonly double _refillPerSecond;
        private readonly int _ttlSeconds;

        public TokenBucketRateLimitingStrategy(
            IDistributedCache cache,
            ILogger<TokenBucketRateLimitingStrategy> logger,
            IRateLimitIdentityResolver identityResolver,
            IOptions<RateLimitingOptions> options)
        {
            _cache = cache;
            _logger = logger;
            _identityResolver = identityResolver;


            var opts = options.Value;
            _capacity = opts.TokenBucketCapacity;
            _refillPerSecond = opts.TokenBucketRefillPerSecond;

            // TTL for Redis entries – reuse window seconds as a reasonable expiry
            _ttlSeconds = opts.WindowSeconds > 0 ? opts.WindowSeconds : 60;
        }

        public async Task<RateLimitDecision> ShouldAllowAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            // var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var identity = _identityResolver.GetIdentity(context);

            var path = context.Request.Path.Value ?? string.Empty;

            //var cacheKey = $"token:{ip}";
            var cacheKey = $"token:{identity}";

            var now = DateTime.UtcNow;

            var entryJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

            TokenBucketEntry entry;

            if (entryJson == null)
            {
                // First time we see this IP: bucket starts full minus one token
                entry = new TokenBucketEntry
                {
                    Tokens = Math.Max(0, _capacity - 1),
                    LastRefillUtc = now
                };

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_ttlSeconds)
                };

                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(entry), options, cancellationToken);
                return new RateLimitDecision(
                    Allowed: true,
                    Reason: null,
                    Limit: _capacity,
                    Remaining: _capacity - 1);
            }

            entry = JsonSerializer.Deserialize<TokenBucketEntry>(entryJson)
                    ?? new TokenBucketEntry { Tokens = _capacity, LastRefillUtc = now };

            // Refill tokens based on time passed
            var elapsedSeconds = (now - entry.LastRefillUtc).TotalSeconds;
            if (elapsedSeconds < 0)
                elapsedSeconds = 0;

            var tokensToAdd = elapsedSeconds * _refillPerSecond;
            entry.Tokens = Math.Min(_capacity, entry.Tokens + tokensToAdd);
            entry.LastRefillUtc = now;

            // Check if we have at least 1 token
            if (entry.Tokens < 1.0)
            {
                // _logger.LogWarning("Token bucket limit exceeded for IP {Ip} on {Path}", ip, path);
                _logger.LogWarning("Token bucket limit exceeded for Identity {Identity} on {Path}",identity, path);

                int? retryAfter = null;

                if (_refillPerSecond > 0)
                {
                    var missingTokens = 1.0 - entry.Tokens;
                    if (missingTokens < 0)
                        missingTokens = 0;

                    var seconds = missingTokens / _refillPerSecond;
                    if (seconds < 1)
                        seconds = 1;

                    retryAfter = (int)Math.Ceiling(seconds);
                }

                return new RateLimitDecision(
                    Allowed: false,
                    Reason: "Token bucket limit exceeded.",
                    Limit: _capacity,
                    Remaining: 0,
                    RetryAfterSeconds: retryAfter);
            }

            // Consume 1 token
            entry.Tokens -= 1.0;

            // Update Redis with new state + TTL
            var ttlSeconds = _ttlSeconds;
            var optionsEntry = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(entry), optionsEntry, cancellationToken);

            var remainingTokens = (int)Math.Floor(entry.Tokens);

            return new RateLimitDecision(
                Allowed: true,
                Reason: null,
                Limit: _capacity,
                Remaining: remainingTokens);
        }

        private class TokenBucketEntry
        {
            public double Tokens { get; set; }
            public DateTime LastRefillUtc { get; set; }
        }
    }
}
