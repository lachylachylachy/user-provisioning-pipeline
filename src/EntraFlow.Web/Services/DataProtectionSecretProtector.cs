using Microsoft.AspNetCore.DataProtection;

namespace EntraFlow.Web.Services;

/// <summary>
/// Protects secrets at rest using ASP.NET Core Data Protection, so the Entra client
/// secret is never written to disk in plaintext. Keys are managed by the framework
/// (persisted to the data-protection key ring).
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "EntraFlow.Settings.ClientSecret";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) =>
        string.IsNullOrEmpty(plaintext) ? "" : _protector.Protect(plaintext);

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
        {
            return "";
        }

        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch
        {
            // Key ring rotated/unavailable — treat as missing rather than crash.
            return "";
        }
    }
}
