using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.Options;

namespace Fakemail.RateLimiter.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Program>();
        }
    
        private IRateLimiter<int> bucketRateLimiter = new BucketRateLimiter<int>(Options.Create(
            new BucketRateLimiterOptions { 
                Burst = 10000000, 
                CacheSize = 20000, 
                RequestsPerSecond = 10000000f 
            }));

        private IRateLimiter<int> countingRateLimiter = new CountingRateLimiter<int>(Options.Create(
            new CountingRateLimiterOptions
            {
                CacheSize = 20000,
                RateLimitDefinitions = new[] {
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
                }.ToList()
            }));

        private int current = 0;

        /// <summary>
        /// This should be essentialy never rate limited
        /// </summary>
        [Benchmark]
        public void BucketRateLimiter()
        {
            bucketRateLimiter.IsRateLimited(current++, out var _);
        }

        /// <summary>
        /// This should be essentialy never rate limited
        /// </summary>
        [Benchmark]
        public void CountingRateLimiter() 
        {
            countingRateLimiter.IsRateLimited(current++, out var _);
        }
    }
}