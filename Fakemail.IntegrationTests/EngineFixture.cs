using System;
using System.IO;

using Fakemail.Core;
using Fakemail.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

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
}