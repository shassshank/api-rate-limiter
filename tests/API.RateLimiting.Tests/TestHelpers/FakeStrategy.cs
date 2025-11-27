using System.Threading;
using System.Threading.Tasks;
using API.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace API.RateLimiting.Tests.TestHelpers
{
    public class FakeStrategy : IRateLimitingStrategy
    {
        public string Name { get; }

        public FakeStrategy(string name)
        {
            Name = name;
        }

        public Task<RateLimitDecision> ShouldAllowAsync(
            HttpContext context,
            CancellationToken cancellationToken = default)
        {
            // Not used in these tests
            return Task.FromResult(new RateLimitDecision(true));
        }
    }
}
