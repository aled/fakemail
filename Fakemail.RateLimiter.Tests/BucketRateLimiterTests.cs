using System;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Xunit;
using FluentAssertions;

namespace Fakemail.RateLimiter.Tests
{
    public class BucketRateLimiterTests
    {
        [Fact]
        public void RateLimiterShouldWork()
        {
            var timeProvider = new FakeTimeProvider(new DateTime(2000, 1, 1));
            var rateLimiter = new BucketRateLimiter<string>(Options.Create(new BucketRateLimiterOptions { Burst = 3, CacheSize = 2, RequestsPerSecond = 4f }), timeProvider);

            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((true, 250)); // Exceeded burst limit

            rateLimiter.IsRateLimited("b").Should().Be((false, 0));
            rateLimiter.IsRateLimited("b").Should().Be((false, 0));
            rateLimiter.IsRateLimited("b").Should().Be((false, 0));
            rateLimiter.IsRateLimited("b").Should().Be((true, 250));  // Exceeded burst limit

            rateLimiter.IsRateLimited("c").Should().Be((false, 0)); // this should evict cache key 'a', as the cache size is 2.
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 250));  // Exceeded burst limit

            // item a should be allowed again immediately, as it was evicted. Now 'b' should be evicted
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((false, 0));
            rateLimiter.IsRateLimited("a").Should().Be((true, 250)); // Exceeded burst limit

            // item c should be allowed again after 250 ms, since the rate limit is 4 req/s
            timeProvider.Advance(TimeSpan.FromMilliseconds(249));
            rateLimiter.IsRateLimited("c").Should().Be((true, 1));
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));

            // Fails due to rounding errors if we try to keep the clock on the 
            // exact boundary of where the rate limiting kicks in.
            // Advance the clock by 1ms to avoid these rounding errors. Nasty.
            timeProvider.Advance(TimeSpan.FromMilliseconds(1));

            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 249));

            // Every 250 ms, we should be allowed one more request
            timeProvider.Advance(TimeSpan.FromMilliseconds(250));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 249));

            timeProvider.Advance(TimeSpan.FromMilliseconds(250));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 249));

            // Allow the burst level to reduce. By waiting 500ms we should be allowed to burst 2 requests.
            timeProvider.Advance(TimeSpan.FromMilliseconds(500));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 249));

            // Allow the burst level to reduce. By waiting 750ms we should be allowed to burst 3 requests.
            timeProvider.Advance(TimeSpan.FromMilliseconds(750));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 250));

            // Waiting longer does not allow to burst any more than 3 requests.
            timeProvider.Advance(TimeSpan.FromMilliseconds(1000));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((false, 0));
            rateLimiter.IsRateLimited("c").Should().Be((true, 250));
        }
    }
}