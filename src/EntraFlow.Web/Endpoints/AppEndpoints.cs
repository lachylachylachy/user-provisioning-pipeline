using System.Security.Claims;
using EntraFlow.Core.Configuration;
using EntraFlow.Core.Graph;
using EntraFlow.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace EntraFlow.Web.Endpoints;

/// <summary>Account (login/logout) and the REST API that mirrors the UI.</summary>
public static class AppEndpoints
{
    public const string ApiKeyHeader = "X-Api-Key";

    public static void MapAccountEndpoints(this WebApplication app)
    {
        app.MapPost("/account/login", async (HttpContext http, IOptions<AdminAuthOptions> admin) =>
        {
            var form = await http.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var creds = admin.Value;
            var ok = string.Equals(username, creds.Username, StringComparison.Ordinal)
                && string.Equals(password, creds.Password, StringComparison.Ordinal);

            if (!ok)
            {
                return Results.Redirect("/login?error=1");
            }

            var claims = new List<Claim> { new(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Results.Redirect(SafeRedirect(returnUrl));
        });

        app.MapPost("/account/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        });
    }

    public static void MapEntraFlowApi(this WebApplication app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter<ApiKeyOrCookieFilter>();

        api.MapPost("/runs", async (
            HttpContext http,
            ProvisioningRunner runner,
            CancellationToken ct) =>
        {
            if (!http.Request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Expected multipart/form-data with a 'file' field." });
            }

            var form = await http.Request.ReadFormAsync(ct);
            var file = form.Files["file"];
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "Missing 'file'." });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { error = "Only .csv files are accepted." });
            }

            await using var stream = file.OpenReadStream();
            var result = await runner.RunAsync(
                file.FileName, stream, http.User.Identity?.Name ?? "api", ct);

            return Results.Ok(new
            {
                result.Id,
                result.SourceName,
                result.ValidCount,
                result.ErrorCount,
                result.DryRun,
                result.SinkMode,
                result.Outcomes,
            });
        }).DisableAntiforgery();

        api.MapGet("/runs", async (IAuditLog audit, CancellationToken ct) =>
            Results.Ok(await audit.RecentAsync(100, ct)));

        api.MapGet("/runs/{id}", async (string id, IAuditLog audit, CancellationToken ct) =>
        {
            var entry = (await audit.RecentAsync(int.MaxValue, ct))
                .FirstOrDefault(e => e.Id == id);
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        });

        api.MapGet("/settings", (ISettingsStore store) =>
        {
            var s = store.Current;
            s.Entra.ClientSecret = ""; // never expose the secret
            return Results.Ok(s);
        });

        api.MapPut("/settings", async (AppSettings updated, ISettingsStore store, CancellationToken ct) =>
        {
            // Empty secret means "keep existing".
            if (string.IsNullOrEmpty(updated.Entra.ClientSecret))
            {
                updated.Entra.ClientSecret = store.Current.Entra.ClientSecret;
            }

            await store.SaveAsync(updated, ct);
            return Results.NoContent();
        });

        api.MapPost("/settings/test-connection", async (
            EntraConnectionOptions options,
            ISettingsStore store,
            IEntraConnectionTester tester,
            CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(options.ClientSecret))
            {
                options.ClientSecret = store.Current.Entra.ClientSecret;
            }

            return Results.Ok(await tester.TestConnectionAsync(options, ct));
        });
    }

    private static string SafeRedirect(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? returnUrl
            : "/";

    /// <summary>Allows an /api request when it carries a valid API key or an authenticated cookie.</summary>
    private sealed class ApiKeyOrCookieFilter(IOptions<AdminAuthOptions> admin) : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var configuredKey = admin.Value.ApiKey;

            var keyOk = !string.IsNullOrEmpty(configuredKey)
                && http.Request.Headers.TryGetValue(ApiKeyHeader, out var provided)
                && string.Equals(provided, configuredKey, StringComparison.Ordinal);

            if (keyOk || http.User.Identity?.IsAuthenticated == true)
            {
                return await next(context);
            }

            return Results.Unauthorized();
        }
    }
}
