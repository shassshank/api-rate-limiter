namespace API.Configuration
{
    public class RateLimitingOptions
    {
        // Used by fixed/sliding
        public int WindowSeconds { get; set; } = 60;
        public int MaxRequests { get; set; } = 10;

        // Used by token bucket
        public int TokenBucketCapacity { get; set; } = 20;
        public double TokenBucketRefillPerSecond { get; set; } = 0.5;
    }
}
