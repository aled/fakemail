using System;

namespace Fakemail.RateLimiter
{
    /// <summary>
    /// Memory cache with a well-defined eviction policy.
    /// 
    /// The cache has a hard capacity limit, with the least-recently-updated item evicted when necessary
    /// 
    /// There is no expiry of items, they will simply be evicted when newer ones are stored. 
    /// </summary>
    /// <typeparam name="K">Type of the cache key</typeparam>
    /// <typeparam name="V">Type of the cache value</typeparam>
    /// <param name="capacity">
    /// Capacity of the cache
    /// </param>
    /// <param name="timeProvider">
    /// Source of time. Abstracting this allows unit testing
    /// </param>
    public class RateLimiterCache<K, V>(int capacity, TimeProvider timeProvider)
        where K : notnull
        where V : notnull
    {
        /// <summary>
        /// Main store of the cache items. Can be looked up by key.
        /// </summary>
        private readonly Dictionary<K, (V value, DateTime updateTime)> _values = new(capacity);

        /// <summary>
        /// Store the last-updated-timestamp of each cache entry in sorted order
        /// </summary>
        private readonly SortedDictionary<DateTime, K> _keysByUpdateTime = [];

        /// <summary>
        /// The last-updated-timestamp dictionary requires unique timestamps.
        /// To force uniqueness, increment the timestamp by as many ticks as 
        /// necessary. This retains the 'least-recently-updated' eviction policy.
        /// </summary>
        private uint tickUniquifier = 0;
        private DateTime _previous;

        /// <summary>
        /// Set a value in the cache
        /// </summary>
        /// <param name="key">The key of the cached item</param>
        /// <param name="value">The value of the cached item</param>
        public void Set(K key, V value)
        {
            if (_values.TryGetValue(key, out var old))
            {
                _keysByUpdateTime.Remove(old.updateTime);
            }
            else
            {
                if (_values.Count >= capacity)
                {
                    // evict oldest entry
                    var (time, oldestKey) = _keysByUpdateTime.First();
                    _keysByUpdateTime.Remove(time);
                    _values.Remove(oldestKey);

                    //Console.WriteLine($"Evicted key {oldestKey}");
                }
            }

            // Get current time,
            var now = timeProvider.GetUtcNow().DateTime;

            // round to 100ms
            // This isn't necessary, but exercises the timestamp uniquifying code
            //var ticks = _timeProvider.UtcNow.Ticks;
            //var now = new DateTime(ticks - ticks % 100000);

            if (_previous == now)
            {
                now = now.AddTicks(tickUniquifier);
            }
            else
            {
                tickUniquifier = 0;
                _previous = now;
            }

            // Keep incrementing the timestamp until we get one that has not been used before
            while (_keysByUpdateTime.ContainsKey(now))
            {
                tickUniquifier++;
                now = now.AddTicks(1);
            }

            _keysByUpdateTime[now] = key;
            _values[key] = (value, now);
        }

        /// <summary>
        /// Get the value of this cache key, and the time it was last updated
        /// </summary>
        /// <param name="key">The cache key</param>
        /// <param name="value">The value of the cached item</param>
        /// <param name="lastUpdatedTime">The last updated time of the cached item</param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V value, out DateTime lastUpdatedTime)
        {
            if (_values.TryGetValue(key, out var item))
            {
                value = item.value;
                lastUpdatedTime = item.updateTime;
                return true;
            }
            value = default!;
            lastUpdatedTime = default;
            return false;
        }
    }
}
