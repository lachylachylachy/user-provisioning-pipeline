using EntraFlow.Core.Configuration;
using EntraFlow.Core.Models;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Writes valid and rejected records to timestamped CSV files in the configured
/// output folder. Valid output mirrors the input columns; error output appends an
/// <c>ErrorReasons</c> column.
/// </summary>
public sealed class CsvUserSink : IUserSink
{
    private readonly EntraFlowOptions _options;
    private readonly TimeProvider _timeProvider;

    public CsvUserSink(IOptions<EntraFlowOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<string> Write(string sourceName, IReadOnlyList<ValidationResult> results)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(results);

        Directory.CreateDirectory(_options.OutputFolder);

        var stamp = _timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmmss");
        var validPath = Path.Combine(_options.OutputFolder, $"{sourceName}-valid-{stamp}.csv");
        var errorPath = Path.Combine(_options.OutputFolder, $"{sourceName}-errors-{stamp}.csv");

        var valid = results.Where(r => r.IsValid).Select(r => r.User).ToList();
        var errors = results.Where(r => !r.IsValid).ToList();

        var written = new List<string>();

        var validLines = new List<string> { "Name,Email,Department,Role" };
        validLines.AddRange(valid.Select(u => string.Join(',',
            Csv(u.Name), Csv(u.Email), Csv(u.Department), Csv(u.Role))));
        File.WriteAllLines(validPath, validLines);
        written.Add(validPath);

        var errorLines = new List<string> { "Name,Email,Department,Role,ErrorReasons" };
        errorLines.AddRange(errors.Select(r => string.Join(',',
            Csv(r.User.Name), Csv(r.User.Email), Csv(r.User.Department), Csv(r.User.Role),
            Csv(string.Join("; ", r.Errors)))));
        File.WriteAllLines(errorPath, errorLines);
        written.Add(errorPath);

        return written;
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
