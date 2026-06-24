using EntraFlow.Core.Configuration;
using EntraFlow.Core.Graph;
using EntraFlow.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Provisions valid users into Microsoft Entra via Graph. Safe by default: a live
/// write only happens when the connection is both <see cref="EntraConnectionOptions.Enabled"/>
/// and not in <see cref="EntraConnectionOptions.DryRun"/>. Otherwise it reports the
/// actions it <em>would</em> take without contacting Entra. Per-user failures are
/// isolated so one bad record does not abort the batch.
/// </summary>
public sealed class GraphUserSink : IUserSink
{
    private readonly IOptionsMonitor<EntraConnectionOptions> _options;
    private readonly ILogger<GraphUserSink> _logger;

    public GraphUserSink(
        IOptionsMonitor<EntraConnectionOptions> options,
        ILogger<GraphUserSink> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> WriteAsync(
        string sourceName,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(results);

        var options = _options.CurrentValue;
        var valid = results.Where(r => r.IsValid).Select(r => r.User).ToList();

        if (valid.Count == 0)
        {
            return ["No valid users to provision."];
        }

        // Double safety gate — never write live unless explicitly enabled AND not dry-run.
        var live = options.Enabled && !options.DryRun;
        if (!live)
        {
            var reason = !options.Enabled ? "connection disabled" : "dry-run";
            _logger.LogInformation(
                "Graph sink in dry-run ({Reason}): {Count} user(s) would be provisioned.",
                reason, valid.Count);
            return valid
                .Select(u => PlannedUser.FromRecord(u, options.FieldMapping))
                .Select(p => $"DRY-RUN: would create {p.UserPrincipalName} (displayName='{p.DisplayName}')")
                .ToList();
        }

        var client = EntraGraphClientFactory.Create(options);
        var outcomes = new List<string>(valid.Count);

        foreach (var record in valid)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var planned = PlannedUser.FromRecord(record, options.FieldMapping);

            try
            {
                var created = await client.Users.PostAsync(
                    ToGraphUser(planned, options), cancellationToken: cancellationToken);
                _logger.LogInformation("Provisioned {Upn} (id {Id}).", planned.UserPrincipalName, created?.Id);
                outcomes.Add($"Created {planned.UserPrincipalName} (id {created?.Id}).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to provision {Upn}.", planned.UserPrincipalName);
                outcomes.Add($"FAILED {planned.UserPrincipalName}: {ex.Message}");
            }
        }

        return outcomes;
    }

    private static User ToGraphUser(PlannedUser planned, EntraConnectionOptions options)
    {
        var password = string.IsNullOrWhiteSpace(options.TemporaryPassword)
            ? PasswordGenerator.Generate()
            : options.TemporaryPassword;

        return new User
        {
            AccountEnabled = true,
            DisplayName = planned.DisplayName,
            UserPrincipalName = planned.UserPrincipalName,
            MailNickname = planned.MailNickname,
            Department = planned.Department,
            JobTitle = planned.JobTitle,
            UsageLocation = planned.UsageLocation,
            PasswordProfile = new PasswordProfile
            {
                Password = password,
                ForceChangePasswordNextSignIn = options.ForceChangePasswordNextSignIn,
            },
        };
    }
}
