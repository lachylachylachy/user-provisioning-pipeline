using System.Text;
using EntraFlow.Core.Configuration;
using EntraFlow.Core.Io;
using EntraFlow.Core.Models;
using EntraFlow.Core.Options;
using EntraFlow.Core.Pipeline;
using EntraFlow.Core.Validation;
using Microsoft.Extensions.Options;

namespace EntraFlow.Web.Services;

/// <summary>Outcome of a single web-driven provisioning run, shaped for the UI/API.</summary>
public sealed record RunResult
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string SourceName { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<ValidationResult> Results { get; init; }
    public required int ValidCount { get; init; }
    public required int ErrorCount { get; init; }
    public required bool DryRun { get; init; }
    public required string SinkMode { get; init; }
    public required IReadOnlyList<string> Outcomes { get; init; }
    public required string ValidCsv { get; init; }
    public required string ErrorCsv { get; init; }

    public int TotalCount => ValidCount + ErrorCount;
}

/// <summary>
/// Runs an uploaded CSV through the pipeline using the current <see cref="AppSettings"/>,
/// building the validator and sink(s) from a live settings snapshot so changes made on
/// the Settings page take effect immediately. Records every run in the audit log.
/// </summary>
public sealed class ProvisioningRunner
{
    private readonly ISettingsStore _settings;
    private readonly IAuditLog _audit;
    private readonly StorageOptions _storage;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TimeProvider _time;

    public ProvisioningRunner(
        ISettingsStore settings,
        IAuditLog audit,
        IOptions<StorageOptions> storage,
        ILoggerFactory loggerFactory,
        TimeProvider time)
    {
        _settings = settings;
        _audit = audit;
        _storage = storage.Value;
        _loggerFactory = loggerFactory;
        _time = time;
    }

    public async Task<RunResult> RunAsync(
        string fileName,
        Stream content,
        string user,
        CancellationToken cancellationToken = default)
    {
        var settings = _settings.Current;
        var now = _time.GetUtcNow();
        var sourceName = SafeName(Path.GetFileNameWithoutExtension(fileName));

        Directory.CreateDirectory(_storage.UploadsFolder);
        var uploadPath = Path.Combine(
            _storage.UploadsFolder, $"{sourceName}-{now:yyyyMMdd-HHmmss}.csv");
        await using (var fs = File.Create(uploadPath))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        var users = new CsvUserReader().Read(uploadPath);
        var results = new UserValidator(settings.Schema).Validate(users).ToList();

        var sink = BuildSink(settings);
        var outcomes = await sink.WriteAsync(sourceName, results, cancellationToken);

        var columns = CsvFormatter.ResolveColumns(settings.Schema, results);
        var validCount = results.Count(r => r.IsValid);
        var errorCount = results.Count - validCount;
        var live = settings.Entra.Enabled && !settings.Entra.DryRun;
        var id = Guid.NewGuid().ToString("N")[..12];

        await _audit.AppendAsync(new AuditEntry
        {
            Id = id,
            Timestamp = now,
            User = user,
            SourceName = sourceName,
            ValidCount = validCount,
            ErrorCount = errorCount,
            DryRun = !live,
            SinkMode = settings.Entra.Sink.ToString(),
            Outcomes = outcomes,
        }, cancellationToken);

        return new RunResult
        {
            Id = id,
            Timestamp = now,
            SourceName = sourceName,
            Columns = columns,
            Results = results,
            ValidCount = validCount,
            ErrorCount = errorCount,
            DryRun = !live,
            SinkMode = settings.Entra.Sink.ToString(),
            Outcomes = outcomes,
            ValidCsv = string.Join(Environment.NewLine, CsvFormatter.ValidLines(columns, results)),
            ErrorCsv = string.Join(Environment.NewLine, CsvFormatter.ErrorLines(columns, results)),
        };
    }

    private IUserSink BuildSink(AppSettings settings)
    {
        var entraFlow = Microsoft.Extensions.Options.Options.Create(new EntraFlowOptions
        {
            OutputFolder = _storage.OutputFolder,
            ArchiveProcessedFiles = false,
        });
        var schema = Microsoft.Extensions.Options.Options.Create(settings.Schema);

        var csv = new CsvUserSink(entraFlow, schema, _time);
        var graph = new GraphUserSink(
            new StaticOptionsMonitor<EntraConnectionOptions>(settings.Entra),
            _loggerFactory.CreateLogger<GraphUserSink>());

        return settings.Entra.Sink switch
        {
            SinkMode.Graph => graph,
            SinkMode.Both => new CompositeUserSink(csv, graph),
            _ => csv,
        };
    }

    private static string SafeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "upload";
        }

        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_');
        }

        return builder.ToString();
    }
}
