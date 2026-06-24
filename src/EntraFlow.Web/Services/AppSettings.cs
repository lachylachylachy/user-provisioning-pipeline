using EntraFlow.Core.Configuration;

namespace EntraFlow.Web.Services;

/// <summary>
/// The runtime-editable configuration an IT team manages through the Settings page:
/// their Entra connection and the validation schema. Persisted by
/// <see cref="ISettingsStore"/> and applied per run by <see cref="ProvisioningRunner"/>.
/// </summary>
public sealed class AppSettings
{
    public EntraConnectionOptions Entra { get; set; } = new();

    public SchemaOptions Schema { get; set; } = SchemaOptions.Default;

    public AppSettings Clone() => new()
    {
        Entra = new EntraConnectionOptions
        {
            TenantId = Entra.TenantId,
            ClientId = Entra.ClientId,
            ClientSecret = Entra.ClientSecret,
            Enabled = Entra.Enabled,
            DryRun = Entra.DryRun,
            Sink = Entra.Sink,
            TemporaryPassword = Entra.TemporaryPassword,
            ForceChangePasswordNextSignIn = Entra.ForceChangePasswordNextSignIn,
            FieldMapping = new GraphFieldMapping
            {
                DisplayName = Entra.FieldMapping.DisplayName,
                UserPrincipalName = Entra.FieldMapping.UserPrincipalName,
                MailNickname = Entra.FieldMapping.MailNickname,
                Department = Entra.FieldMapping.Department,
                JobTitle = Entra.FieldMapping.JobTitle,
                UsageLocation = Entra.FieldMapping.UsageLocation,
            },
        },
        Schema = new SchemaOptions
        {
            UniqueField = Schema.UniqueField,
            Fields = Schema.Fields.Select(f => new FieldRule
            {
                Name = f.Name,
                Required = f.Required,
                Format = f.Format,
                AllowedValues = f.AllowedValues is null ? null : [.. f.AllowedValues],
            }).ToList(),
        },
    };
}
