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

        var auth = app.MapGroup("/api/v1/admin/auth").WithTags("Admin authentication");
        auth.MapPost("/login", LoginAsync).RequireRateLimiting("admin-login");

        var admin = app.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .AddEndpointFilter<AdminSessionFilter>();
        admin.MapGet("/stats", (IAuditRepository repository, CancellationToken ct) => repository.GetStatsAsync(ct));
        admin.MapGet("/audit", (int? limit, IAuditRepository repository, CancellationToken ct) => repository.ListAsync(Math.Clamp(limit ?? 100, 1, 500), ct));
        admin.MapGet("/settings", (IOptions<CaptchaOptions> settings) => new EffectiveCaptchaSettings(settings.Value.MaxNumber, settings.Value.ExpiresInSeconds));
        admin.MapGet("/auth/me", (HttpContext context) => Results.Ok((AdminUser)context.Items["AdminUser"]!));
        admin.MapPost("/auth/logout", LogoutAsync);
        admin.MapPost("/auth/change-password", ChangePasswordAsync);

        return app;
    }

    private static async Task<IResult> LoginAsync(AdminLoginRequest request, IAdminAuthStore authStore, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)) return Results.BadRequest(new { error = "username_and_password_required" });
        var session = await authStore.LoginAsync(request.Username, request.Password, cancellationToken);
        return session is null
            ? Results.Unauthorized()
            : Results.Ok(new AdminLoginResponse(session.Token, session.ExpiresAt, session.User));
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, IAdminAuthStore authStore, CancellationToken cancellationToken)
    {
        await authStore.LogoutAsync((string)context.Items["AdminToken"]!, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(ChangePasswordRequest request, HttpContext context, IAdminAuthStore authStore, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12) return Results.BadRequest(new { error = "password_too_short" });
        var user = (AdminUser)context.Items["AdminUser"]!;
        var changed = await authStore.ChangePasswordAsync(user.Id, request.CurrentPassword ?? "", request.NewPassword, cancellationToken);
        return changed ? Results.NoContent() : Results.BadRequest(new { error = "current_password_invalid" });
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
