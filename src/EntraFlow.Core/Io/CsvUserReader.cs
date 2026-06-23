using EntraFlow.Core.Models;

namespace EntraFlow.Core.Io;

/// <summary>
/// Reads user records from a CSV file with a header row of
/// <c>Name,Email,Department,Role</c>. Supports quoted fields containing commas.
/// Missing trailing columns are treated as empty rather than throwing, so that
/// such rows surface as validation errors instead of crashing the run.
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

        // Skip the header row; line numbers are 1-based for human-friendly reporting.
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseLine(line);

            records.Add(new UserRecord
            {
                Name = Field(fields, 0),
                Email = Field(fields, 1),
                Department = Field(fields, 2),
                Role = Field(fields, 3),
                SourceLine = line,
                LineNumber = i + 1,
            });
        }

        return records;
    }

    private static string Field(IReadOnlyList<string> fields, int index) =>
        index < fields.Count ? fields[index].Trim() : "";

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
