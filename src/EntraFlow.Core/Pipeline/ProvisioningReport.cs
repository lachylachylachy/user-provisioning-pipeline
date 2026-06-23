using EntraFlow.Core.Models;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Summary of a single processed input file: which file was handled, how many
/// records were valid versus rejected, and where the results were written.
/// </summary>
public sealed record ProvisioningReport
{
    public required string SourceFile { get; init; }

    public required int ValidCount { get; init; }

    public required int ErrorCount { get; init; }

    public required IReadOnlyList<string> OutputFiles { get; init; }

    public int TotalCount => ValidCount + ErrorCount;
}
