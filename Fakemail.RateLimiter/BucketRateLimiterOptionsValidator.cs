using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.RateLimiter
{
    public class BucketRateLimiterOptionsValidator
    {
        public static void Validate(BucketRateLimiterOptions options)
        {
            if (options.Burst < 1)
            {
                throw new ArgumentException("Must not be less than 1", nameof(options.Burst));
            }

            if (options.RequestsPerSecond <= 0)
            {
                throw new ArgumentException("Must be greater than 0", nameof(options.RequestsPerSecond));
            }

            if (options.CacheSize <= 1)
            {
                throw new ArgumentException("Must be greater than 1", nameof(options.CacheSize));
            }
        }
    }
}
