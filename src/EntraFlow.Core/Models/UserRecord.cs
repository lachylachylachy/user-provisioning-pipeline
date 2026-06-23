namespace EntraFlow.Core.Models;

/// <summary>
/// A single user row read from a provisioning source (currently CSV).
/// The raw <see cref="SourceLine"/> and 1-based <see cref="LineNumber"/> are kept
/// so problems can be reported back against the original file.
/// </summary>
public sealed record UserRecord
{
    public required string Name { get; init; }

    public required string Email { get; init; }

    public required string Department { get; init; }

    public required string Role { get; init; }

    /// <summary>The original, unparsed line from the source file.</summary>
    public string SourceLine { get; init; } = "";

    /// <summary>1-based line number in the source file (header is line 1).</summary>
    public int LineNumber { get; init; }
}
