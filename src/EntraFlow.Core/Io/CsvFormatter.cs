using EntraFlow.Core.Configuration;
using EntraFlow.Core.Models;

namespace EntraFlow.Core.Io;

/// <summary>
/// Shared CSV serialisation for validation results. Used by the disk sink and by
/// the web app's download endpoints so both produce identical output.
/// </summary>
public static class CsvFormatter
{
    /// <summary>
    /// Output columns: schema fields first (stable order, present even with no rows),
    /// then any additional field names found on the records.
    /// </summary>
    public static IReadOnlyList<string> ResolveColumns(
        SchemaOptions schema,
        IReadOnlyList<ValidationResult> results)
    {
        var columns = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in schema.Fields)
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

    public static List<string> ValidLines(
        IReadOnlyList<string> columns,
        IReadOnlyList<ValidationResult> results)
    {
        var lines = new List<string> { string.Join(',', columns.Select(Field)) };
        lines.AddRange(results
            .Where(r => r.IsValid)
            .Select(r => string.Join(',', columns.Select(c => Field(r.User[c])))));
        return lines;
    }

    public static List<string> ErrorLines(
        IReadOnlyList<string> columns,
        IReadOnlyList<ValidationResult> results)
    {
        var header = columns.Append("ErrorReasons");
        var lines = new List<string> { string.Join(',', header.Select(Field)) };
        lines.AddRange(results
            .Where(r => !r.IsValid)
            .Select(r => string.Join(',',
                columns.Select(c => Field(r.User[c])).Append(Field(string.Join("; ", r.Errors))))));
        return lines;
    }

    /// <summary>Quotes a field when it contains a comma, quote, or newline.</summary>
    public static string Field(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
