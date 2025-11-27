using API.RateLimiting;
using System.Text.Json;
using API.Model;

namespace API.Middleware
{
    public class RateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitingStrategySelector _selector;
        private readonly ILogger<RateLimiterMiddleware> _logger;

        public RateLimiterMiddleware(
            RequestDelegate next,
            IRateLimitingStrategySelector selector,
            ILogger<RateLimiterMiddleware> logger)
        {
            _next = next;
            _selector = selector;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            if (path.StartsWith("/swagger") || path.StartsWith("/health"))
            {
                await _next(context);
                return;
            }

            var strategy = _selector.SelectStrategy(context);
            var decision = await strategy.ShouldAllowAsync(context);

            if (!decision.Allowed)
            {
                _logger.LogWarning(
                    "Request blocked by rate limiting strategy {Strategy} on path {Path}. Reason: {Reason}",
                    strategy.Name,
                    path,
                    decision.Reason ?? "n/a");

                // Set rate limit headers on block
                if (decision.Limit.HasValue)
                {
                    context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.Value.ToString();
                }

                if (decision.Remaining.HasValue)
                {
                    var remaining = Math.Max(0, decision.Remaining.Value);
                    context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                }

                // compute RetryAfterSeconds
                if (decision.RetryAfterSeconds.HasValue)
                {
                    context.Response.Headers["Retry-After"] = decision.RetryAfterSeconds.Value.ToString();
                }

                var errorBody = new RateLimitErrorResponse
                {
                    Strategy = strategy.Name,
                    Reason = decision.Reason,
                    Limit = decision.Limit,
                    Remaining = decision.Remaining,
                    RetryAfterSeconds = decision.RetryAfterSeconds
                };

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(errorBody);
                await context.Response.WriteAsync(json);

                return;
            }

            if (decision.Limit.HasValue)
            {
                context.Response.Headers["X-RateLimit-Limit"] = decision.Limit.Value.ToString();
            }

            if (decision.Remaining.HasValue)
            {
                var remaining = Math.Max(0, decision.Remaining.Value);
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            }

            await _next(context);
        }

    }
}
