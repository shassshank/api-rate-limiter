using Microsoft.AspNetCore.Builder;

namespace API.Middleware
{
    public static class RateLimitingApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the custom rate limiting middleware to the pipeline.
        /// </summary>
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RateLimiterMiddleware>();
        }
    }
}
