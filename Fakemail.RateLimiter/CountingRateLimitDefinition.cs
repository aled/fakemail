namespace Fakemail.RateLimiter
{
    public class CountingRateLimitDefinition
    {
        public int MaxRequests { get; set; }
        public TimeSpan Period { get; set; }
    }
}
