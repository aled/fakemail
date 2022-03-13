using System;
using System.IO;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Data.EntityFramework;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args)
                .UseSystemd()
                .Build()
                .RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<SmtpService>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IDataStorage, EntityFrameworkDataStorage>();
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
