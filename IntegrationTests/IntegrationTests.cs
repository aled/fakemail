using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.Data;
using Fakemail.Smtp;

using NUnit.Framework;

namespace Fakemail.IntegrationTests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public async Task Test1()
        {
            var redisConfiguration = new RedisConfiguration
            {
                Host = "localhost",
                Port = 6379,
                Password = "Password1!",
                DatabaseNumber = 15
            };

            var cts = new CancellationTokenSource();
            var serverTask = new Server().RunAsync(redisConfiguration, cts.Token);

            var smtpClient = new SmtpClient("localhost", 12025);
            var mailMessage = new MailMessage("from@test.com", "fred@fakemail.stream", "subject", "body");

            smtpClient.SendAsync(mailMessage, null);

            // TODO: query the engine to show the email was stored
            // Right now, need to manually inspect redis.
            Thread.Sleep(5000); // seems to be long enough

            cts.Cancel();
            await serverTask;
        }
    }
}
