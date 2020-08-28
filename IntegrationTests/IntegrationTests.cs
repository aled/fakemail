using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Smtp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using SmtpServer.Storage;

using Serilog;
using System;
using System.Linq;

namespace Fakemail.IntegrationTests
{
    [TestFixture]
    public class IntegrationTests
    {
        private IHostBuilder CreateHostBuilder()
        {
            var redisConfiguration = new RedisConfiguration
            {
                Host = "localhost",
                Port = 6379,
                Password = "Password1!",
                DatabaseNumber = 15
            };

            Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .CreateLogger();

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Server>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IRedisConfiguration>(x => redisConfiguration);
                    services.AddSingleton<IDataStorage, RedisDataStorage>();
                    services.AddSingleton<IMessageStoreFactory, MessageStoreFactory>();
                    services.AddSingleton<IMessageStore, Fakemail.Smtp.MessageStore>();
                    services.AddSingleton<IMailboxFilterFactory, MailboxFilterFactory>();
                    services.AddSingleton<IMailboxFilter, Fakemail.Smtp.MailboxFilter>();
                });
        }

        [Test]
        public async Task Test1()
        {
            var host = CreateHostBuilder().Build();

            await host.StartAsync();

            var smtpClient = new SmtpClient("localhost", 12025);

            var guid = Guid.NewGuid();
            var mailMessage = new MailMessage("from@test.com", "fred@fakemail.stream", $"subject-{guid}", "body");

            smtpClient.Send(mailMessage);

            // Query the engine to show the email was stored
            var engine = host.Services.GetRequiredService<IEngine>();

            var latestMessageSummary = (await engine.GetMessageSummaries("fred@fakemail.stream", 0, 10)).First();

            Assert.AreEqual($"subject-{guid}", latestMessageSummary.Subject);

            await host.StopAsync();
        }
    }
}