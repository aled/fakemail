using System.Net;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fakemail.RateLimiter
{
    public class IpRateLimitingMiddleware(
        RequestDelegate next, 
        ILogger<IpRateLimitingMiddleware> logger, 
        IRateLimiter<uint> rateLimiter)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // try getting the X-Real-IP header, else use the remote IP address
            if (!IPAddress.TryParse(context.Request.Headers["X-Real-IP"], out var ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress;
            }

            logger.LogInformation("RateLimiting: checking {ipAddress}", ipAddress);

            var ipAddressBytes = context.Connection.RemoteIpAddress.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ipAddressBytes);

            var ipAddressUint = BitConverter.ToUInt32(ipAddressBytes, 0);

            if (rateLimiter.IsRateLimited(ipAddressUint, out var retryAfterTimespan))
            {
                var retryAfterSeconds = ((int)(1 + retryAfterTimespan.TotalSeconds)).ToString();
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "text/plain";
                context.Response.Headers["Retry-After"] = retryAfterSeconds;
                await context.Response.WriteAsync($"Too many requests. Please try again after {retryAfterSeconds} second(s)");
                return;
            }
            await next(context);
        }
    }
}