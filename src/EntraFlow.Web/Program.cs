using EntraFlow.Core.Graph;
using Microsoft.AspNetCore.DataProtection;
using EntraFlow.Web.Components;
using EntraFlow.Web.Endpoints;
using EntraFlow.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Blazor (interactive server components).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Options bound from configuration.
builder.Services.AddOptions<StorageOptions>().Bind(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.AddOptions<AdminAuthOptions>().Bind(builder.Configuration.GetSection(AdminAuthOptions.SectionName));

// Secret protection + app services. Persist Data Protection keys to the data
// directory so the encrypted client secret survives restarts (mount this as a
// volume in containers; swap in Azure Key Vault for multi-instance deployments).
var storageOptions = builder.Configuration.GetSection(StorageOptions.SectionName)
    .Get<StorageOptions>() ?? new StorageOptions();
Directory.CreateDirectory(storageOptions.KeysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(storageOptions.KeysFolder))
    .SetApplicationName("EntraFlow");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
builder.Services.AddSingleton<ISettingsStore, JsonSettingsStore>();
builder.Services.AddSingleton<IAuditLog, JsonlAuditLog>();
builder.Services.AddSingleton<IEntraConnectionTester, EntraConnectionTester>();
builder.Services.AddSingleton<ProvisioningRunner>();

// Authentication: a single admin account behind a cookie; Blazor cascading auth state.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Cap upload size (hardening); CSV provisioning files are small.
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAccountEndpoints();
app.MapEntraFlowApi();

// Warn loudly if the app is exposed with the default admin password.
var adminOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminAuthOptions>>().Value;
if (!app.Environment.IsDevelopment() && adminOptions.IsDefaultPassword)
{
    app.Logger.LogWarning(
        "Admin password is unset/default. Set Admin__Password before exposing Entra-Flow.");
}

app.Run();

// Exposed so the test host (WebApplicationFactory) can reference the entry point.
public partial class Program;
