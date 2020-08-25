using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

using MimeKit;

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Math.EC.Rfc7748;
using Org.BouncyCastle.Utilities.Collections;

using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;

using StackExchange.Redis;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace FakemailSmtpServer
{
    // Class that guarantees a unique timestamp every time it is used
    // Uses millisecond resolution.
    static class UniqueTimestamp
    {
        private static Stopwatch _stopwatch = new Stopwatch();
        private static long _startTicks;
        private static long _prevousMillis = -1;
        private static object _previousMillisLock = new object();

        static UniqueTimestamp()
        {
            _stopwatch.Start();
            _startTicks = DateTime.UtcNow.Ticks;
        }

        public static long Millis
        {
            get
            {
                var millis = (_startTicks + _stopwatch.ElapsedTicks) / 10000;

                lock (_previousMillisLock)
                {
                    if (millis <= _prevousMillis)
                    {
                        millis = _prevousMillis + 1;
                        _prevousMillis = millis;
                    }
                }
                return millis;
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            IServiceCollection ServiceCollection = new ServiceCollection();

            var redisOptions = new ConfigurationOptions();
            redisOptions.EndPoints.Add("localhost", 6379);
            redisOptions.Password = "Password1!";

            ServiceCollection.AddSingleton<IConnectionMultiplexer>(x => ConnectionMultiplexer.Connect(redisOptions));

            ServiceCollection.AddSingleton<IDistributedCache>(x => new RedisCache(new RedisCacheOptions {ConfigurationOptions = redisOptions}));

            ServiceCollection.AddTransient<IMessageStore, MessageStore>();
            ServiceCollection.AddTransient<IMessageStoreFactory, MessageStoreFactory>();

            IServiceProvider Services = ServiceCollection.BuildServiceProvider();

            var options = new SmtpServerOptionsBuilder()
                .ServerName("fakemail.stream")
                .Port(12025, 12465, 12587)
                .MessageStore(Services.GetRequiredService<IMessageStoreFactory>())
                .MailboxFilter(new SampleMailboxFilter())
                .UserAuthenticator(new SampleUserAuthenticator())
                .Build();

            var smtpServer = new SmtpServer.SmtpServer(options);
            await smtpServer.StartAsync(CancellationToken.None);

            Console.WriteLine("Exiting");
        }
    }

    public class MessageStoreFactory : IMessageStoreFactory
    {
        private IMessageStore _messageStore;

        public MessageStoreFactory(IMessageStore messageStore)
        {
            _messageStore = messageStore;
        }

        public IMessageStore CreateInstance(ISessionContext context)
        {
            return _messageStore;
        }
    }

    public class MessageStore : IMessageStore
    {
        private IConnectionMultiplexer _redis { get; set; }

        public MessageStore(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                var textMessage = (ITextMessage)transaction.Message;

                var message = MimeMessage.Load(textMessage.Content);

                string messageId = UniqueTimestamp.Millis.ToString("00000000000000");

                Console.WriteLine(message.TextBody);

                Directory.CreateDirectory("/tmp/fakemail");
                await File.WriteAllTextAsync($"/tmp/fakemail/{messageId}.txt", message.TextBody);

                var db = _redis.GetDatabase(1);

                foreach (var mailbox in message.To.Mailboxes)
                {
                    Console.WriteLine("TO: " + mailbox);

                    // some simple validation
                    if (mailbox.Address.Length > 100)
                        continue;

                    var split = mailbox.Address.Split("@");
                    if (split.Length != 2)
                        continue;

                    // reorder the mailbox so that the domain comes first (for easy sorting by domain)
                    // do the gmail trick allowing users to put a '+' symbol in the user name, so fred+1@z.com and fred+2@z.com go to the same address.
                    var user = new string(split[0].TakeWhile(x => x != '+').ToArray());
                    var domain = split[1];
                    var reversedAddress = domain + "@" + user;

                    Console.WriteLine("Adding to key 'mailbox-addresses': " + reversedAddress);

                    await db.SortedSetAddAsync("mailbox-addresses", reversedAddress, 0, CommandFlags.None);

                    // add the message ID to each mailbox
                    await db.SortedSetAddAsync("mailbox-index:" + reversedAddress, messageId, 0, CommandFlags.None);

                    // store the message as a MIME object
                    using (var s = new MemoryStream())
                    {
                        message.WriteTo(s);
                        await db.StringSetAsync("message-full:" + messageId, s.ToArray(), TimeSpan.FromDays(7));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                throw;
            }
            return SmtpResponse.Ok;
        }
    }

    public class SampleMailboxFilter : IMailboxFilter, IMailboxFilterFactory
    {
        public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            return Task.FromResult(MailboxFilterResult.Yes);

            if (string.Equals(from.Host, "test.com"))
            {
                return Task.FromResult(MailboxFilterResult.Yes);
            }
            return Task.FromResult(MailboxFilterResult.NoPermanently);
        }

        public Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            return Task.FromResult(MailboxFilterResult.Yes);
        }

        public IMailboxFilter CreateInstance(ISessionContext context)
        {
            return new SampleMailboxFilter();
        }
    }

    public class SampleUserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
    {
        public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {
            Console.WriteLine("User={0} Password={1}", user, password);

            return Task.FromResult(user.Length > 1);
        }

        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            return new SampleUserAuthenticator();
        }
    }
}
