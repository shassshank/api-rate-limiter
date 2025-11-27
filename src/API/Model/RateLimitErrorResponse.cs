namespace API.Model
{
    public class RateLimitErrorResponse
    {
        public string Error { get; set; } = "rate_limit_exceeded";
        public string Strategy { get; set; } = default!;
        public string? Reason { get; set; }
        public int? Limit { get; set; }
        public int? Remaining { get; set; }
        public int? RetryAfterSeconds { get; set; }  // optional for future
    }
}
