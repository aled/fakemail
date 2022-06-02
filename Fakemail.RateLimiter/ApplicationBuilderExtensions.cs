using System;

using Microsoft.AspNetCore.Builder;

namespace Fakemail.RateLimiter
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseIpRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpRateLimitingMiddleware>();
        }
    }
}
