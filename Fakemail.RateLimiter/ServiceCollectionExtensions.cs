using System;

using Microsoft.Extensions.DependencyInjection;

namespace Fakemail.RateLimiter
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddClientIpRateLimiting(this IServiceCollection services)
        {
            services.AddSingleton<IRateLimiter<uint>, CountingRateLimiter<uint>>();
            return services;
        }
    }
}
