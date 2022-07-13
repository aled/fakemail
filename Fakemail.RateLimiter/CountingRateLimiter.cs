using System;

using Microsoft.Extensions.Options;

namespace Fakemail.RateLimiter
{
    static class Extensions
    {
        public static DateTime RoundDown(this DateTime dt, TimeSpan period)
        {
            return new DateTime(dt.Ticks - dt.Ticks % period.Ticks);
        }
    }

    public class CountingRateLimiter<K> : IRateLimiter<K>
        where K : notnull, IComparable<K>
    {
        /// <summary>
        /// Use a fixed struct (no references) to reduce memory usage
        /// TODO: use smaller datatypes if necessary
        /// </summary>
        struct Counts
        {
            public DateTime lastUpdate;
            public int countA;
            public int countB;
            public int countC;
        }

        private IClock _clock;
        private RateLimiterCache<K, Counts> _cache;
        private CountingRateLimiterOptions _options;
        private readonly object _lock = new object();
        
        public CountingRateLimiter(IOptions<CountingRateLimiterOptions> options) : this(options, new SystemClock())
        {
        }

        public CountingRateLimiter(IOptions<CountingRateLimiterOptions> options, IClock clock)
        {
            CountingRateLimiterOptionsValidator.Validate(options.Value);

            _options = options.Value;
            _clock = clock ?? new SystemClock();
            _cache = new RateLimiterCache<K, Counts>(_options.CacheSize, _clock);
        }

        private DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private bool Process(DateTime lastUpdate, DateTime now, Span<int> oldCount, Span<int> newCount, CountingRateLimitDefinition definition, ref DateTime retryAt)
        {
            newCount[0] = 1;

            if (lastUpdate.RoundDown(definition.Period) == now.RoundDown(definition.Period))
                newCount[0] += oldCount[0];

            if (newCount[0] > definition.MaxRequests)
            {
                retryAt = Max(retryAt, now.RoundDown(definition.Period) + definition.Period);
                return true;
            }
            return false;
        }

        public bool IsRateLimited(K key, out TimeSpan retryAfter)
        {
            var isRateLimited = false;
            var now = _clock.UtcNow;
            var retryAt = now;

            Span<int> newCounts = stackalloc int[] { 1, 1, 1 };
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var oldValue, out _))
                {
                    Span<int> oldCounts = stackalloc int[] { oldValue.countA, oldValue.countB, oldValue.countC };

                    // Validation of RateLimitDefinitions ensures that the count is no more than 3.
                    for (int i = 0; i < _options.RateLimitDefinitions.Count; i++)
                    {
                        isRateLimited |= Process(oldValue.lastUpdate, now, oldCounts.Slice(i, 1), newCounts.Slice(i, 1), _options.RateLimitDefinitions[i], ref retryAt);
                    }
                }

                // Don't update counts for rate-limited requests.
                // TODO: Maybe counts should be updated on rate-limited requests?
                if (isRateLimited)
                {
                    _cache.Set(key, new Counts { lastUpdate = now, countA = oldValue.countA, countB = oldValue.countB, countC = oldValue.countC });
                }
                else
                {
                    _cache.Set(key, new Counts { lastUpdate = now, countA = newCounts[0], countB = newCounts[1], countC = newCounts[2] });
                }
            }
            retryAfter = retryAt - now;
            return isRateLimited;
        }
    }
}
