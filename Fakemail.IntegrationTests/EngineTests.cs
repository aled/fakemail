using System;
using System.IO;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data.EntityFramework;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MimeKit;

using Serilog;

using Xunit;

namespace Fakemail.IntegrationTests
{
    public class EngineFixture : IDisposable
    {
        private readonly string _dbFile = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}fakemail-enginetests-{DateTime.Now.ToString("HHmmss")}-{Utils.CreateId()}.sqlite";
        private IHost host;

        public IEngine Engine { get; set; }

        public EngineFixture()
        {
            host = CreateHostBuilder(new string[] { })
             .Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FakemailDbContext>();
                db.Database.EnsureCreated();
            }

            Engine = host.Services.GetRequiredService<IEngine>();
        }

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContextFactory<FakemailDbContext>(options => options.UseSqlite($"Data Source={_dbFile}"));
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                });
        }
        
        public void Dispose()
        {
            var log = host.Services.GetRequiredService<ILogger>();
            log.Information("Disposing of EngineFixture");

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FakemailDbContext>();
                db.Database.EnsureDeleted();
            }
            File.Delete(_dbFile);
        }
    }

    public class EngineTests : IClassFixture<EngineFixture>
    {
        EngineFixture _fixture;

        public EngineTests(EngineFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateUser()
        {
            var username = Utils.CreateId();
            var password = Utils.CreateId();

            var result = await _fixture.Engine.CreateUserAsync(
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
            var username = Utils.CreateId();
            var password = Utils.CreateId();

            var result = await _fixture.Engine.CreateUserAsync(
                new ApiModels.User
                {
                    Username = username,
                    Password = password
                }
            );

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            result = await _fixture.Engine.CreateUserAsync(
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
           var user = new ApiModels.User
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var result = await _fixture.Engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            user.Password = "WrongPassword!!!";
            var auth = await _fixture.Engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(false);
        }

        [Fact]
        public async Task AuthenticateUser_CorrectPassword()
        {
            var user = new ApiModels.User
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var result = await _fixture.Engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            var auth = await _fixture.Engine.AuthenticateUserAsync(user);

            auth.Success.Should().Be(true);
        }

        [Fact]
        public async Task AuthenticateUser_NoSuchUser()
        {
            var user = new ApiModels.User
            {
                Username = Utils.CreateId(),
                Password = Utils.CreateId()
            };

            var result = await _fixture.Engine.CreateUserAsync(user);

            result.Should().NotBeNull();
            result.Success.Should().BeTrue();

            user.Username = "NoSuchUser!!!";
            var auth = await _fixture.Engine.AuthenticateUserAsync(user);

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
    }
}