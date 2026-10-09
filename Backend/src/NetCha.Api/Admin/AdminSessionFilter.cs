namespace NetCha.Api.Admin;

public sealed class AdminSessionFilter(IAdminAuthStore authStore) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : null;
        if (string.IsNullOrWhiteSpace(token)) return Results.Unauthorized();
        var user = await authStore.ValidateSessionAsync(token, context.HttpContext.RequestAborted);
        if (user is null) return Results.Unauthorized();
        context.HttpContext.Items["AdminUser"] = user;
        context.HttpContext.Items["AdminToken"] = token;
        return await next(context);
    }
}

