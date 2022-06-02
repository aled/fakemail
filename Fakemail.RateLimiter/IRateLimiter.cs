
namespace Fakemail.RateLimiter
{
    public interface IRateLimiter<K> where K : notnull, IComparable<K>
    {
        bool IsRateLimited(K key, out TimeSpan retryAfter);
    }
}