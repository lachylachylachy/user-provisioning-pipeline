using EntraFlow.Core.DependencyInjection;
using EntraFlow.Core.Pipeline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Entra-Flow CLI — drop CSV files into the configured input folder, then run this
// app to validate and split them into valid/error outputs.
//
// Configuration precedence (highest last): appsettings.json -> environment
// variables (EntraFlow__InputFolder, ...) -> command line (--EntraFlow:InputFolder=...).

var builder = Host.CreateApplicationBuilder(args);

// Load appsettings.json from the executable's directory (where it is copied on
// build) so configuration is honored regardless of the current working directory.
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
    optional: true,
    reloadOnChange: false);

// Re-assert environment variables and command line as the highest-precedence
// sources, so they still override the JSON file added above.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddEntraFlowCore(builder.Configuration);

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var pipeline = host.Services.GetRequiredService<IProvisioningPipeline>();

try
{
    var reports = await pipeline.RunAsync();

    if (reports.Count == 0)
    {
        logger.LogInformation("Run complete: no files processed.");
        return 0;
    }

    var totalValid = reports.Sum(r => r.ValidCount);
    var totalErrors = reports.Sum(r => r.ErrorCount);

    logger.LogInformation(
        "Run complete: {Files} file(s), {Valid} valid, {Errors} rejected.",
        reports.Count, totalValid, totalErrors);

    // Non-zero exit when any record was rejected, so schedulers/CI can react.
    return totalErrors > 0 ? 2 : 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Provisioning run failed.");
    return 1;
}

// Marker type so ILogger<Program> resolves with top-level statements.
internal sealed partial class Program;
