namespace EntraFlow.Core.Configuration;

/// <summary>
/// Folder configuration for the provisioning pipeline. Bound from the
/// <c>EntraFlow</c> section of <c>appsettings.json</c> and overridable via
/// command line or environment variables.
/// </summary>
public sealed class EntraFlowOptions
{
    public const string SectionName = "EntraFlow";

    /// <summary>Folder watched for incoming <c>*.csv</c> files to process.</summary>
    public string InputFolder { get; set; } = "data/input";

    /// <summary>Folder where valid/error result files are written.</summary>
    public string OutputFolder { get; set; } = "data/output";

    /// <summary>
    /// When true, processed input files are moved into an <c>archive</c> subfolder
    /// of the input folder so they are not reprocessed on the next run.
    /// </summary>
    public bool ArchiveProcessedFiles { get; set; } = true;
}
