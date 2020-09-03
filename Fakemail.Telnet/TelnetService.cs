using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Fakemail.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

namespace Fakemail.Telnet
{
    public class TelnetService : IHostedService
    {
        Task _serverTask;

        private ILogger<TelnetService> _log;
        private IHostApplicationLifetime _lifetime;
        private IEngine _engine;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TelnetService(ILogger<TelnetService> log, IHostApplicationLifetime lifetime, IEngine engine)
        {
            _log = log;
            _lifetime = lifetime;
            _engine = engine;
        }

        private void OnStarted()
        {
            _log.LogInformation("TelnetService: OnStarted");
        }

        private void OnStopping()
        {
            _log.LogInformation("TelnetService: OnStopping");
        }

        private void OnStopped()
        {
            _log.LogInformation("TelnetService: OnStopped");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting hosted service");

            _lifetime.ApplicationStarted.Register(OnStarted);
            _lifetime.ApplicationStopping.Register(OnStopping);
            _lifetime.ApplicationStopped.Register(OnStopped);

            Log.Information("Starting Telnet server");

            _serverTask = new TelnetServer().StartAsync(_engine, _cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Stopping Telnet hosted service");
            _cancellationTokenSource.Cancel();
            await _serverTask;
        }
    }
}
