using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SmtpServer;
using SmtpServer.Authentication;
using SmtpServer.ComponentModel;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class SmtpService : IHostedService
    {
        private Task _serverTask;

        private ILogger<SmtpService> _log;
        private IHostApplicationLifetime _lifetime;
        private IMessageStoreFactory _messageStoreFactory;
        private IMailboxFilterFactory _mailboxFilterFactory;
        private IUserAuthenticatorFactory _userAuthenticatorFactory;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public SmtpService(ILogger<SmtpService> log, 
            IHostApplicationLifetime lifetime, 
            IMessageStoreFactory messageStoreFactory, 
            IMailboxFilterFactory mailboxFilterFactory,
            IUserAuthenticatorFactory userAuthenticatorFactory)
        {
            _log = log;
            _lifetime = lifetime;
            _messageStoreFactory = messageStoreFactory;
            _mailboxFilterFactory = mailboxFilterFactory;
            _userAuthenticatorFactory = userAuthenticatorFactory;
        }

        private void OnStarted()
        {
            _log.LogInformation("Started Smtp service");
        }

        private void OnStopping()
        {
            _log.LogInformation("Stopping Smtp service");
        }

        private void OnStopped()
        {
            _log.LogInformation("Stopped Smtp service");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting Smtp service");

            _lifetime.ApplicationStarted.Register(OnStarted);
            _lifetime.ApplicationStopping.Register(OnStopping);
            _lifetime.ApplicationStopped.Register(OnStopped);

            var options = new SmtpServerOptionsBuilder()
               .ServerName("fakemail.stream")
               .Port(12025, 12465, 12587)
               .CommandWaitTimeout(TimeSpan.FromSeconds(10))
               .MaxAuthenticationAttempts(1)
               .MaxMessageSize(100 * 1024)
               .MaxRetryCount(0)
               .NetworkBufferSize(4096)
               .Build();

            var serviceProvider = new ServiceProvider();
            serviceProvider.Add(_messageStoreFactory);
            serviceProvider.Add(_mailboxFilterFactory);
            serviceProvider.Add(_userAuthenticatorFactory);

            _serverTask = new SmtpServer.SmtpServer(options, serviceProvider).StartAsync(_cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Cancelling Smtp service");
            _cancellationTokenSource.Cancel();
            await _serverTask;
        }
    }
}
