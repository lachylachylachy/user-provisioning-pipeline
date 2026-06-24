using System.Text.RegularExpressions;
using EntraFlow.Core.Configuration;
using EntraFlow.Core.Models;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Validation;

/// <summary>
/// Config-driven validator. For each <see cref="FieldRule"/> in the active
/// <see cref="SchemaOptions"/> it flags missing required values, bad formats
/// (e.g. email), and values outside an allowed list. It also rejects duplicate
/// values of the configured unique field (default <c>Email</c>) within a batch.
/// Comparisons are case-insensitive.
/// </summary>
public sealed partial class UserValidator : IUserValidator
{
    private readonly SchemaOptions _schema;

    /// <summary>Creates a validator using the built-in default schema.</summary>
    public UserValidator() : this(SchemaOptions.Default)
    {
    }

    public UserValidator(IOptions<SchemaOptions> options)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value)
    {
    }

    public UserValidator(SchemaOptions schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public IEnumerable<ValidationResult> Validate(IEnumerable<UserRecord> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        // Tracks unique-field values already seen in this batch so the second
        // occurrence is reported. Scoped to the call so the validator is reusable.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasUnique = !string.IsNullOrWhiteSpace(_schema.UniqueField);

        foreach (var user in users)
        {
            var errors = new List<string>();

            foreach (var field in _schema.Fields)
            {
                var value = user[field.Name];
                var isEmpty = string.IsNullOrWhiteSpace(value);

                if (field.Required && isEmpty)
                {
                    errors.Add($"Missing {field.Name}");
                    continue; // No point format/whitelist-checking an empty value.
                }

                if (isEmpty)
                {
                    continue; // Optional and absent.
                }

                if (field.Format == FieldFormat.Email && !EmailRegex().IsMatch(value.Trim()))
                {
                    errors.Add($"Invalid {field.Name} format");
                }

                if (field.AllowedValues is { Count: > 0 } allowed &&
                    !allowed.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Invalid {field.Name} value");
                }
            }

            if (hasUnique)
            {
                var uniqueValue = user[_schema.UniqueField!].Trim();
                if (uniqueValue.Length > 0 && !seen.Add(uniqueValue))
                {
                    errors.Add($"Duplicate {_schema.UniqueField}");
                }
            }

            yield return errors.Count == 0
                ? ValidationResult.Valid(user)
                : ValidationResult.Invalid(user, errors);
        }
    }

    // Pragmatic email check: non-empty local part, single @, dotted domain.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
