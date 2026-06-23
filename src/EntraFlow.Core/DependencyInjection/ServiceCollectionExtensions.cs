using EntraFlow.Core.Configuration;
using EntraFlow.Core.Io;
using EntraFlow.Core.Pipeline;
using EntraFlow.Core.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.TryAddSingletonTimeProvider();

        services.AddSingleton<IUserReader, CsvUserReader>();
        services.AddTransient<IUserValidator, UserValidator>();
        services.AddSingleton<IUserSink, CsvUserSink>();
        services.AddSingleton<IProvisioningPipeline, ProvisioningPipeline>();

        return services;
    }

    private static void TryAddSingletonTimeProvider(this IServiceCollection services)
    {
        if (!services.Any(d => d.ServiceType == typeof(TimeProvider)))
        {
            services.AddSingleton(TimeProvider.System);
        }
    }
}
