using EntraFlow.Core.Configuration;
using EntraFlow.Core.Graph;
using EntraFlow.Core.Io;
using EntraFlow.Core.Pipeline;
using EntraFlow.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.DependencyInjection;

/// <summary>
/// Registers the Entra-Flow Core services so hosts only need a single call.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEntraFlowCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<EntraFlowOptions>()
            .Bind(configuration.GetSection(EntraFlowOptions.SectionName));

        services
            .AddOptions<SchemaOptions>()
            .Bind(configuration.GetSection(SchemaOptions.SectionName));

        services
            .AddOptions<EntraConnectionOptions>()
            .Bind(configuration.GetSection(EntraConnectionOptions.SectionName));

        services.TryAddSingletonTimeProvider();

        services.AddSingleton<IUserReader, CsvUserReader>();
        services.AddTransient<IUserValidator, UserValidator>();
        services.AddSingleton<IEntraConnectionTester, EntraConnectionTester>();

        // Concrete sinks, plus an IUserSink resolved from the configured sink mode.
        services.AddSingleton<CsvUserSink>();
        services.AddSingleton<GraphUserSink>();
        services.AddSingleton<IUserSink>(ResolveSink);

        services.AddSingleton<IProvisioningPipeline, ProvisioningPipeline>();

        return services;
    }

    /// <summary>
    /// Picks the active sink from <see cref="EntraConnectionOptions.Sink"/>. Read via
    /// <see cref="IOptionsMonitor{T}"/> so a host that updates settings at runtime can
    /// rebuild the pipeline and have the new mode take effect.
    /// </summary>
    private static IUserSink ResolveSink(IServiceProvider sp)
    {
        var mode = sp.GetRequiredService<IOptionsMonitor<EntraConnectionOptions>>().CurrentValue.Sink;
        var csv = sp.GetRequiredService<CsvUserSink>();
        var graph = sp.GetRequiredService<GraphUserSink>();

        return mode switch
        {
            SinkMode.Graph => graph,
            SinkMode.Both => new CompositeUserSink(csv, graph),
            _ => csv,
        };
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
