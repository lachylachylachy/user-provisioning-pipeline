using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EntraFlow.Web.Services;

/// <summary>One recorded provisioning run, for compliance and history.</summary>
public sealed record AuditEntry
{
    public required string Id { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required string User { get; init; }

    public required string SourceName { get; init; }

    public required int ValidCount { get; init; }

    public required int ErrorCount { get; init; }

    public required bool DryRun { get; init; }

    public required string SinkMode { get; init; }

    public IReadOnlyList<string> Outcomes { get; init; } = [];
}

/// <summary>Append-only audit trail of provisioning runs.</summary>
public interface IAuditLog
{
    Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEntry>> RecentAsync(int max = 100, CancellationToken cancellationToken = default);
}

/// <summary>
/// JSON-lines audit log: one record per line, appended atomically. Pluggable for a
/// database-backed implementation later without changing callers.
/// </summary>
public sealed class JsonlAuditLog : IAuditLog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonlAuditLog(IOptions<StorageOptions> storage)
    {
        _path = storage.Value.AuditFile;
    }

    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var line = JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_path, line, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<AuditEntry>> RecentAsync(int max = 100, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(_path, cancellationToken);
        var entries = new List<AuditEntry>(lines.Length);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<AuditEntry>(line, JsonOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Skip a corrupt line rather than fail the whole history view.
            }
        }

        entries.Reverse(); // newest first
        return entries.Take(max).ToList();
    }
}
