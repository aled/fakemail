using System;

using Microsoft.Extensions.Options;

using Xunit;
using FluentAssertions;
using System.Linq;

namespace Fakemail.RateLimiter.Tests
{
    public class CountingRateLimiterTests
    {
        [Fact]
        public void RateLimiterShouldWork()
        {
            var initialTime = new DateTime(2000, 1, 1);
            var clock = new DummyClock(initialTime);
            var rateLimiter = new CountingRateLimiter<string>(Options.Create(new CountingRateLimiterOptions 
            { 
                RateLimitDefinitions = new[] 
                { 
                    new CountingRateLimitDefinition { MaxRequests = 3, Period = TimeSpan.FromSeconds(1) },
                    new CountingRateLimitDefinition { MaxRequests = 30, Period = TimeSpan.FromSeconds(60) },
                    new CountingRateLimitDefinition { MaxRequests = 50, Period = TimeSpan.FromMinutes(15) }
                }.ToList(),
                CacheSize = 2
            }), clock);

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
            clock.Advance(TimeSpan.FromMilliseconds(999));
            rateLimiter.IsRateLimited("c").Should().Be((true, 1));
            clock.Advance(TimeSpan.FromMilliseconds(1));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0)); // started a new second - 3 requests allowed
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 1000));

            // make 30 requests in under 60s, (though don't go over 3 requests in one second!). There are already 6 requests that were not limited
            // up to this point, so need another 24
            clock.Advance(TimeSpan.FromSeconds(1));

            for (int i = 0; i < 24; i++)
            {
                clock.Advance(TimeSpan.FromSeconds(0.5));
                rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            }

            clock.Advance(TimeSpan.FromSeconds(0.5));
            var timeToNextMinute = initialTime.AddMinutes(1) - clock.UtcNow;
            rateLimiter.IsRateLimited("c").Should().Be((true, (long)timeToNextMinute.TotalMilliseconds));
            clock.Advance(timeToNextMinute);
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));

            // make 50 requests in 15 minutes. There are already 31 requests that were not limited up to this point.
            for (int i = 0; i < 19; i++)
            {
                clock.Advance(TimeSpan.FromSeconds(6));
                rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            }
            var timeToNext15Mins = initialTime.AddMinutes(15) - clock.UtcNow;
            rateLimiter.IsRateLimited("c").Should().Be((true, (long)timeToNext15Mins.TotalMilliseconds));

            clock.Advance(timeToNext15Mins);
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
        }
    }
}