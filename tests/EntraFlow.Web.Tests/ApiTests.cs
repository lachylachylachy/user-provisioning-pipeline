using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EntraFlow.Web.Tests;

/// <summary>
/// End-to-end API tests over the real web host: auth gating and a full provisioning
/// run through the CSV sink. Each factory uses an isolated temp data directory.
/// </summary>
public sealed class ApiTests : IClassFixture<EntraFlowFactory>
{
    private const string ApiKey = "test-api-key";

    private readonly EntraFlowFactory _factory;

    public ApiTests(EntraFlowFactory factory) => _factory = factory;

    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Home_WhenUnauthenticated_RedirectsToLogin()
    {
        var response = await Client().GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Api_WithoutKey_IsUnauthorized()
    {
        var response = await Client().GetAsync("/api/runs");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Api_WithKey_ReturnsRuns()
    {
        var client = Client();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

        var response = await client.GetAsync("/api/runs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostRun_ValidatesAndSplitsCsv()
    {
        var client = Client();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

        var csv = string.Join('\n',
            "Name,Email,Department,Role",
            "Jane Doe,jane@company.com,IT,Admin",
            ",missing@company.com,HR,User",          // missing name
            "Bad Email,not-an-email,IT,Admin",        // bad email
            "Jane Doe,jane@company.com,IT,Admin");    // duplicate email

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", "users.csv");

        var response = await client.PostAsync("/api/runs", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RunSummary>();

        Assert.NotNull(result);
        Assert.Equal(1, result!.ValidCount);
        Assert.Equal(3, result.ErrorCount);
        Assert.True(result.DryRun);
        Assert.Equal("Csv", result.SinkMode);
    }

    [Fact]
    public async Task PostRun_RejectsNonCsv()
    {
        var client = Client();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("x"u8.ToArray()), "file", "users.txt");

        var response = await client.PostAsync("/api/runs", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record RunSummary(
        string Id, string SourceName, int ValidCount, int ErrorCount, bool DryRun, string SinkMode);
}

/// <summary>Web host configured with an API key and an isolated temp data directory.</summary>
public sealed class EntraFlowFactory : WebApplicationFactory<Program>
{
    private readonly string _dataDir =
        Path.Combine(Path.GetTempPath(), $"entraflow-tests-{Guid.NewGuid():N}");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:ApiKey"] = "test-api-key",
                ["Admin:Password"] = "test-password",
                ["Storage:DataDirectory"] = _dataDir,
            });
        });

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_dataDir))
        {
            try { Directory.Delete(_dataDir, recursive: true); }
            catch (IOException) { /* best-effort cleanup */ }
        }
    }
}
