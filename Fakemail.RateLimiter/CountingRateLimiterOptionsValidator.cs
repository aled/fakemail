namespace Fakemail.RateLimiter
{
    public class CountingRateLimiterOptionsValidator
    {
        public static void Validate(CountingRateLimiterOptions options)
        {
            if (options.RateLimitDefinitions == null || options.RateLimitDefinitions.Count < 1)
            {
                throw new ArgumentException("Must be at least one rate limit definition", nameof(options.RateLimitDefinitions));
            }

            if (options.RateLimitDefinitions.Count > 3)
            {
                throw new ArgumentException("Must be 3 or fewer rate limit definition", nameof(options.RateLimitDefinitions));
            }

            foreach (var d in options.RateLimitDefinitions)
            {
                if (d.Period < TimeSpan.FromSeconds(1))
                    throw new ArgumentException("Minimum period is 1 second", nameof(options.RateLimitDefinitions) + "." + nameof(d.Period));

                if (d.MaxRequests < 1)
                    throw new ArgumentException("Must be at least 1", nameof(options.RateLimitDefinitions) + "." + nameof(d.MaxRequests));
            }

            if (options.CacheSize <= 1)
            {
                throw new ArgumentException("Must be greater than 1", nameof(options.CacheSize));
            }
        }
    }
}
