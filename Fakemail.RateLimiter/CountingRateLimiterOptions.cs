namespace Fakemail.RateLimiter
{
    public class CountingRateLimiterOptions
    {
        public List<CountingRateLimitDefinition> RateLimitDefinitions { get; set; } = [];
        public int CacheSize { get; set; }
    }
}
