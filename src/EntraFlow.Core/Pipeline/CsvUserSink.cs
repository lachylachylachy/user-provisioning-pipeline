using EntraFlow.Core.Configuration;
using EntraFlow.Core.Models;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Writes valid and rejected records to timestamped CSV files in the configured
/// output folder. Columns are driven by the active <see cref="SchemaOptions"/>
/// (plus any extra fields present on the records), so output keeps pace with the
/// input schema. The error file appends an <c>ErrorReasons</c> column.
/// </summary>
public sealed class CsvUserSink : IUserSink
{
    private readonly EntraFlowOptions _options;
    private readonly SchemaOptions _schema;
    private readonly TimeProvider _timeProvider;

    public CsvUserSink(
        IOptions<EntraFlowOptions> options,
        IOptions<SchemaOptions> schema,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _schema = schema.Value;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<string>> WriteAsync(
        string sourceName,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(results);

        Directory.CreateDirectory(_options.OutputFolder);

        var columns = ResolveColumns(results);

        var stamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmmss");
        var validPath = Path.Combine(_options.OutputFolder, $"{sourceName}-valid-{stamp}.csv");
        var errorPath = Path.Combine(_options.OutputFolder, $"{sourceName}-errors-{stamp}.csv");

        var written = new List<string>();

        var validLines = new List<string> { string.Join(',', columns.Select(Csv)) };
        validLines.AddRange(results
            .Where(r => r.IsValid)
            .Select(r => string.Join(',', columns.Select(c => Csv(r.User[c])))));
        await File.WriteAllLinesAsync(validPath, validLines, cancellationToken);
        written.Add(validPath);

        var errorHeader = columns.Append("ErrorReasons");
        var errorLines = new List<string> { string.Join(',', errorHeader.Select(Csv)) };
        errorLines.AddRange(results
            .Where(r => !r.IsValid)
            .Select(r => string.Join(',',
                columns.Select(c => Csv(r.User[c])).Append(Csv(string.Join("; ", r.Errors))))));
        await File.WriteAllLinesAsync(errorPath, errorLines, cancellationToken);
        written.Add(errorPath);

        return written;
    }

    /// <summary>
    /// Columns are the schema fields first (stable order, present even when there
    /// are no rows), then any additional field names found on the records.
    /// </summary>
    private List<string> ResolveColumns(IReadOnlyList<ValidationResult> results)
    {
        var columns = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in _schema.Fields)
        {
            if (seen.Add(field.Name))
            {
                columns.Add(field.Name);
            }
        }

        foreach (var result in results)
        {
            foreach (var key in result.User.Fields.Keys)
            {
                if (seen.Add(key))
                {
                    columns.Add(key);
                }
            }
        }

        return columns;
    }

    /// <summary>Quotes a field when it contains a comma, quote, or newline.</summary>
    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
