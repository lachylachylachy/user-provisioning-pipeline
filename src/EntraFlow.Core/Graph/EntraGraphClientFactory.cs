using Azure.Identity;
using EntraFlow.Core.Configuration;
using Microsoft.Graph;

namespace EntraFlow.Core.Graph;

/// <summary>
/// Builds <see cref="GraphServiceClient"/> instances from connection options using
/// the client-credentials (app-only) flow. Centralised so the sink and the
/// connection tester construct clients identically.
/// </summary>
public static class EntraGraphClientFactory
{
    private static readonly string[] DefaultScopes = ["https://graph.microsoft.com/.default"];

    public static GraphServiceClient Create(EntraConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.HasCredentials)
        {
            throw new InvalidOperationException(
                "Entra credentials are incomplete (TenantId, ClientId and ClientSecret are required).");
        }

        var credential = new ClientSecretCredential(
            options.TenantId,
            options.ClientId,
            options.ClientSecret);

        return new GraphServiceClient(credential, DefaultScopes);
    }
}
