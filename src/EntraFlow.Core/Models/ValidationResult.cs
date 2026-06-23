namespace EntraFlow.Core.Models;

/// <summary>
/// The outcome of validating a single <see cref="UserRecord"/>.
/// A record is valid when <see cref="Errors"/> is empty.
/// </summary>
public sealed record ValidationResult
{
    public required UserRecord User { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public bool IsValid => Errors.Count == 0;

    public static ValidationResult Valid(UserRecord user) =>
        new() { User = user, Errors = [] };

    public static ValidationResult Invalid(UserRecord user, IReadOnlyList<string> errors) =>
        new() { User = user, Errors = errors };
}
