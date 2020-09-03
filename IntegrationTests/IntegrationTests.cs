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
using System.Collections.Generic;
using Org.BouncyCastle.Math.EC.Rfc7748;

/// <summary>
/// To start a local redis server for these tests to work:
/// redis-server --requirepass Password1! --appendonly no
/// 
/// To connect to this instance in another window:
/// redis-cli
/// auth Password1!
/// select 15
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
                Password = "Password1!",
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

            

            var engine = host.Services.GetRequiredService<IEngine>();
            var result = await engine.CreateMailboxAsync("e67f0c7f-4947-4be3-abe4-a1f1b3f3aef7@fakemail.stream");

            Assert.IsTrue(result.Success);
            var mailbox = result.Mailbox;
            var subjects = new string[10];
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                subjects[i] = $"subject-{Guid.NewGuid()}";
                var mailMessage = new MailMessage("from@test.com", mailbox, subjects[i], "body");
                
                var smtpClient = new SmtpClient("localhost", 12025);
                tasks.Add(Task.Run(() => smtpClient.Send(mailMessage)));
            }
            await Task.WhenAll(tasks);

            // Query the engine to show the email was stored
            var messageSummaries = await engine.GetMessageSummaries(mailbox, 0, 10);

            Assert.AreEqual(1, messageSummaries.Where(x => x.Subject == subjects[0]).Count());

            await host.StopAsync();
        }
    }
}