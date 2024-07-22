using System;

namespace Fakemail.RateLimiter.Tests
{
    internal static class Extensions
    {
        public static (bool, long) IsRateLimited<K>(this IRateLimiter<K> r, K key)
            where K : notnull, IComparable<K>
        {
            bool ret = r.IsRateLimited(key, out TimeSpan retryAfter, out var _);

            // because the calculations are floating point, round to nearest millisecond for testing
            var x = (long)retryAfter.Add(TimeSpan.FromMilliseconds(0.5)).TotalMilliseconds;
            return (ret, x);
        }
    }
}