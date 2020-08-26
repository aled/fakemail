using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fakemail.Core;
using Fakemail.Data;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using Org.BouncyCastle.Asn1.TeleTrust;
using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using StackExchange.Redis;

namespace FakemailSmtpServer
{
    class Program
    {
        static Task Main(string[] args)
        {
            return new Program().Run();
        }

        private async Task Run()
        {
            IServiceCollection ServiceCollection = new ServiceCollection();

            var redisOptions = new ConfigurationOptions();
            redisOptions.EndPoints.Add("localhost", 6379);
            redisOptions.Password = "Password1!";

            ServiceCollection.AddSingleton<IEngine, Engine>();

            ServiceCollection.AddSingleton<IConnectionMultiplexer>(x => ConnectionMultiplexer.Connect(redisOptions));
            ServiceCollection.AddSingleton<IDataStorage, RedisDataStorage>();

            ServiceCollection.AddTransient<IMessageStore, MessageStore>();
            ServiceCollection.AddTransient<IMessageStoreFactory, MessageStoreFactory>();

            var Services = ServiceCollection.BuildServiceProvider();

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
        private IEngine _engine;

        public MessageStore(IEngine engine)
        {
            _engine = engine;
        }

        public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, CancellationToken cancellationToken)
        {
            try
            {
                var textMessage = (ITextMessage)transaction.Message;
                var message = MimeMessage.Load(textMessage.Content);
                await _engine.OnEmailReceivedAsync(transaction.From.AsAddress(), transaction.To.Select(x => x.AsAddress()), transaction.Parameters, message);
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
