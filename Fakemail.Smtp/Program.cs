using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Exceptions;
using SmtpServer.Storage;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Telnet;

namespace Fakemail.Smtp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await CreateHostBuilder(args)
                .UseSystemd()
                .Build()
                .RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .Enrich.WithExceptionDetails()
               .WriteTo.Console()
               .CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            var redisConfiguration = new RedisConfiguration
            {
                Host = "localhost",
                Port = 6379,
                Password = "Password1!",
                DatabaseNumber = 1
            };

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<SmtpService>();
                    services.AddHostedService<TelnetService>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IRedisConfiguration>(x => redisConfiguration);
                    services.AddSingleton<IDataStorage, RedisDataStorage>();
                    services.AddSingleton<IMessageStoreFactory, MessageStoreFactory>();
                    services.AddSingleton<IMessageStore, MessageStore>();
                    services.AddSingleton<IMailboxFilterFactory, MailboxFilterFactory>();
                    services.AddSingleton<IMailboxFilter, MailboxFilter>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("fakemail.config", optional: true);
                    configHost.AddCommandLine(args);
                });
        } 
    }
}
