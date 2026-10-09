using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using NetCha.Api.Configuration;

namespace NetCha.Api.Admin;

public sealed class AdminApiKeyFilter(IOptions<AdminOptions> options) : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(expected))
            return ValueTask.FromResult<object?>(TypedResults.Problem("Admin API is disabled until Admin:ApiKey is configured.", statusCode: StatusCodes.Status503ServiceUnavailable));

        var supplied = context.HttpContext.Request.Headers["X-Admin-Key"].ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        if (expectedBytes.Length != suppliedBytes.Length || !CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());

        return next(context);
    }
}
