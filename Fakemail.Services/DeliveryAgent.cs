using System;
using System.Collections.Concurrent;

using Fakemail.ApiModels;
using Fakemail.Core;
using Fakemail.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

namespace Fakemail.Services
{
    public class DeliveryAgentConfig
    {
        public string IncomingDirectory { get; set; } = "c:\\temp\\fakemail\\incoming";
        public string FailedDirectory { get; set; } = "c:\\temp\\fakemail\\failed";
        public int PollSeconds { get; set; } = 30;
    }

    // To be run either as a standalone executable, or as a worker service within the API
    public class DeliveryAgent
    {
        private ILogger<DeliveryAgent> _log;
        private DeliveryAgentConfig _config;  
        private IEngine _engine;
        private BlockingCollection<string> _queue = new BlockingCollection<string>(new ProducerConsumerSet<string>(), 2);

        public static async Task Main(string[] args)
        {
            // Usage:
            //  Fakemail.DeliveryAgent [-poll [seconds]] directory
            var config = new DeliveryAgentConfig();

            for (int i = 0; i < args.Length; i++)
            {
                if (i == args.Length - 1)
                {
                    config.IncomingDirectory = args[i];
                }
                else if (args[i] == "-poll")
                {
                    config.PollSeconds = Convert.ToInt32(args[++i]);
                }
            }

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var jwtSigningKey = "gfjherjhjhkdgfjhkgdfjhkgdfjhkgfdhjdfghjkfdg";

            var host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContextFactory<FakemailDbContext>(options => options.UseSqlite($"Data Source={"c:\\temp\\fakemail.sqlite"}"));
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IJwtAuthentication>(new JwtAuthentication(jwtSigningKey));
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FakemailDbContext>();
                db.Database.EnsureCreated();
            }

            var engine = host.Services.GetRequiredService<IEngine>();
            var log = host.Services.GetRequiredService<ILogger<DeliveryAgent>>();
        
            var cancellationToken = new CancellationTokenSource().Token;

            await new DeliveryAgent(config, engine, log).RunAsync(cancellationToken);
        }

        public DeliveryAgent(DeliveryAgentConfig config, IEngine engine, ILogger<DeliveryAgent> log)
        {
            _log = log;
            _config = config;
            _engine = engine;
        }

        private void OnRenamed(object source, FileSystemEventArgs e)
        {
            // In this application we should be able to process the changed file immediately,
            // as it is moved rather than created.
            _queue.Add(e.Name);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var directoryInfo = new DirectoryInfo(_config.IncomingDirectory);
            var failedDirectoryInfo = new DirectoryInfo(_config.FailedDirectory);

            var watcher = new FileSystemWatcher(directoryInfo.FullName);

            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            watcher.Renamed += OnRenamed;
            watcher.Created += OnRenamed;

            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = false;
            
            var pollTimestamp = DateTime.MinValue;
            var pollTimeSpan = TimeSpan.FromSeconds(_config.PollSeconds);
            var pollPeriodMillis = (int)(pollTimeSpan.TotalMilliseconds);

            while (!cancellationToken.IsCancellationRequested)
            {
                // if we are due a poll, do it now, but only if the queue is empty. Only update the timestamp if it completes fully
                // (it will stop if the queue gets full)
                if (DateTime.Now - pollTimestamp >= pollTimeSpan && !_queue.Any())
                {
                    _log.LogInformation("Polling now...");
                    var completedPoll = true;

                    foreach (var file in directoryInfo.GetFiles())
                    {
                        if (!_queue.TryAdd(file.Name))
                        {
                            _log.LogInformation("Pausing poll - queue full");
                            completedPoll = false;
                            break;
                        }
                    }
                    if (completedPoll)
                    {
                        _log.LogInformation("Poll completed");
                        pollTimestamp = DateTime.Now;
                    }
                }
                else
                {
                    if (_queue.TryTake(out var filename, pollPeriodMillis, cancellationToken))
                    {
                        var fileInfo = new FileInfo(Path.Join(directoryInfo.FullName, filename));

                        // Open in read/write mode to ensure it isn't still being written to.
                        try
                        {
                            _log.LogInformation($"Delivering: {fileInfo.Name}");
                            using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite))
                            {
                                await _engine.CreateEmailAsync(stream);
                            }
                            File.Delete(fileInfo.FullName);
                        }
                        catch (IOException ioe)
                        {
                            // This is mainly to temporary issues (e.g. locked files), which should be retried.
                            // There are potentially some permanent issues (e.g. permissions) which should not be retried.
                            // TODO: Don't have infinite retries
                            _log.LogError(ioe.Message);
                        }
                        catch (Exception e)
                        {
                            _log.LogError(e.Message);
                            try
                            {
                                File.Move(fileInfo.FullName, Path.Join(failedDirectoryInfo.FullName, fileInfo.Name));
                            }
                            catch (Exception e2)
                            {
                                _log.LogError(e2.Message);
                            }
                        }
                    }
                }
            }

            watcher.Dispose();
        }
    }
}
