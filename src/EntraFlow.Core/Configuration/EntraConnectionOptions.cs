namespace EntraFlow.Core.Configuration;

/// <summary>
/// Connection and behaviour settings for provisioning users into Microsoft Entra
/// via Graph. Bound from the <c>Entra</c> configuration section. An IT team supplies
/// their own app-registration details here (or via the web Settings page).
/// <para>
/// Safe by default: <see cref="DryRun"/> is true and <see cref="Enabled"/> is false,
/// so no live writes happen until both are deliberately turned on.
/// </para>
/// </summary>
public sealed class EntraConnectionOptions
{
    public const string SectionName = "Entra";

    /// <summary>Directory (tenant) ID of the target Entra tenant.</summary>
    public string TenantId { get; set; } = "";

    /// <summary>Application (client) ID of the app registration.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Client secret for the app registration. Stored encrypted at rest.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>
    /// Master gate for live writes. Even with <see cref="DryRun"/> off, nothing is
    /// written to Entra unless this is true.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>When true (default), planned actions are logged but not executed.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Which sink(s) the pipeline writes through.</summary>
    public SinkMode Sink { get; set; } = SinkMode.Csv;

    /// <summary>Maps input field names to Entra user properties.</summary>
    public GraphFieldMapping FieldMapping { get; set; } = new();

    /// <summary>
    /// Temporary password assigned to created users. When blank, a strong random
    /// password is generated per user (the user is forced to change it on first sign-in).
    /// </summary>
    public string TemporaryPassword { get; set; } = "";

    /// <summary>Whether created users must change their password at first sign-in.</summary>
    public bool ForceChangePasswordNextSignIn { get; set; } = true;

    /// <summary>True when tenant/client/secret are all present.</summary>
    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}

/// <summary>Destination(s) for validated provisioning results.</summary>
public enum SinkMode
{
    /// <summary>Write valid/error CSV files only (default, no Entra calls).</summary>
    Csv = 0,

    /// <summary>Provision valid users into Entra via Graph.</summary>
    Graph = 1,

    /// <summary>Write CSV files and provision into Entra.</summary>
    Both = 2,
}

/// <summary>
/// Maps input fields to Entra (Graph) user properties. Values are input field
/// names; defaults match the built-in Name/Email/Department/Role schema.
/// </summary>
public sealed class GraphFieldMapping
{
    /// <summary>Input field used for <c>displayName</c>.</summary>
    public string DisplayName { get; set; } = "Name";

    /// <summary>Input field used for <c>userPrincipalName</c> (must be a valid UPN/email).</summary>
    public string UserPrincipalName { get; set; } = "Email";

    /// <summary>
    /// Input field used to derive <c>mailNickname</c> (the part before the @ is used).
    /// </summary>
    public string MailNickname { get; set; } = "Email";

    /// <summary>Optional input field mapped to <c>department</c>.</summary>
    public string? Department { get; set; } = "Department";

    /// <summary>Optional input field mapped to <c>jobTitle</c>.</summary>
    public string? JobTitle { get; set; } = "Role";

    /// <summary>Default <c>usageLocation</c> (ISO 3166-1 alpha-2) for created users.</summary>
    public string? UsageLocation { get; set; } = "GB";
}
