using Microsoft.Extensions.Options;
using NetCha.Api.Admin;
using NetCha.Api.Audit;
using NetCha.Api.Captcha;
using NetCha.Api.Configuration;
using NetCha.Api.Replay;

namespace NetCha.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapNetChaEndpoints(this IEndpointRouteBuilder app)
    {
        var captcha = app.MapGroup("/api/v1/captcha").WithTags("Captcha");
        captcha.MapGet("/challenge", (IAltchaService service) => Results.Ok(service.CreateChallenge()))
            .RequireRateLimiting("challenge")
            .WithName("CreateCaptchaChallenge");

        captcha.MapPost("/verify", VerifyAsync)
            .RequireRateLimiting("verify")
            .WithName("VerifyCaptcha");

        var admin = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .AddEndpointFilter<AdminApiKeyFilter>();
        admin.MapGet("/stats", (IAuditRepository repository, CancellationToken ct) => repository.GetStatsAsync(ct));
        admin.MapGet("/audit", (int? limit, IAuditRepository repository, CancellationToken ct) => repository.ListAsync(Math.Clamp(limit ?? 100, 1, 500), ct));
        admin.MapGet("/settings", (IOptions<CaptchaOptions> settings) => new EffectiveCaptchaSettings(settings.Value.MaxNumber, settings.Value.ExpiresInSeconds));

        return app;
    }

    private static async Task<IResult> VerifyAsync(
        VerifyCaptchaRequest request,
        IAltchaService service,
        IReplayStore replayStore,
        IAuditRepository audit,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var verification = service.Verify(request.Payload);
        var verified = verification.IsValid;
        var error = verification.Error;
        if (verified)
        {
            verified = await replayStore.TryConsumeAsync(verification.ReplayKey!, verification.ExpiresAt!.Value, cancellationToken);
            if (!verified) error = "challenge_replayed";
        }

        await audit.RecordAsync(
            verified,
            error,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return verified
            ? Results.Ok(new VerifyCaptchaResponse(true))
            : Results.BadRequest(new VerifyCaptchaResponse(false, error));
    }
}
