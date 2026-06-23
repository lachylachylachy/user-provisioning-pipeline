using EntraFlow.Core.Models;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Destination for validated provisioning results. The CSV implementation writes
/// files today; a future <c>EntraFlow.Data</c> project can add a SQL-backed sink
/// (EF Core/Dapper) without changing the pipeline.
/// </summary>
public interface IUserSink
{
    /// <summary>
    /// Persists the results of one processed source file.
    /// </summary>
    /// <param name="sourceName">Base name of the source file, used to label output.</param>
    /// <param name="results">Validation results for every record in the source.</param>
    /// <returns>Paths (or identifiers) of the outputs that were written.</returns>
    IReadOnlyList<string> Write(string sourceName, IReadOnlyList<ValidationResult> results);
}
