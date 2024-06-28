using Microsoft.Extensions.Options;

namespace Fakemail.RateLimiter
{
    /// <summary>
    /// Rate limiter. Modelled as a leaky bucket that can hold a number of requests equal to the burst limit.
    /// Each request that is allowed is put into the bucket. If a request would overflow the bucket, it is not allowed.
    /// The level of requests in the bucket naturally leaks out over time at the requests-per-second rate.
    /// </summary>
    /// <typeparam name="K">Type of the key used for rate limiting</typeparam>
    public class BucketRateLimiter<K> : IRateLimiter<K> 
        where K : notnull, IComparable<K>
    {
        private readonly BucketRateLimiterOptions _options;
        private readonly IClock _clock;
        private readonly RateLimiterCache<K, float> _cache;
        private readonly object _lock = new();
        private double _secondsPerRequest;

        public BucketRateLimiter(IOptions<BucketRateLimiterOptions> options) : this(options, new SystemClock())
        {
        }

        public BucketRateLimiter(IOptions<BucketRateLimiterOptions> options, IClock clock)
        {
            BucketRateLimiterOptionsValidator.Validate(options.Value);

            _options = options.Value;
            _clock = clock ?? new SystemClock();
            _cache = new RateLimiterCache<K, float>(_options.CacheSize, _clock);

            _secondsPerRequest = 1f / _options.RequestsPerSecond;
        }

        /// <summary>
        /// Check whether request is permitted or rate-limited, and update the rate-limiting cache.
        /// </summary>
        /// <param name="key">Key for aggregating requests for the purpose of rate-limiting, e.g. IP address, username</param>
        /// <param name="retryAfter">The time to wait until a request will be allowed</param>
        /// <returns></returns>
        public bool IsRateLimited(K key, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            var isRateLimited = false;
            var now = _clock.UtcNow;

            // increment the level by 1 for each request.
            // TODO: allow different requests to have different increments
            var increment = 1.0d;
            var newLevel = increment;

            // Look up the level of this key in the cache
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out float oldLevel, out DateTime lastUpdated))
                {
                    // Recalculate level. It will have reduced by requests-per-second for every second
                    // since the timestamp was last calculated.
                    var timeSinceLastCalculated = now.Subtract(lastUpdated);

                    // This can go negative because the lastUpdated as stored by the cache can be later
                    // then the real value, because it makes timestamps unique by incrementing them.
                    if (timeSinceLastCalculated < TimeSpan.Zero)
                        timeSinceLastCalculated = TimeSpan.Zero;

                    var level = oldLevel - (timeSinceLastCalculated.Ticks * _options.RequestsPerSecond / 10000000);

                    // This happens when enough time has passed since the last update, that the bucket emptied
                    if (level < 0)
                        level = 0;

                    // This should never happen
                    if (level > _options.Burst)
                        level = _options.Burst;

                    // if the level plus the level of the new request would exceed the burst limit, then the request should be rate-limited
                    var candidateLevel = level + increment;

                    if (candidateLevel > _options.Burst)
                    {
                        isRateLimited = true;
                        newLevel = level;

                        // calculate how long until the next request is allowed
                        var fractionOfIncrementToWait = (candidateLevel - _options.Burst) / increment;
                        retryAfter = TimeSpan.FromSeconds(fractionOfIncrementToWait * _secondsPerRequest);
                    }
                    else
                    {
                        newLevel = candidateLevel;
                    }
                }

                _cache.Set(key, (float)newLevel);
                return isRateLimited;
            }
        }
    }
}