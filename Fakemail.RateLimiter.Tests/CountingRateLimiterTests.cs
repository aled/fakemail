using System;

using FluentAssertions;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace Fakemail.RateLimiter.Tests
{
    public class CountingRateLimiterTests
    {
        [Fact]
        public void RateLimiterShouldWork()
        {
            var initialTime = new DateTime(2000, 1, 1);
            var timeProvider = new FakeTimeProvider(initialTime);
            var rateLimiter = new CountingRateLimiter<string>(Options.Create(new CountingRateLimiterOptions
            {
                RateLimitDefinitions =
                [
                    new CountingRateLimitDefinition { MaxRequests = 3, Period = TimeSpan.FromSeconds(1) },
                    new CountingRateLimitDefinition { MaxRequests = 30, Period = TimeSpan.FromSeconds(60) },
                    new CountingRateLimitDefinition { MaxRequests = 50, Period = TimeSpan.FromMinutes(15) }
                ],
                CacheSize = 2
            }), timeProvider);

            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((true, 1000)); // Exceeded 3 requests in 1 second

            rateLimiter.IsRateLimited("b").Should().Be((false, 0));
            rateLimiter.IsRateLimited("b").Should().Be((false, 0));
            rateLimiter.IsRateLimited("b").Should().Be((false, 0));
            rateLimiter.IsRateLimited("b").Should().Be((true, 1000)); // Exceeded 3 requests in 1 second

            rateLimiter.IsRateLimited("c").Should().Be((false, 0)); // this should evict cache key 'a', as the cache size is 2.
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 1000)); // Exceeded 3 requests in 1 second

            // item a should be allowed again immediately, as it was evicted. Now 'b' should be evicted
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((true, 1000)); // Exceeded 3 requests in 1 second

            // item c should be allowed again after 1000 ms
            timeProvider.Advance(TimeSpan.FromMilliseconds(999));
            rateLimiter.IsRateLimited("c").Should().Be((true, 1));
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0)); // started a new second - 3 requests allowed
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 1000));

            // make 30 requests in under 60s, (though don't go over 3 requests in one second!). There are already 6 requests that were not limited
            // up to this point, so need another 24
            timeProvider.Advance(TimeSpan.FromSeconds(1));

            for (int i = 0; i < 24; i++)
            {
                timeProvider.Advance(TimeSpan.FromSeconds(0.5));
                rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            }

            timeProvider.Advance(TimeSpan.FromSeconds(0.5));
            var timeToNextMinute = initialTime.AddMinutes(1) - timeProvider.GetUtcNow();
            rateLimiter.IsRateLimited("c").Should().Be((true, (long)timeToNextMinute.TotalMilliseconds));
            timeProvider.Advance(timeToNextMinute);
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));

            // make 50 requests in 15 minutes. There are already 31 requests that were not limited up to this point.
            for (int i = 0; i < 19; i++)
            {
                timeProvider.Advance(TimeSpan.FromSeconds(6));
                rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            }
            var timeToNext15Mins = initialTime.AddMinutes(15) - timeProvider.GetUtcNow();
            rateLimiter.IsRateLimited("c").Should().Be((true, (long)timeToNext15Mins.TotalMilliseconds));

            timeProvider.Advance(timeToNext15Mins);
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
        }
    }
}