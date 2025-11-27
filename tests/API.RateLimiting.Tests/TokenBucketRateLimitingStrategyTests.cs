using System.Threading.Tasks;
using API.Configuration;
using API.RateLimiting;
using API.RateLimiting.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.RateLimiting.Tests
{
    public class TokenBucketRateLimitingStrategyTests
    {
        private TokenBucketRateLimitingStrategy CreateStrategy(
            IDistributedCache cache,
            int capacity = 2,
            double refillPerSecond = 0.0,
            int windowSeconds = 60)
        {
            var options = Options.Create(new RateLimitingOptions
            {
                TokenBucketCapacity        = capacity,
                TokenBucketRefillPerSecond = refillPerSecond,
                WindowSeconds              = windowSeconds
            });

            var logger           = NullLogger<TokenBucketRateLimitingStrategy>.Instance;
            var identityResolver = new TestIdentityResolver("token-test");

            return new TokenBucketRateLimitingStrategy(cache, logger, identityResolver, options);
        }

        private HttpContext CreateHttpContext(string path = "/api/demo/token")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            return context;
        }

        [Fact]
        public async Task Allows_Up_To_Capacity_Then_Blocks()
        {
            // Arrange
            var cache    = DistributedCacheFactory.CreateMemoryCache();
            var strategy = CreateStrategy(cache, capacity: 2, refillPerSecond: 0.0);

            var context = CreateHttpContext();

            // Act
            var d1 = await strategy.ShouldAllowAsync(context);
            var d2 = await strategy.ShouldAllowAsync(context);
            var d3 = await strategy.ShouldAllowAsync(context);

            // Assert
            Assert.True(d1.Allowed);
            Assert.Equal(2, d1.Limit);
            Assert.Equal(1, d1.Remaining);

            Assert.True(d2.Allowed);
            Assert.Equal(0, d2.Remaining);

            Assert.False(d3.Allowed);
            Assert.Equal("Token bucket limit exceeded.", d3.Reason);
            Assert.Equal(2, d3.Limit);
            Assert.Equal(0, d3.Remaining);
            // Retry-After may be null if refillPerSecond == 0
        }
    }
}
