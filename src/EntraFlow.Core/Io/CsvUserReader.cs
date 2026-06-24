using EntraFlow.Core.Models;

namespace EntraFlow.Core.Io;

/// <summary>
/// Reads user records from a CSV file. The first non-empty line is treated as the
/// header; every column it names becomes a field on each <see cref="UserRecord"/>,
/// so arbitrary extra columns flow through without code changes. Supports quoted
/// fields containing commas. Missing trailing columns are treated as empty rather
/// than throwing, so such rows surface as validation errors instead of crashing.
/// </summary>
public sealed class CsvUserReader : IUserReader
{
    public IReadOnlyList<UserRecord> Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Input file not found: {path}", path);
        }

        var lines = File.ReadAllLines(path);
        var records = new List<UserRecord>(lines.Length);

        // Locate the header (first non-blank line); its position fixes the 1-based
        // line numbers used for human-friendly error reporting.
        var headerIndex = Array.FindIndex(lines, l => !string.IsNullOrWhiteSpace(l));
        if (headerIndex < 0)
        {
            return records;
        }

        var headers = ParseLine(lines[headerIndex]).Select(h => h.Trim()).ToList();

        for (var i = headerIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var values = ParseLine(line);

            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Count; col++)
            {
                var key = headers[col];
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                fields[key] = col < values.Count ? values[col].Trim() : "";
            }

            records.Add(new UserRecord(fields, sourceLine: line, lineNumber: i + 1));
        }

        return records;
    }

    /// <summary>
    /// Minimal RFC 4180-style splitter: handles quoted fields and escaped quotes
    /// (<c>""</c>) so values such as <c>"Doe, Jane"</c> parse as one field.
    /// </summary>
    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
