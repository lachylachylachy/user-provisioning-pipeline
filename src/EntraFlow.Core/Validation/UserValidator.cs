using EntraFlow.Core.Models;

namespace EntraFlow.Core.Validation;

/// <summary>
/// Default validator: flags missing required fields and duplicate email addresses
/// within a single batch. Email comparison is case-insensitive.
/// </summary>
public sealed class UserValidator : IUserValidator
{
    public IEnumerable<ValidationResult> Validate(IEnumerable<UserRecord> users)
    {
        ArgumentNullException.ThrowIfNull(users);

        // Tracks emails already seen in this batch so the second occurrence is
        // reported as a duplicate. Scoped to the call so the validator is reusable.
        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in users)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                errors.Add("Missing Name");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                errors.Add("Missing Email");
            }
            else if (!seenEmails.Add(user.Email.Trim()))
            {
                errors.Add("Duplicate Email");
            }

            if (string.IsNullOrWhiteSpace(user.Department))
            {
                errors.Add("Missing Department");
            }

            if (string.IsNullOrWhiteSpace(user.Role))
            {
                errors.Add("Missing Role");
            }

            yield return errors.Count == 0
                ? ValidationResult.Valid(user)
                : ValidationResult.Invalid(user, errors);
        }
    }
}
