using Fakemail.RateLimiter;

using Polly;
using Polly.Extensions.Http;

namespace Fakemail.Web;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.Configure<FakemailApiClientOptions>(builder.Configuration.GetSection("Api"));
        builder.Services.Configure<CountingRateLimiterOptions>(builder.Configuration.GetSection("IpRateLimit"));

        builder.Services.AddHttpClient<IFakemailApiClient, FakemailApiClient>()
            .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

        builder.Services.AddClientIpRateLimiting();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseIpRateLimiting();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapRazorPages();

        app.Run();
    }
}