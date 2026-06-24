using EntraFlow.Core.Configuration;
using EntraFlow.Core.Io;
using EntraFlow.Core.Models;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Writes valid and rejected records to timestamped CSV files in the configured
/// output folder. Columns are driven by the active <see cref="SchemaOptions"/>
/// (plus any extra fields present on the records). Serialisation is shared with the
/// web download path via <see cref="CsvFormatter"/>.
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

        var columns = CsvFormatter.ResolveColumns(_schema, results);

        var stamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmmss");
        var validPath = Path.Combine(_options.OutputFolder, $"{sourceName}-valid-{stamp}.csv");
        var errorPath = Path.Combine(_options.OutputFolder, $"{sourceName}-errors-{stamp}.csv");

        await File.WriteAllLinesAsync(validPath, CsvFormatter.ValidLines(columns, results), cancellationToken);
        await File.WriteAllLinesAsync(errorPath, CsvFormatter.ErrorLines(columns, results), cancellationToken);

        return [validPath, errorPath];
    }
}
