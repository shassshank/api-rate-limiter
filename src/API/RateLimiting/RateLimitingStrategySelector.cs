using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using API.Configuration;

namespace API.RateLimiting
{
    public interface IRateLimitingStrategySelector
    {
        IRateLimitingStrategy SelectStrategy(HttpContext context);
    }

    public class RateLimitingStrategySelector : IRateLimitingStrategySelector
{
    private readonly IEnumerable<IRateLimitingStrategy> _strategies;
    private readonly RateLimitPolicyOptions _policyOptions;

    public RateLimitingStrategySelector(
        IEnumerable<IRateLimitingStrategy> strategies,
        IOptions<RateLimitPolicyOptions> policyOptions)
    {
        _strategies = strategies;
        _policyOptions = policyOptions.Value;
    }

    public IRateLimitingStrategy SelectStrategy(HttpContext context)
    {
        var path = (context.Request.Path.Value ?? string.Empty).ToLowerInvariant();

        foreach (var rule in _policyOptions.Rules)
        {
            var prefix = rule.PathPrefix.ToLowerInvariant();

            if (path.StartsWith(prefix))
            {
                var strategy = _strategies.FirstOrDefault(
                    s => string.Equals(s.Name, rule.Strategy, StringComparison.OrdinalIgnoreCase));

                if (strategy != null)
                    return strategy;
            }
        }

        // default/fallback
        var fixedWindow = _strategies.FirstOrDefault(s => s.Name == "FixedWindow");
        return fixedWindow ?? _strategies.First();
    }
}

}
