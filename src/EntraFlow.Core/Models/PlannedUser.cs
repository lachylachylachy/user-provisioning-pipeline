using EntraFlow.Core.Configuration;

namespace EntraFlow.Core.Models;

/// <summary>
/// The Entra user payload computed from a <see cref="UserRecord"/> and the
/// configured <see cref="GraphFieldMapping"/>. Kept provider-agnostic so the
/// mapping can be unit-tested without the Graph SDK, and so dry-run can report
/// exactly what would be created.
/// </summary>
public sealed record PlannedUser
{
    public required string DisplayName { get; init; }

    public required string UserPrincipalName { get; init; }

    public required string MailNickname { get; init; }

    public string? Department { get; init; }

    public string? JobTitle { get; init; }

    public string? UsageLocation { get; init; }

    /// <summary>Builds a <see cref="PlannedUser"/> from a record using the mapping.</summary>
    public static PlannedUser FromRecord(UserRecord record, GraphFieldMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(mapping);

        var upn = record[mapping.UserPrincipalName].Trim();
        var nicknameSource = record[mapping.MailNickname].Trim();
        var nickname = nicknameSource.Contains('@')
            ? nicknameSource[..nicknameSource.IndexOf('@')]
            : nicknameSource;

        return new PlannedUser
        {
            DisplayName = record[mapping.DisplayName].Trim(),
            UserPrincipalName = upn,
            MailNickname = nickname,
            Department = Optional(record, mapping.Department),
            JobTitle = Optional(record, mapping.JobTitle),
            UsageLocation = string.IsNullOrWhiteSpace(mapping.UsageLocation)
                ? null
                : mapping.UsageLocation,
        };
    }

    private static string? Optional(UserRecord record, string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return null;
        }

        var value = record[field].Trim();
        return value.Length == 0 ? null : value;
    }
}
