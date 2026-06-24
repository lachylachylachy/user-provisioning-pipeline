using EntraFlow.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace EntraFlow.Core.Graph;

/// <summary>
/// Default tester: authenticates with the supplied options and performs a cheap
/// read (<c>/organization</c>) to confirm credentials and permissions.
/// </summary>
public sealed class EntraConnectionTester : IEntraConnectionTester
{
    private readonly ILogger<EntraConnectionTester> _logger;

    public EntraConnectionTester(ILogger<EntraConnectionTester> logger)
    {
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        EntraConnectionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.HasCredentials)
        {
            return ConnectionTestResult.Fail(
                "Tenant ID, Client ID and Client Secret are all required.");
        }

        try
        {
            var client = EntraGraphClientFactory.Create(options);
            var org = await client.Organization.GetAsync(cancellationToken: cancellationToken);

            var name = org?.Value?.FirstOrDefault()?.DisplayName;
            return ConnectionTestResult.Ok(
                string.IsNullOrWhiteSpace(name)
                    ? "Connected to Entra successfully."
                    : $"Connected to '{name}'.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Entra connection test failed.");
            return ConnectionTestResult.Fail($"Connection failed: {ex.Message}");
        }
    }
}
