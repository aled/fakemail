using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Fakemail.ApiModels;
using Fakemail.Core;

namespace Fakemail.Services
{
    public class CleanupService(
        IEngine engine,
        ILogger<CleanupService> log,
        IOptions<CleanupServiceOptions> options)
        : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (options.Value.PollSecondsMin < 1)
            {
                log.LogWarning("PollSecondsMin must be greater than 0");
            }
            if (options.Value.PollSecondsMax == int.MaxValue)
            {
                log.LogWarning("PollSecondsMax must be less than int.MaxValue");
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                log.LogDebug("Cleanup running");
                var request = new CleanupEmailsRequest
                {
                    MaxEmailAgeSeconds = options.Value.MaxEmailAgeSeconds,
                    MaxEmailCount = options.Value.MaxEmailCount
                };

                var response = await engine.CleanupEmailsAsync(request, cancellationToken);

                if (response.Success)
                {
                    log.LogInformation("Cleanup deleted {count} emails", response.TotalEmailsDeleted);
                }
                else
                {
                    log.LogError("Cleanup failed {message}", response.ErrorMessage);
                }

                // Randomize the timing of the next run
                var waitTime = TimeSpan.FromSeconds(
                    Random.Shared.Next(options.Value.PollSecondsMin, options.Value.PollSecondsMax + 1));

                log.LogDebug("Cleanup service waiting for {waitTime}s to run", waitTime.Seconds);

                await Task.Delay(waitTime, cancellationToken);
            }
        }
    }
}
