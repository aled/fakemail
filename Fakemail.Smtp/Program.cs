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

using SmtpServer.Authentication;
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
                    var smtpConfiguration = new SmtpConfiguration();
                    hostContext.Configuration.GetSection("Smtp").Bind(smtpConfiguration);

                    services.AddSingleton(smtpConfiguration);
                    services.AddHostedService<SmtpService>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IUserAuthenticatorFactory, FakemailUserAuthenticatorFactory>();
                    services.AddSingleton<IUserAuthenticator, FakemailUserAuthenticator>();
                    services.AddSingleton<IDataStorage, EntityFrameworkDataStorage>();
                    services.AddSingleton<IMessageStoreFactory, MessageStoreFactory>();
                    services.AddSingleton<IMessageStore, FakemailMessageStore>();
                    services.AddSingleton<IMailboxFilterFactory, MailboxFilterFactory>();
                    services.AddSingleton<IMailboxFilter, FakemailMailboxFilter>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                    configHost.AddJsonFile("fakemail.settings.json", optional: true);
                    configHost.AddCommandLine(args);
                });
        }
    }
}
