namespace Fakemail.RateLimiter
{
    public class CountingRateLimiterOptions
    {
        public List<CountingRateLimitDefinition> RateLimitDefinitions { get; set; } = new List<CountingRateLimitDefinition>();
        public int CacheSize { get; set; }
    }
}
