using EntraFlow.Core.Models;

namespace EntraFlow.Core.Validation;

/// <summary>
/// Validates a sequence of user records. Implementations may carry state across a
/// single batch (for example, to detect duplicate emails), so a new instance should
/// be created per run unless the implementation documents otherwise.
/// </summary>
public interface IUserValidator
{
    /// <summary>
    /// Validates the supplied records in order, yielding one result per input record.
    /// </summary>
    IEnumerable<ValidationResult> Validate(IEnumerable<UserRecord> users);
}
