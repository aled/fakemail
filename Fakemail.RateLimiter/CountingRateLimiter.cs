using System.Text;

using Microsoft.Extensions.Options;

namespace Fakemail.RateLimiter
{
    internal static class Extensions
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
        private struct Counts
        {
            public DateTime lastUpdate;
            public int countA;
            public int countB;
            public int countC;
        }

        private readonly TimeProvider _timeProvider;
        private readonly RateLimiterCache<K, Counts> _cache;
        private readonly CountingRateLimiterOptions _options;
        private readonly object _lock = new();

        public CountingRateLimiter(IOptions<CountingRateLimiterOptions> options) : this(options, TimeProvider.System)
        {
        }

        public CountingRateLimiter(IOptions<CountingRateLimiterOptions> options, TimeProvider timeProvider)
        {
            CountingRateLimiterOptionsValidator.Validate(options.Value);

            _options = options.Value;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _cache = new RateLimiterCache<K, Counts>(_options.CacheSize, _timeProvider);
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static bool Process(DateTime lastUpdate, DateTime now, Span<int> oldCount, Span<int> newCount, CountingRateLimitDefinition definition, ref DateTime retryAt)
        {
            newCount[0] = 1;

            if (lastUpdate.RoundDown(definition.Period) == now.RoundDown(definition.Period))
                newCount[0] += oldCount[0];

            if (newCount[0] > definition.MaxRequests)
            {
                retryAt = CountingRateLimiter<K>.Max(retryAt, now.RoundDown(definition.Period) + definition.Period);
                return true;
            }
            return false;
        }

        public bool IsRateLimited(K key, out TimeSpan retryAfter, out string stats)
        {
            var isRateLimited = false;
            var now = _timeProvider.GetUtcNow().DateTime;
            var retryAt = now;

            Span<int> newCounts = [1, 1, 1];
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var oldValue, out _))
                {
                    Span<int> oldCounts = [oldValue.countA, oldValue.countB, oldValue.countC];

                    // Validation of RateLimitDefinitions ensures that the count is no more than 3.
                    for (int i = 0; i < _options.RateLimitDefinitions.Count; i++)
                    {
                        isRateLimited |= CountingRateLimiter<K>.Process(oldValue.lastUpdate, now, oldCounts.Slice(i, 1), newCounts.Slice(i, 1), _options.RateLimitDefinitions[i], ref retryAt);
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

            // Generate stats
            var statsBuilder = new StringBuilder();
            for (int i = 0; i < _options.RateLimitDefinitions.Count; i++)
            {
                var limit = _options.RateLimitDefinitions[i].MaxRequests;
                var period = _options.RateLimitDefinitions[i].Period;
                if (i > 0) statsBuilder.Append(';');
                statsBuilder.Append($"{newCounts[i]}/{limit},{(int)period.TotalSeconds}s");
            }
            stats = statsBuilder.ToString();

            return isRateLimited;
        }
    }
}
