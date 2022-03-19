using System;
using System.Linq;
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
        public int[] Ports { get; private set; }

        private ILogger<SmtpService> _log;
        private IHostApplicationLifetime _lifetime;
        private IMessageStoreFactory _messageStoreFactory;
        private IMailboxFilterFactory _mailboxFilterFactory;
        private IUserAuthenticatorFactory _userAuthenticatorFactory;
        private SmtpConfiguration _smtpConfiguration;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public SmtpService(ILogger<SmtpService> log, 
            IHostApplicationLifetime lifetime, 
            IMessageStoreFactory messageStoreFactory, 
            IMailboxFilterFactory mailboxFilterFactory,
            IUserAuthenticatorFactory userAuthenticatorFactory,
            SmtpConfiguration smtpConfiguration)
        {
            _log = log;
            _lifetime = lifetime;
            _messageStoreFactory = messageStoreFactory;
            _mailboxFilterFactory = mailboxFilterFactory;
            _userAuthenticatorFactory = userAuthenticatorFactory;
            _smtpConfiguration = smtpConfiguration;
        }

        // IHostedService events
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

        // SmtpServer events
        private void OnListenerStarted(object sender, ListenerEventArgs e)
        {
            _log.LogInformation($"Smtp listener started: Port {e.EndpointDefinition.Endpoint.Port}");
        }

        private void OnListenerFaulted(object sender, ListenerFaultedEventArgs e)
        {
            _log.LogInformation($"Smtp listener faulted: Port {e.EndpointDefinition.Endpoint.Port}");
        }

        private void OnSessionFaulted(object sender, SessionFaultedEventArgs e)
        {
            _log.LogInformation("Smtp session faulted");
        }

        public bool IsRunning => _serverTask != null;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting Smtp service");

            _lifetime.ApplicationStarted.Register(OnStarted);
            _lifetime.ApplicationStopping.Register(OnStopping);
            _lifetime.ApplicationStopped.Register(OnStopped);

            // try up to 5 times in case port is in use.
            int triesRemaining = 5;
            while (_serverTask == null && triesRemaining > 0)
            {
                Ports = _smtpConfiguration.Ports.Select(x => x > 0 ? x : Random.Shared.Next(1024, 65535)).ToArray();

                var options = new SmtpServerOptionsBuilder()
                   .ServerName("fakemail.stream")
                   .Endpoint(builder => builder.Port(Ports.First()).AuthenticationRequired(true).AllowUnsecureAuthentication(true))
                   .CommandWaitTimeout(TimeSpan.FromSeconds(10))
                   .MaxAuthenticationAttempts(1)
                   .MaxMessageSize(100 * 1024)
                   .MaxRetryCount(1)
                   .NetworkBufferSize(4096)
                   .Build();

                var serviceProvider = new ServiceProvider();
                serviceProvider.Add(_messageStoreFactory);
                serviceProvider.Add(_mailboxFilterFactory);
                serviceProvider.Add(_userAuthenticatorFactory);

                var smtpServer = new SmtpServer.SmtpServer(options, serviceProvider);

                smtpServer.SessionFaulted += new EventHandler<SessionFaultedEventArgs>(OnSessionFaulted);
                smtpServer.ListenerStarted += new EventHandler<ListenerEventArgs>(OnListenerStarted);
                smtpServer.ListenerFaulted += new EventHandler<ListenerFaultedEventArgs>(OnListenerFaulted);

                if (smtpServer.OpenEndpointListeners())
                {
                    _serverTask = smtpServer.StartAsync(_cancellationTokenSource.Token);
                }
            }
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Cancelling Smtp service");
            _cancellationTokenSource.Cancel();
            if (_serverTask != null)
            {
                await _serverTask;
            }
        }
    }
}
