using System;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

using SmtpServer;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class Server
    {
        public Task RunAsync(RedisConfiguration redisConfiguration, CancellationToken cancellationToken)
        {
            IServiceCollection services = new ServiceCollection();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            services.AddSingleton(Log.Logger);
            services.AddSingleton<IEngine, Engine>();
            services.AddSingleton<IRedisConfiguration>(x => redisConfiguration);
            services.AddSingleton<IDataStorage, RedisDataStorage>();
            services.AddSingleton<IMessageStoreFactory, MessageStoreFactory>();
            services.AddSingleton<IMessageStore, MessageStore>();
            services.AddSingleton<IMailboxFilterFactory, MailboxFilterFactory>();
            services.AddSingleton<IMailboxFilter, MailboxFilter>();

            var provider = services.BuildServiceProvider();

            var options = new SmtpServerOptionsBuilder()
                .ServerName("fakemail.stream")
                .Port(12025, 12465, 12587)
                .MessageStore(provider.GetRequiredService<IMessageStoreFactory>())
                .MailboxFilter(provider.GetRequiredService<IMailboxFilterFactory>())
                .Build();

            Log.Information("Starting SMTP server");

            var smtpServer = new SmtpServer.SmtpServer(options);

            return smtpServer.StartAsync(cancellationToken);
        }
    }
}
