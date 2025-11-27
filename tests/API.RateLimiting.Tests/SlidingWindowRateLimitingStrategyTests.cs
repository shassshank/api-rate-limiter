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
    public class SlidingWindowRateLimitingStrategyTests
    {
        private SlidingWindowRateLimitingStrategy CreateStrategy(
            IDistributedCache cache,
            int windowSeconds = 60,
            int maxRequests = 3)
        {
            var options = Options.Create(new RateLimitingOptions
            {
                WindowSeconds = windowSeconds,
                MaxRequests   = maxRequests
            });

            var logger           = NullLogger<SlidingWindowRateLimitingStrategy>.Instance;
            var identityResolver = new TestIdentityResolver("sliding-test");

            return new SlidingWindowRateLimitingStrategy(cache, logger, identityResolver, options);
        }

        private HttpContext CreateHttpContext(string path = "/api/demo/sliding")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            return context;
        }

        [Fact]
        public async Task Allows_Up_To_MaxRequests_Then_Blocks()
        {
            // Arrange
            var cache    = DistributedCacheFactory.CreateMemoryCache();
            var strategy = CreateStrategy(cache, windowSeconds: 60, maxRequests: 3);

            var context = CreateHttpContext();

            // Act
            var d1 = await strategy.ShouldAllowAsync(context);
            var d2 = await strategy.ShouldAllowAsync(context);
            var d3 = await strategy.ShouldAllowAsync(context);
            var d4 = await strategy.ShouldAllowAsync(context);

            // Assert
            Assert.True(d1.Allowed);
            Assert.Equal(3, d1.Limit);
            Assert.Equal(2, d1.Remaining);

            Assert.True(d2.Allowed);
            Assert.Equal(1, d2.Remaining);

            Assert.True(d3.Allowed);
            Assert.Equal(0, d3.Remaining);

            Assert.False(d4.Allowed);
            Assert.Equal("Sliding window limit exceeded.", d4.Reason);
            Assert.Equal(3, d4.Limit);
            Assert.Equal(0, d4.Remaining);
        }
    }
}
