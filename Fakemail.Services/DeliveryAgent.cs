using System;
using System.Collections.Concurrent;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Serilog;

using Fakemail.Core;
using Fakemail.Data.EntityFramework;
using Fakemail.ApiModels;

namespace Fakemail.Services
{
    // To be run either as a standalone executable, or as a worker service within the API
    public class DeliveryAgent : BackgroundService
    {
        private ILogger<DeliveryAgent> _log;
        private DeliveryAgentOptions _options;
        private IEngine _engine;
        private BlockingCollection<string> _queue = new BlockingCollection<string>(new ProducerConsumerSet<string>(), 2);

        public static async Task Main(string[] args)
        {
            // Usage:
            //  Fakemail.DeliveryAgent -p seconds -f <fail directory> -n <new mail directory>
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            IConfigurationRoot configRoot = null;

            var host = Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    // this reads the 'Environment' environment variable, which should set to Development in the debug profile
                    configHost.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.Sources.Clear();
                    config.AddJsonFile("appsettings.json", true);
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true);
                    config.AddCommandLine(args, new Dictionary<string, string>
                    {
                        { "-n", "IncomingDirectory" },
                        { "-f", "FailedDirectory" },
                        { "-p", "PollSeconds" },
                    });
                    configRoot = config.Build();
                })
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var connectionString = hostContext.Configuration.GetConnectionString("fakemail");
                    services.AddDbContextFactory<FakemailDbContext>(options => options.UseSqlite(connectionString));

                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddHttpClient<IPwnedPasswordApi, PwnedPasswordApi>();

                    // TODO: make a JwtOptions class, similar to DeliveryAgentOptions
                    var jwtSecret = hostContext.Configuration["Jwt:Secret"];
                    var jwtValidIssuer = hostContext.Configuration["Jwt:ValidIssuer"];
                    var jwtExpiryMinutes = Convert.ToInt32(hostContext.Configuration["Jwt:ExpiryMinutes"]);
                    services.AddSingleton<IJwtAuthentication>(new JwtAuthentication(jwtSecret, jwtValidIssuer, jwtExpiryMinutes));

                    services.Configure<DeliveryAgentOptions>(hostContext.Configuration.GetSection("DeliveryAgent"));
                    services.AddHostedService<DeliveryAgent>();
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

            await host.RunAsync();
        }

        public DeliveryAgent(IEngine engine, ILogger<DeliveryAgent> log, IOptions<DeliveryAgentOptions> options)
        {
            _log = log;
            _options = options.Value;
            _engine = engine;
        }

        private void OnRenamed(object source, FileSystemEventArgs e)
        {
            _queue.Add(e.Name);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var directoryInfo = new DirectoryInfo(_options.IncomingDirectory);
            var failedDirectoryInfo = new DirectoryInfo(_options.FailedDirectory);

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
            var pollTimeSpan = TimeSpan.FromSeconds(_options.PollSeconds);
            var pollPeriodMillis = (int)(pollTimeSpan.TotalMilliseconds);

            while (!cancellationToken.IsCancellationRequested)
            {
                // if we are due a poll, do it now, but only if the queue is empty. Only update the timestamp if it completes fully
                // (it will stop if the queue gets full)
                if (DateTime.Now - pollTimestamp >= pollTimeSpan && !_queue.Any())
                {
                    _log.LogTrace("Polling now...");
                    var completedPoll = true;

                    foreach (var file in directoryInfo.GetFiles())
                    {
                        if (!_queue.TryAdd(file.Name))
                        {
                            _log.LogTrace("Pausing poll - queue full");
                            completedPoll = false;
                            break;
                        }
                    }
                    if (completedPoll)
                    {
                        _log.LogTrace("Poll completed");
                        pollTimestamp = DateTime.Now;
                    }
                }
                else
                {
                    if (_queue.TryTake(out var filename, pollPeriodMillis, cancellationToken))
                    {
                        var fileInfo = new FileInfo(Path.Join(directoryInfo.FullName, filename));

                        try
                        {
                            _log.LogInformation($"Delivering: {fileInfo.Name}");

                            // Open in read/write mode to ensure it isn't still being written by the SMTP server.
                            CreateEmailResponse response = null;
                            using (var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite))
                            {
                                response = await _engine.CreateEmailAsync(stream);
                            }
                            
                            if (response.Success) 
                            {
                                File.Delete(fileInfo.FullName);
                            }
                            else
                            {
                                _log.LogError(response.ErrorMessage);
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
                        catch (IOException ioe)
                        {
                            // This is mainly for temporary issues (e.g. locked files), which should be retried.
                            // There are potentially some permanent issues (e.g. permissions) which should not be retried.
                            // TODO: Don't have infinite retries
                            _log.LogError(ioe.Message);
                        }
                        catch (Exception e)
                        {
                            _log.LogError(e.Message);

                        }
                    }
                }
            }

            watcher.Dispose();
        }
    }
}
