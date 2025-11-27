using API.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace API.RateLimiting.Tests.TestHelpers
{
    public class TestIdentityResolver : IRateLimitIdentityResolver
    {
        private readonly string _identity;

        public TestIdentityResolver(string identity = "test-identity")
        {
            _identity = identity;
        }

        public string GetIdentity(HttpContext context) => _identity;
    }
}
