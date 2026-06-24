using EntraFlow.Core.Configuration;
using EntraFlow.Core.Models;
using EntraFlow.Core.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Tests;

public class GraphUserSinkTests
{
    private static ValidationResult Valid(string name, string email) =>
        ValidationResult.Valid(UserRecord.FromCoreFields(name, email, "IT", "Admin"));

    private static GraphUserSink Sink(EntraConnectionOptions options) =>
        new(new StaticOptionsMonitor<EntraConnectionOptions>(options), NullLogger<GraphUserSink>.Instance);

    [Fact]
    public async Task DryRun_PlansWithoutContactingEntra()
    {
        // Even with no credentials, dry-run must not attempt a network call.
        var sink = Sink(new EntraConnectionOptions { Enabled = true, DryRun = true });

        var outcomes = await sink.WriteAsync("batch", [Valid("Jane Doe", "jane@company.com")]);

        Assert.Single(outcomes);
        Assert.Contains("DRY-RUN", outcomes[0]);
        Assert.Contains("jane@company.com", outcomes[0]);
    }

    [Fact]
    public async Task NotEnabled_IsTreatedAsDryRun_EvenWhenDryRunFalse()
    {
        var sink = Sink(new EntraConnectionOptions { Enabled = false, DryRun = false });

        var outcomes = await sink.WriteAsync("batch", [Valid("Jane Doe", "jane@company.com")]);

        Assert.Contains("DRY-RUN", outcomes[0]);
    }

    [Fact]
    public async Task NoValidUsers_ReturnsNothingToProvision()
    {
        var sink = Sink(new EntraConnectionOptions { Enabled = true, DryRun = true });

        var outcomes = await sink.WriteAsync("batch",
            [ValidationResult.Invalid(UserRecord.FromCoreFields("", "", "", ""), ["Missing Name"])]);

        Assert.Single(outcomes);
        Assert.Contains("No valid users", outcomes[0]);
    }

    [Fact]
    public void PlannedUser_DerivesMailNicknameFromEmailLocalPart()
    {
        var record = UserRecord.FromCoreFields("Jane Doe", "jane.doe@company.com", "Finance", "Manager");

        var planned = PlannedUser.FromRecord(record, new GraphFieldMapping());

        Assert.Equal("jane.doe", planned.MailNickname);
        Assert.Equal("jane.doe@company.com", planned.UserPrincipalName);
        Assert.Equal("Jane Doe", planned.DisplayName);
        Assert.Equal("Finance", planned.Department);
        Assert.Equal("Manager", planned.JobTitle);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
