using System.Text;
using System.Text.Json.Serialization;

using Fakemail.Core;
using Fakemail.Data.EntityFramework;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Polly;
using Polly.Extensions.Http;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("fakemail");

var jwtSecret = builder.Configuration["Jwt:Secret"];
var jwtValidIssuer = builder.Configuration["Jwt:ValidIssuer"];
var jwtExpiryMinutes = Convert.ToInt32(builder.Configuration["Jwt:ExpiryMinutes"]);

builder.Host.UseSerilog((ctx, loggerConfiguration) => loggerConfiguration.WriteTo.Console());

builder.Services.AddDbContextFactory<FakemailDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(Log.Logger);
builder.Services.AddSingleton<IEngine, Engine>();
builder.Services.AddHttpClient<IPwnedPasswordApi, PwnedPasswordApi>()
    .AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// In the development environment, allow CORS from the web server to the API.
// In production, they will use the same origin (using a reverse proxy)
var corsPolicyName = "_fakemail_development_allowed_origins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins("https://localhost:7150");
    });
});
      
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition
   = JsonIgnoreCondition.WhenWritingDefault 
   | JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidIssuer = jwtValidIssuer,
        ValidateAudience = false,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret))
    };
});

builder.Services.AddSingleton<IJwtAuthentication>(new JwtAuthentication(jwtSecret, jwtValidIssuer, jwtExpiryMinutes));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });

    // This allows the user to enter the bearer token into the swagger explorer, so
    // that authorized API calls can be made
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) 
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(corsPolicyName);
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FakemailDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
