using System.Collections.Generic;
using API.Configuration;
using API.RateLimiting;
using API.RateLimiting.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace API.RateLimiting.Tests
{
    public class RateLimitingStrategySelectorTests
    {
        private HttpContext CreateContext(string path)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = path;
            return ctx;
        }

        private RateLimitingStrategySelector CreateSelector(
            IEnumerable<IRateLimitingStrategy> strategies,
            RateLimitPolicyOptions policyOptions)
        {
            var options = Options.Create(policyOptions);
            return new RateLimitingStrategySelector(strategies, options);
        }

        [Fact]
        public void Selects_Strategy_Based_On_PathPrefix()
        {
            // Arrange
            var fixedStrategy   = new FakeStrategy("FixedWindow");
            var slidingStrategy = new FakeStrategy("SlidingWindow");
            var tokenStrategy   = new FakeStrategy("TokenBucket");

            var strategies = new List<IRateLimitingStrategy>
            {
                fixedStrategy,
                slidingStrategy,
                tokenStrategy
            };

            var policies = new RateLimitPolicyOptions
            {
                Rules = new List<RateLimitPolicyRule>
                {
                    new RateLimitPolicyRule
                    {
                        PathPrefix = "/api/demo/fixed",
                        Strategy   = "FixedWindow"
                    },
                    new RateLimitPolicyRule
                    {
                        PathPrefix = "/api/demo/sliding",
                        Strategy   = "SlidingWindow"
                    },
                    new RateLimitPolicyRule
                    {
                        PathPrefix = "/api/demo/token",
                        Strategy   = "TokenBucket"
                    }
                }
            };

            var selector = CreateSelector(strategies, policies);

            // Act
            var s1 = selector.SelectStrategy(CreateContext("/api/demo/fixed"));
            var s2 = selector.SelectStrategy(CreateContext("/api/demo/sliding/extra"));
            var s3 = selector.SelectStrategy(CreateContext("/api/demo/token?x=1"));

            // Assert
            Assert.Same(fixedStrategy,   s1);
            Assert.Same(slidingStrategy, s2);
            Assert.Same(tokenStrategy,   s3);
        }

        [Fact]
        public void Falls_Back_To_FixedWindow_When_No_Policy_Matches()
        {
            // Arrange
            var fixedStrategy   = new FakeStrategy("FixedWindow");
            var slidingStrategy = new FakeStrategy("SlidingWindow");

            var strategies = new List<IRateLimitingStrategy>
            {
                fixedStrategy,
                slidingStrategy
            };

            var policies = new RateLimitPolicyOptions
            {
                Rules = new List<RateLimitPolicyRule>
                {
                    new RateLimitPolicyRule
                    {
                        PathPrefix = "/api/demo/fixed",
                        Strategy   = "FixedWindow"
                    }
                }
            };

            var selector = CreateSelector(strategies, policies);

            // Act
            var s1 = selector.SelectStrategy(CreateContext("/api/other/path"));

            // Assert
            Assert.Same(fixedStrategy, s1);
        }
    }
}
