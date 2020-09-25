using System;

using Fakemail.Core;
using Fakemail.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Exceptions;

namespace Fakemail.Grpc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
              .Enrich.WithExceptionDetails()
              .WriteTo.Console()
              .CreateLogger();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();

            var redisConfiguration = new RedisConfiguration
            {
                Host = "localhost",
                Port = 6379,
                Password = "Password1!",
                DatabaseNumber = 1
            };

            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(Log.Logger);
                    services.AddSingleton<IEngine, Engine>();
                    services.AddSingleton<IRedisConfiguration>(x => redisConfiguration);
                    services.AddSingleton<IDataStorage, RedisDataStorage>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}
