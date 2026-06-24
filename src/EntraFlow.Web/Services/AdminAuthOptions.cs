namespace EntraFlow.Web.Services;

/// <summary>
/// Credentials for the single administrative account that protects the app, plus an
/// optional API key for programmatic access. Supplied via configuration / secrets
/// (e.g. environment variables <c>Admin__Username</c>, <c>Admin__Password</c>,
/// <c>Admin__ApiKey</c>). Entra SSO can replace this later.
/// </summary>
public sealed class AdminAuthOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = "admin";

    /// <summary>Admin password. Change this from the default before exposing the app.</summary>
    public string Password { get; set; } = "change-me";

    /// <summary>Optional API key required on <c>/api</c> requests via <c>X-Api-Key</c>.</summary>
    public string ApiKey { get; set; } = "";

    public bool IsDefaultPassword => Password is "change-me" or "";
}
