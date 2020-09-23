using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Fakemail.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Exceptions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Fakemail.Core;

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
