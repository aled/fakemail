using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using SmtpServer;
using SmtpServer.Storage;

namespace Fakemail.Smtp
{
    public class Server : IHostedService
    {
        Task _serverTask;

        private ILogger<Server> _log;
        private IHostApplicationLifetime _lifetime; 
        private IMessageStoreFactory _messageStoreFactory;
        private IMailboxFilterFactory _mailboxFilterFactory;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        
        public Server(ILogger<Server> log, IHostApplicationLifetime lifetime, IMessageStoreFactory messageStoreFactory, IMailboxFilterFactory mailboxFilterFactory)
        {
            _log = log;
            _lifetime = lifetime;
            _messageStoreFactory = messageStoreFactory;
            _mailboxFilterFactory = mailboxFilterFactory;
        }

        private void OnStarted()
        {
            _log.LogInformation("OnStarted");
        }

        private void OnStopping()
        {
            _log.LogInformation("OnStopping");
        }

        private void OnStopped()
        {
            _log.LogInformation("OnStopped");
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting hosted service");

            _lifetime.ApplicationStarted.Register(OnStarted);
            _lifetime.ApplicationStopping.Register(OnStopping);
            _lifetime.ApplicationStopped.Register(OnStopped);

            var options = new SmtpServerOptionsBuilder()
               .ServerName("fakemail.stream")
               .Port(12025, 12465, 12587)
               .MessageStore(_messageStoreFactory)
               .MailboxFilter(_mailboxFilterFactory)
               .Build();

            Log.Information("Starting SMTP server");

            _serverTask = new SmtpServer.SmtpServer(options).StartAsync(_cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Stopping hosted service");
            _cancellationTokenSource.Cancel();
            await _serverTask;
        }
    }
}
