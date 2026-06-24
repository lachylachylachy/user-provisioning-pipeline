namespace EntraFlow.Core.Configuration;

/// <summary>
/// Describes the expected shape of an input record and the rules used to validate
/// it. Bound from the <c>Schema</c> configuration section. When absent, the
/// <see cref="Default"/> shape (Name/Email/Department/Role, Email format-checked)
/// reproduces the original built-in behaviour.
/// <para>
/// This is the "easy to add more fields" seam: an IT team can declare additional
/// fields and rules in configuration (or the web Settings page) without code.
/// </para>
/// </summary>
public sealed class SchemaOptions
{
    public const string SectionName = "Schema";

    /// <summary>The fields that make up a record, in display/output order.</summary>
    public List<FieldRule> Fields { get; set; } = DefaultFields();

    /// <summary>
    /// Field whose value must be unique across a batch (case-insensitive).
    /// Defaults to <c>Email</c>. Set to <c>null</c>/empty to disable the check.
    /// </summary>
    public string? UniqueField { get; set; } = "Email";

    /// <summary>A fresh schema matching the original built-in validation rules.</summary>
    public static SchemaOptions Default => new();

    private static List<FieldRule> DefaultFields() =>
    [
        new() { Name = "Name", Required = true },
        new() { Name = "Email", Required = true, Format = FieldFormat.Email },
        new() { Name = "Department", Required = true },
        new() { Name = "Role", Required = true },
    ];
}

/// <summary>Validation rules for a single field.</summary>
public sealed class FieldRule
{
    /// <summary>Column/attribute name as it appears in the input header.</summary>
    public string Name { get; set; } = "";

    /// <summary>When true, a blank/whitespace value is rejected.</summary>
    public bool Required { get; set; }

    /// <summary>Optional format constraint applied to non-empty values.</summary>
    public FieldFormat Format { get; set; } = FieldFormat.None;

    /// <summary>
    /// Optional whitelist; when set, a non-empty value must match one of these
    /// (case-insensitive). Useful for constraining Department/Role to known values.
    /// </summary>
    public List<string>? AllowedValues { get; set; }
}

/// <summary>Supported value-format constraints.</summary>
public enum FieldFormat
{
    None = 0,
    Email = 1,
}
