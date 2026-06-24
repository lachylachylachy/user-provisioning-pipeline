namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Orchestrates the end-to-end provisioning run: discover input files, validate
/// their records, and persist results via the configured sink.
/// </summary>
public interface IProvisioningPipeline
{
    /// <summary>
    /// Processes every <c>*.csv</c> file in the configured input folder.
    /// </summary>
    /// <returns>One report per processed file.</returns>
    Task<IReadOnlyList<ProvisioningReport>> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a single input file.
    /// </summary>
    Task<ProvisioningReport> ProcessFileAsync(string path, CancellationToken cancellationToken = default);
}
