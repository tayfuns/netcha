using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using NetCha.Api.Admin;
using NetCha.Api.Audit;
using NetCha.Api.Captcha;
using NetCha.Api.Configuration;
using NetCha.Api.Endpoints;
using NetCha.Api.Replay;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<CaptchaOptions>().BindConfiguration(CaptchaOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<RedisOptions>().BindConfiguration(RedisOptions.SectionName);
builder.Services.AddOptions<PersistenceOptions>().BindConfiguration(PersistenceOptions.SectionName);
builder.Services.AddOptions<AdminOptions>().BindConfiguration(AdminOptions.SectionName);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICaptchaSecret, CaptchaSecret>();
builder.Services.AddSingleton<IAltchaService, AltchaService>();
builder.Services.AddSingleton<MemoryReplayStore>();
builder.Services.AddSingleton<MemoryAuditRepository>();
builder.Services.AddSingleton<MemoryAdminAuthStore>();
builder.Services.AddScoped<AdminSessionFilter>();

var redisConnection = builder.Configuration[$"{RedisOptions.SectionName}:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var redisConfiguration = ConfigurationOptions.Parse(redisConnection);
        redisConfiguration.AbortOnConnectFail = false;
        redisConfiguration.ConnectRetry = 2;
        return ConnectionMultiplexer.Connect(redisConfiguration);
    });
    builder.Services.AddSingleton<IReplayStore, RedisReplayStore>();
}
else
{
    builder.Services.AddSingleton<IReplayStore>(sp => sp.GetRequiredService<MemoryReplayStore>());
}

var postgresConnection = builder.Configuration[$"{PersistenceOptions.SectionName}:ConnectionString"];
if (!string.IsNullOrWhiteSpace(postgresConnection))
{
    builder.Services.AddSingleton(NpgsqlDataSource.Create(postgresConnection));
    builder.Services.AddSingleton<PostgresAuditRepository>();
    builder.Services.AddSingleton<IAuditRepository, ResilientAuditRepository>();
    builder.Services.AddSingleton<IAdminAuthStore, PostgresAdminAuthStore>();
    builder.Services.AddHostedService<PostgresInitializer>();
}
else
{
    builder.Services.AddSingleton<IAuditRepository>(sp => sp.GetRequiredService<MemoryAuditRepository>());
    builder.Services.AddSingleton<IAdminAuthStore>(sp => sp.GetRequiredService<MemoryAdminAuthStore>());
}

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("challenge", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("verify", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem("An unexpected server error occurred.").ExecuteAsync(context);
}));
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapNetChaEndpoints();
app.Run();

public partial class Program;
