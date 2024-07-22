using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.Options;

namespace Fakemail.RateLimiter.Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<Program>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
        private readonly IRateLimiter<int> bucketRateLimiter = new BucketRateLimiter<int>(Options.Create(
            new BucketRateLimiterOptions
            {
                Burst = 10000000,
                CacheSize = 20000,
                RequestsPerSecond = 10000000f
            }));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
        private readonly IRateLimiter<int> countingRateLimiter = new CountingRateLimiter<int>(Options.Create(
            new CountingRateLimiterOptions
            {
                CacheSize = 20000,
                RateLimitDefinitions = [
                    new CountingRateLimitDefinition {
                        MaxRequests = 10000000,
                        Period = TimeSpan.FromSeconds(100)
                    },
                    new CountingRateLimitDefinition {
                        MaxRequests = 10000000,
                        Period = TimeSpan.FromSeconds(100)
                    },
                    new CountingRateLimitDefinition {
                        MaxRequests = 10000000,
                        Period = TimeSpan.FromSeconds(100)
                    }
                ]
            }));

        private int current = 0;

        /// <summary>
        /// This should never be rate limited
        /// </summary>
        [Benchmark]
        public void BucketRateLimiter()
        {
            bucketRateLimiter.IsRateLimited(current++, out var _, out var _);
        }

        /// <summary>
        /// This should never be rate limited
        /// </summary>
        [Benchmark]
        public void CountingRateLimiter()
        {
            countingRateLimiter.IsRateLimited(current++, out var _, out var _);
        }
    }
}