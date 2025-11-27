namespace API.RateLimiting
{
    public interface IRateLimitIdentityResolver
    {
        /// <summary>
        /// Returns a stable identifier used for rate limiting,
        /// e.g. user id, API key, or IP address.
        /// </summary>
        string GetIdentity(HttpContext context);
    }
}
