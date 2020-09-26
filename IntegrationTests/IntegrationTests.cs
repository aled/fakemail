using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Smtp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using Serilog;

using SmtpServer.Storage;

/// <summary>
/// To start a local redis server for these tests to work:
/// redis-server --appendonly no
///
/// To connect to this instance in another window:
/// redis-cli
/// select 1
/// </summary>
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
                DatabaseNumber = 1
            };

            Log.Logger = new LoggerConfiguration()
               .WriteTo.Console()
               .CreateLogger();

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<SmtpService>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IRedisConfiguration>(x => redisConfiguration);
                    services.AddSingleton<IDataStorage, RedisDataStorage>();
                    services.AddSingleton<IMessageStoreFactory, MessageStoreFactory>();
                    services.AddSingleton<IMessageStore, Smtp.MessageStore>();
                    services.AddSingleton<IMailboxFilterFactory, MailboxFilterFactory>();
                    services.AddSingleton<IMailboxFilter, Smtp.MailboxFilter>();
                });
        }

        [Test]
        public async Task Test1()
        {
            var host = CreateHostBuilder().Build();

            await host.StartAsync();
            var engine = host.Services.GetRequiredService<IEngine>();
            var result = await engine.CreateMailboxAsync("e67f0c7f-4947-4be3-abe4-a1f1b3f3aef7@fakemail.stream");

            Assert.IsTrue(result.Success);
            var mailbox = result.Mailbox;

            int mailCount = 10;
            var subjects = new string[mailCount];
            var bodies = new string[mailCount];
            var tasks = new List<Task>();
            for (int i = 0; i < mailCount; i++)
            {
                subjects[i] = $"subject-{Guid.NewGuid()}";
                bodies[i] = $"body-{Guid.NewGuid()}";
                var mailMessage = new MailMessage("from@test.com", mailbox, subjects[i], bodies[i]);

                var smtpClient = new SmtpClient("localhost", 12025);
                tasks.Add(Task.Run(() => smtpClient.Send(mailMessage)));
            }
            await Task.WhenAll(tasks);

            // Query the engine to show the emails were stored
            var messageSummaries = await engine.GetMessageSummaries(mailbox, 0, mailCount);

            for (int i = 0; i < mailCount; i++)
            {
                Assert.AreEqual(1, messageSummaries.Where(x => x.Subject == subjects[i]).Count(), subjects[i]);
                Assert.AreEqual(1, messageSummaries.Where(x => x.Body.Trim() == bodies[i]).Count(), bodies[i]);
            }

            await host.StopAsync();
        }
    }
}
