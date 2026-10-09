using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NetCha.Api.Captcha;

namespace NetCha.Api.Tests;

public sealed class ApiIntegrationTests(NetChaFactory factory) : IClassFixture<NetChaFactory>
{
    [Fact]
    public async Task Health_endpoint_is_healthy()
    {
        using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_is_exposed()
    {
        using var client = factory.CreateHttpsClient();
        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Challenge_can_be_verified_only_once()
    {
        using var client = factory.CreateHttpsClient();
        var challenge = await client.GetFromJsonAsync<AltchaChallenge>("/api/v1/captcha/challenge");
        Assert.NotNull(challenge);
        var payload = AltchaServiceTests.EncodeSolvedPayload(challenge, 5_000);

        var first = await client.PostAsJsonAsync("/api/v1/captcha/verify", new { payload });
        var second = await client.PostAsJsonAsync("/api/v1/captcha/verify", new { payload });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        var result = await second.Content.ReadFromJsonAsync<VerifyCaptchaResponse>();
        Assert.Equal("challenge_replayed", result?.Error);
    }

    [Fact]
    public async Task Admin_endpoint_requires_api_key()
    {
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/stats")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Admin-Key", "integration-test-admin-key-with-32-bytes");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/stats")).StatusCode);
    }
}

public sealed class NetChaFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Captcha:HmacKey"] = "integration-test-captcha-key-with-32-bytes",
            ["Captcha:MaxNumber"] = "5000",
            ["Captcha:ExpiresInSeconds"] = "60",
            ["Admin:ApiKey"] = "integration-test-admin-key-with-32-bytes",
            ["Redis:ConnectionString"] = null,
            ["Persistence:ConnectionString"] = null
        }));
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });
}
