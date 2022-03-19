using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Data.EntityFramework;
using Fakemail.Smtp;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

using SmtpServer.Authentication;
using SmtpServer.Storage;

using Xunit;
using System.Text;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;

namespace Fakemail.IntegrationTests
{
    public class SmtpFixture : IAsyncLifetime
    {
        IHost _host;
        public SmtpService SmtpService { get; set; }

        public async Task InitializeAsync()
        {
            Log.Logger = new LoggerConfiguration()
              .WriteTo.Console()
              .CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var smtpConfiguration = new SmtpConfiguration();
                    hostContext.Configuration.GetSection("Smtp").Bind(smtpConfiguration);

                    services.AddSingleton(smtpConfiguration);
                    services.AddHostedService<SmtpService>();
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IUserAuthenticatorFactory, FakemailUserAuthenticatorFactory>();
                    services.AddSingleton<IUserAuthenticator, FakemailUserAuthenticator>();
                    services.AddSingleton<IDataStorage, EntityFrameworkDataStorage>();
                    services.AddSingleton<IMessageStoreFactory, MessageStoreFactory>();
                    services.AddSingleton<IMessageStore, FakemailMessageStore>();
                    services.AddSingleton<IMailboxFilterFactory, MailboxFilterFactory>();
                    services.AddSingleton<IMailboxFilter, FakemailMailboxFilter>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());

                    var json = JsonSerializer.Serialize(new Dictionary<object, object>
                    {
                        { "Smtp", new SmtpConfiguration { Ports = new List<int> { -1 } } }
                    });
                    var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    
                    configHost.AddJsonStream(jsonStream);
                    
                })
                .Build();

            await _host.StartAsync();

            // Check if smtp service started
            SmtpService = _host.Services.GetService<IEnumerable<IHostedService>>().First(x => x is SmtpService) as SmtpService;
            if (!SmtpService.IsRunning)
            {
                throw new Exception("SmtpService did not start");
            }
        }
        
        public async Task DisposeAsync()
        {
            await _host.StopAsync();
        }
    }

    public class SmtpTests : IClassFixture<SmtpFixture>
    {
        private int _port;

        public SmtpTests(SmtpFixture smtpFixture)
        {
            _port = smtpFixture.SmtpService.Ports.First();
        }

        [Fact]
        public async Task SendEmail()
        {
            string logFile = "c:\\temp\\NetworkTrace.Log";
            TextWriterTraceListener listener = new TextWriterTraceListener(logFile);
            
            Trace.Listeners.Add(listener);
            Trace.AutoFlush = true;

            var smtpClient = new SmtpClient("192.168.1.165", _port);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential
            {
                UserName = "asdf",
                Password = "qwer"
            };
            smtpClient.EnableSsl = false;

            var email = new MailMessage();
            email.Subject = "Subject";
            email.Body = "Body";
            email.From = new MailAddress("From@From");
            email.To.Add(new MailAddress("To@To"));

            await smtpClient.SendMailAsync(email);
        }
    }
}
