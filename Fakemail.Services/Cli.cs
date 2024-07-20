using Fakemail.Core;
using Fakemail.Data.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;

namespace Fakemail.Services
{
    public class Cli
    {
        public static async Task Main()
        {
            // Usage:
            //  Fakemail.DeliveryAgent -p seconds -f <fail directory> -n <new mail directory>
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            IConfigurationRoot configRoot = null;

            var host = Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    // this reads the 'Environment' environment variable, which should set to Development in the debug profile
                    configHost.AddEnvironmentVariables();
                })
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.Sources.Clear();
                    config.AddJsonFile("appsettings.json", true);
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true);
                    configRoot = config.Build();
                })
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var connectionString = hostContext.Configuration.GetConnectionString("fakemail");
                    services.AddDbContextFactory<FakemailDbContext>(options => options.UseSqlite(connectionString));
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton(TimeProvider.System);
                    services.AddHttpClient<IPwnedPasswordApi, PwnedPasswordApi>();

                    // TODO: make a JwtOptions class, similar to DeliveryAgentOptions
                    var jwtSecret = hostContext.Configuration["Jwt:Secret"];
                    var jwtValidIssuer = hostContext.Configuration["Jwt:ValidIssuer"];
                    var jwtExpiryMinutes = Convert.ToInt32(hostContext.Configuration["Jwt:ExpiryMinutes"]);
                    services.AddSingleton<IJwtAuthentication>(new JwtAuthentication(jwtSecret, jwtValidIssuer, jwtExpiryMinutes));
                    services.Configure<DeliveryAgentOptions>(hostContext.Configuration.GetSection("DeliveryAgent"));
                    services.AddHostedService<DeliveryAgent>();
                    services.Configure<CleanupServiceOptions>(hostContext.Configuration.GetSection("CleanupService"));
                    services.AddHostedService<CleanupService>();
                })
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FakemailDbContext>();
                db.Database.EnsureCreated();
            }

            var cancellationToken = new CancellationTokenSource().Token;

            await host.RunAsync();
        }
    }
}
