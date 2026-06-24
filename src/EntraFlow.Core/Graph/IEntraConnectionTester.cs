using EntraFlow.Core.Configuration;

namespace EntraFlow.Core.Graph;

/// <summary>
/// Verifies that a set of Entra connection options can authenticate and read from
/// the tenant, so the UI can show a clear pass/fail before saving or running.
/// </summary>
public interface IEntraConnectionTester
{
    Task<ConnectionTestResult> TestConnectionAsync(
        EntraConnectionOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a connection test.</summary>
public sealed record ConnectionTestResult(bool Success, string Message)
{
    public static ConnectionTestResult Ok(string message) => new(true, message);

    public static ConnectionTestResult Fail(string message) => new(false, message);
}
