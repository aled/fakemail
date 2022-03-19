using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data;
using Fakemail.Data.EntityFramework;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MimeKit;

using Serilog;

using Xunit;

namespace Fakemail.IntegrationTests
{
    public class EngineTests
    {
        private static IEngine CreateEngine()
        {
            return CreateHostBuilder(new string[] {"-connectionString", ""})
              .Build()
              .Services
              .GetRequiredService<IEngine>();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IDataStorage, EntityFrameworkDataStorage>();                   
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                });
        }

        [Fact]
        public async Task CreateUser()
        {
            var engine = CreateEngine();

            var username = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            var result = await engine.CreateUserAsync(
                new ApiModels.User {
                    Username = username,
                    Password = password
                }
            );

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUser_WillNotCreateDuplicate()
        {
            var engine = CreateEngine();

            var username = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            var result = await engine.CreateUserAsync(
                new ApiModels.User
                {
                    Username = username,
                    Password = password
                }
            );

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            result = await engine.CreateUserAsync(
               new ApiModels.User
               {
                   Username = username,
                   Password = password
               }
           );

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("User already exists");
        }

        [Fact]
        public async Task AuthenticateUser_WrongPassword()
        {
            var engine = CreateEngine();

            var user = new ApiModels.User
            {
                Username = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString()
            };

            var result = await engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            user.Password = "WrongPassword!!!";
            var auth = await engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(false);
        }

        [Fact]
        public async Task AuthenticateUser_CorrectPassword()
        {
            var engine = CreateEngine();

            var user = new ApiModels.User
            {
                Username = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString()
            };

            var result = await engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            var auth = await engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(true);
        }

        [Fact]
        public async Task AuthenticateUser_NoSuchUser()
        {
            var engine = CreateEngine();

            var user = new ApiModels.User
            {
                Username = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString()
            };

            var result = await engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            user.Username = "NoSuchUser!!!";
            var auth = await engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(false);
        }

        private static MimeMessage GenerateEmail()
        {
            var from = new InternetAddressList(new InternetAddress[] { new MailboxAddress("Fred Flintstone", "fred@flintstone.com") });
            var to = new InternetAddressList(new InternetAddress[] { new MailboxAddress("Barney Rubble", "barney@rubble.com") });
            var body = new TextPart("plain")
            {
                Text = "... dabba doo"
            };
            return new MimeMessage(from, to, "Yabba...", body);
        }

        [Fact]
        public async Task CreateEmail()
        {
            var engine = CreateEngine();

            var username = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();

            var result = await engine.CreateUserAsync(
               new ApiModels.User
               {
                   Username = username,
                   Password = password
               }
            );

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            var email = GenerateEmail();
            
            await engine.OnEmailReceivedAsync(
                username,
                "fred@flintstone.com",
                new[] { "barny@rubble.com", "wilma@flintstone.com" },
                new Dictionary<string, string>(),
                email);
        }

        [Fact]
        public async Task ListEmails()
        {
            var engine = CreateEngine();

            var user = new ApiModels.User
            {
                Username = Guid.NewGuid().ToString(),
                Password = Guid.NewGuid().ToString()
            };

            var result = await engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            var email = GenerateEmail();

            await engine.OnEmailReceivedAsync(
                user.Username,
                "fred@flintstone.com",
                new[] { "barny@rubble.com", "wilma@flintstone.com" },
                new Dictionary<string, string>(),
                email);

            var emailsResult = await engine.ReadEmailsAsync(user, 0, 10);

            emailsResult.Emails.Length.Should().Be(1);
        }
    }
}