using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Fakemail.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

using Serilog;

namespace Fakemail.Core.Tests
{
    public class DummyPwnedPasswordApi : IPwnedPasswordApi
    {
        public Task<string> RangeAsync(string prefix)
        {
            // The hash for 'asdfasdfasdf' is 79437F5EDDA13F9C0669B978DD7A9066DD2059F1
            // Return this one (and a couple of others), to make that password trigger the
            // PwnedPassword check
            return Task.FromResult("F56E4F3B8721E983BA9C23C260EBF4AA526:1\r\n"
                 + "F5EDDA13F9C0669B978DD7A9066DD2059F1:7322\r\n"
                 + "F5FDC0B32D57F567BE7E6F5A932B995F642:2\r\n");
        }
    }

    public class ShortJwtKeyEngineFixture : EngineFixture
    {
        public override string JwtSigningKey => RandomNumberGenerator.GetHexString(63);
    }

    public class EngineFixture : IDisposable
    {
        private readonly string _dbFile = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}fakemail-enginetests-{DateTime.Now:HHmmss}-{Utils.CreateId()}.sqlite";

        private IHost? host;

        private readonly Lazy<IEngine> _engine;

        public IEngine Engine => _engine.Value;

        public FakeTimeProvider TimeProvider => host?.Services.GetRequiredService<TimeProvider>() as FakeTimeProvider ?? throw new Exception();

        public static readonly string ExamplePwnedPassword = "asdfasdfasdf";

        // use a random jwt signing key, so tokens generated here will not be valid in production or anywhere else
        public virtual string JwtSigningKey => RandomNumberGenerator.GetHexString(64);

        public EngineFixture()
        {
            // The engine is created lazily so that exceptions (e.g. due to invalid JWT key length) are
            // not raised in internal XUnit classes.
            _engine = new Lazy<IEngine>(CreateEngine);
        }

        private IEngine CreateEngine()
        {
            host = CreateHostBuilder()
             .Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FakemailDbContext>();
                db.Database.EnsureCreated();
            }

            return host.Services.GetRequiredService<IEngine>();
        }

        private IHostBuilder CreateHostBuilder()
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
                    services.AddSingleton<TimeProvider, FakeTimeProvider>();
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IJwtAuthentication>(new JwtAuthentication(JwtSigningKey, "", 10));

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (host != null)
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
    }
}