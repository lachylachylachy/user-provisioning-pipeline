using EntraFlow.Core.Models;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Destination for validated provisioning results. The CSV implementation writes
/// files; the Graph implementation provisions users into Entra. A composite can
/// drive several sinks. The async signature accommodates network-bound sinks.
/// </summary>
public interface IUserSink
{
    /// <summary>
    /// Persists the results of one processed source file.
    /// </summary>
    /// <param name="sourceName">Base name of the source file, used to label output.</param>
    /// <param name="results">Validation results for every record in the source.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Human-readable outputs/outcomes (file paths, created ids, or dry-run notes).</returns>
    Task<IReadOnlyList<string>> WriteAsync(
        string sourceName,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default);
}
