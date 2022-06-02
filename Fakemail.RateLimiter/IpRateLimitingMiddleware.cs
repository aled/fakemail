using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fakemail.RateLimiter
{
    public class IpRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpRateLimitingMiddleware> _logger;
        private readonly IRateLimiter<uint> _rateLimiter;

        public IpRateLimitingMiddleware(RequestDelegate next, ILogger<IpRateLimitingMiddleware> logger, IRateLimiter<uint> rateLimiter)
        {
            _next = next;
            _logger = logger;
            _rateLimiter = rateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress;

            _logger.LogInformation($"RateLimiting: checking {ipAddress}");

            var ipAddressBytes = context.Connection.RemoteIpAddress.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ipAddressBytes);

            var ipAddressUint = BitConverter.ToUInt32(ipAddressBytes, 0);

            if (_rateLimiter.IsRateLimited(ipAddressUint, out var retryAfterTimespan))
            {
                var retryAfterSeconds = Math.Ceiling(retryAfterTimespan.TotalSeconds).ToString();
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "text/plain";
                context.Response.Headers["Retry-After"] = retryAfterSeconds;
                await context.Response.WriteAsync($"Too many requests. Please try again after {retryAfterSeconds} second(s)");
                return;
            }
            await _next(context);
        }
    }
}