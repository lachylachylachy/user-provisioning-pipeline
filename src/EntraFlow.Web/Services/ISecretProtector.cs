namespace EntraFlow.Web.Services;

/// <summary>
/// Protects sensitive values (the Entra client secret) at rest. Phase 3 ships a
/// pass-through; Phase 4 swaps in an ASP.NET Core Data Protection implementation
/// without changing callers.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}
