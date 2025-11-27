namespace API.Configuration
{
    public class RateLimitPolicyRule
    {
        public string PathPrefix { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
    }

    public class RateLimitPolicyOptions
    {
        public List<RateLimitPolicyRule> Rules { get; set; } = new();
    }
}
