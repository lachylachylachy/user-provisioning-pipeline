namespace EntraFlow.Web.Services;

/// <summary>Where the web app keeps its data (settings, audit log, uploads, outputs).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Root data directory; all other paths are derived from it.</summary>
    public string DataDirectory { get; set; } = "data";

    public string SettingsFile => Path.Combine(DataDirectory, "settings.json");

    public string AuditFile => Path.Combine(DataDirectory, "audit.jsonl");

    public string UploadsFolder => Path.Combine(DataDirectory, "uploads");

    public string OutputFolder => Path.Combine(DataDirectory, "output");
}
