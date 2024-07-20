namespace Fakemail.RateLimiter
{
    /// <summary>
    /// Rate limiter is modelled as a bucket which has a size equal to the 'burst' paramater.
    /// Every time a request is accepted, the level in the bucket increases by 1.
    /// The bucket empties itself at a rate equal to the 'requestsPerSecond' parameter.
    ///
    /// If a request would cause the bucket to overflow, the request is simply discarded.
    ///
    /// The number of buckets is limited; those with the lowest levels are discarded if needed.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    public class BucketRateLimiterOptions
    {
        public float RequestsPerSecond { get; set; }
        public int Burst { get; set; }
        public int CacheSize { get; set; }
    }
}