namespace EntraFlow.Core.Models;

/// <summary>
/// A single user row read from a provisioning source (currently CSV).
/// <para>
/// Fields are stored generically in <see cref="Fields"/> (a case-insensitive,
/// header-ordered map) so the schema can grow without code changes — an IT team
/// can add columns to their CSV and configure rules for them. The well-known
/// <see cref="Name"/>/<see cref="Email"/>/<see cref="Department"/>/<see cref="Role"/>
/// accessors are conveniences over that map.
/// </para>
/// The raw <see cref="SourceLine"/> and 1-based <see cref="LineNumber"/> are kept
/// so problems can be reported back against the original file.
/// </summary>
public sealed class UserRecord
{
    private readonly IReadOnlyDictionary<string, string> _fields;

    public UserRecord(
        IReadOnlyDictionary<string, string> fields,
        string sourceLine = "",
        int lineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(fields);

        // Copy into a case-insensitive dictionary; insertion order (header order)
        // is preserved so downstream sinks can emit stable columns.
        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in fields)
        {
            copy[key] = value;
        }

        _fields = copy;
        SourceLine = sourceLine;
        LineNumber = lineNumber;
    }

    /// <summary>All fields for this record, keyed by column name (case-insensitive).</summary>
    public IReadOnlyDictionary<string, string> Fields => _fields;

    /// <summary>Returns the value for <paramref name="field"/>, or empty string if absent.</summary>
    public string this[string field] =>
        _fields.TryGetValue(field, out var value) ? value : "";

    public string Name => this["Name"];

    public string Email => this["Email"];

    public string Department => this["Department"];

    public string Role => this["Role"];

    /// <summary>The original, unparsed line from the source file.</summary>
    public string SourceLine { get; }

    /// <summary>1-based line number in the source file (header is line 1).</summary>
    public int LineNumber { get; }

    /// <summary>
    /// Convenience factory for the well-known four-field shape, used by tests and
    /// callers that don't need arbitrary columns.
    /// </summary>
    public static UserRecord FromCoreFields(
        string name,
        string email,
        string department,
        string role,
        string sourceLine = "",
        int lineNumber = 0) =>
        new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Name"] = name,
                ["Email"] = email,
                ["Department"] = department,
                ["Role"] = role,
            },
            sourceLine,
            lineNumber);
}
