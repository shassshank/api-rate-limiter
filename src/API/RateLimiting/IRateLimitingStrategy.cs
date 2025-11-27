namespace API.RateLimiting
{
    public interface IRateLimitingStrategy
    {
        string Name { get; }

        Task<RateLimitDecision> ShouldAllowAsync(
            HttpContext context,
            CancellationToken cancellationToken = default);
    }

    public record RateLimitDecision(
        bool Allowed,
        string? Reason = null,
        int? Limit = null,
        int? Remaining = null,
        int? RetryAfterSeconds = null);
}
