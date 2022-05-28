using System;
using System.IO;
using System.Threading.Tasks;

using Fakemail.Core;
using Fakemail.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace Fakemail.Core.Tests
{
    public class DummyPwnedPasswordApi : IPwnedPasswordApi
    {
        public async Task<string> RangeAsync(string prefix)
        {
            // Prevent warning about async method lacking await operators
            await Task.Yield();

            // The hash for 'asdfasdfasdf' is 79437F5EDDA13F9C0669B978DD7A9066DD2059F1
            // Return this one (and a couple of others), to make that password trigger the 
            // PwnedPassword check
            return "F56E4F3B8721E983BA9C23C260EBF4AA526:1\r\n"
                 + "F5EDDA13F9C0669B978DD7A9066DD2059F1:7322\r\n"
                 + "F5FDC0B32D57F567BE7E6F5A932B995F642:2\r\n";
        }
    }

    public class EngineFixture : IDisposable
    {
        private readonly string _dbFile = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}fakemail-enginetests-{DateTime.Now.ToString("HHmmss")}-{Utils.CreateId()}.sqlite";
        private IHost host;

        public IEngine Engine { get; set; }

        public static readonly string ExamplePwnedPassword = "asdfasdfasdf";

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

            // use a random jwt signing key, so tokens generated here will not be valid in production or anywhere else
            var jwtSigningKey = Utils.CreateId(16);

            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContextFactory<FakemailDbContext>(options => options.UseSqlite($"Data Source={_dbFile}"));
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IJwtAuthentication>(new JwtAuthentication(jwtSigningKey, "", 10));

                    // Swap the commented line to use the real PwnedPassword Api in tests
                    services.AddSingleton<IPwnedPasswordApi, DummyPwnedPasswordApi>();
                    //services.AddHttpClient<IPwnedPasswordApi, PwnedPasswordApi>();
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
}