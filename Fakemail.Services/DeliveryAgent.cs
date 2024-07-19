using System;
using System.Collections.Concurrent;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Fakemail.Core;
using Fakemail.ApiModels;

namespace Fakemail.Services
{
    // To be run either as a standalone executable, or as a worker service within the API
    public class DeliveryAgent(
        IEngine engine, 
        ILogger<DeliveryAgent> log, 
        IOptions<DeliveryAgentOptions> options) 
        : BackgroundService
    {
        private readonly BlockingCollection<string> _queue = new(new ProducerConsumerSet<string>(), 2);

        private void OnRenamed(object source, FileSystemEventArgs e)
        {
            _queue.Add(e.Name);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var directoryInfo = new DirectoryInfo(options.Value.IncomingDirectory);
            var failedDirectoryInfo = new DirectoryInfo(options.Value.FailedDirectory);

            while (!cancellationToken.IsCancellationRequested && !Directory.Exists(directoryInfo.FullName))
            {
                log.LogWarning("Waiting for directory to be created: {name}", directoryInfo.FullName);
                await Task.Delay(TimeSpan.FromSeconds(300), cancellationToken);
            }

            var watcher = new FileSystemWatcher(directoryInfo.FullName)
            {
                NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size
            };

            watcher.Renamed += OnRenamed;
            watcher.Created += OnRenamed;

            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = false;

            var pollTimestamp = DateTime.MinValue;
            var pollTimeSpan = TimeSpan.FromSeconds(options.Value.PollSeconds);
            var pollPeriodMillis = (int)(pollTimeSpan.TotalMilliseconds);

            while (!cancellationToken.IsCancellationRequested)
            {
                // if we are due a poll, do it now, but only if the queue is empty. Only update the timestamp if it completes fully
                // (it will stop if the queue gets full)
                if (DateTime.UtcNow - pollTimestamp >= pollTimeSpan && _queue.Count == 0)
                {
                    log.LogTrace("Polling now...");
                    var completedPoll = true;

                    foreach (var file in directoryInfo.GetFiles())
                    {
                        if (!_queue.TryAdd(file.Name))
                        {
                            log.LogTrace("Pausing poll - queue full");
                            completedPoll = false;
                            break;
                        }
                    }
                    if (completedPoll)
                    {
                        log.LogTrace("Poll completed");
                        pollTimestamp = DateTime.UtcNow;
                    }
                }
                else
                {
                    if (_queue.TryTake(out var filename, pollPeriodMillis, cancellationToken))
                    {
                        var fileInfo = new FileInfo(Path.Join(directoryInfo.FullName, filename));

                        try
                        {
                            log.LogInformation("Delivering: {filename}", fileInfo.Name);

                            // Open in read/write mode to ensure it isn't still being written by the SMTP server.
                            CreateEmailResponse response = null;
                            using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite))
                            {
                                response = await engine.CreateEmailAsync(stream);
                            }
                            
                            if (response.Success) 
                            {
                                File.Delete(fileInfo.FullName);
                            }
                            else
                            {
                                log.LogError("Error copying email to database: {errorMessage}", response.ErrorMessage);
                                try
                                {
                                    File.Move(fileInfo.FullName, Path.Join(failedDirectoryInfo.FullName, fileInfo.Name));
                                }
                                catch (Exception e2)
                                {
                                    log.LogError("Error moving email to failed directory: {errorMessage}", e2.Message);
                                }
                            }
                        }
                        catch (IOException ioe)
                        {
                            // This is mainly for temporary issues (e.g. locked files), which should be retried.
                            // There are potentially some permanent issues (e.g. permissions) which should not be retried.
                            // TODO: Don't have infinite retries
                            log.LogError("IOException while delivering mail: {errorMessage}\n{stackTrace}", ioe.Message, ioe.StackTrace);
                        }
                        catch (Exception e)
                        {
                            log.LogError("Exception while delivering mail: {errorMessage}\n{stackTrace}", e.Message, e.StackTrace);

                        }
                    }
                }
            }

            watcher.Dispose();
        }
    }
}
