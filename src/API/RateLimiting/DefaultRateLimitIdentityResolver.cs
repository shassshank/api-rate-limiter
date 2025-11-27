using System.Security.Claims;

namespace API.RateLimiting
{
    public class DefaultRateLimitIdentityResolver : IRateLimitIdentityResolver
    {
        private readonly IApiKeyRegistry _apiKeyRegistry;
        private readonly ILogger<DefaultRateLimitIdentityResolver> _logger;

        public DefaultRateLimitIdentityResolver(
            IApiKeyRegistry apiKeyRegistry,
            ILogger<DefaultRateLimitIdentityResolver> logger)
        {
            _apiKeyRegistry = apiKeyRegistry;
            _logger = logger;
        }

        public string GetIdentity(HttpContext context)
        {
            var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(apiKey) &&
                _apiKeyRegistry.TryGet(apiKey, out var entry) &&
                entry is not null)
            {
                var plan = string.IsNullOrWhiteSpace(entry.Plan) ? "UnknownPlan" : entry.Plan;
                _logger.LogDebug("Resolved API key identity. Key={Key}, Plan={Plan}", apiKey, plan);

                return $"key:{plan}:{apiKey}";
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // Invalid key - we can log it but still fall back to user/IP
                _logger.LogWarning("Unrecognized API key used: {ApiKey}", apiKey);
            }

            // Authenticated user
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? context.User.Identity?.Name;

                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return $"user:{userId}";
                }
            }

            // Fallback to IP address
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ip}";
        }
    }
}
