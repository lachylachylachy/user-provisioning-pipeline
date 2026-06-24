using EntraFlow.Core.Configuration;
using EntraFlow.Core.Io;
using EntraFlow.Core.Models;
using EntraFlow.Core.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Default pipeline: reads each CSV in the input folder, validates the records,
/// writes results through the sink, and optionally archives the processed input.
/// </summary>
public sealed class ProvisioningPipeline : IProvisioningPipeline
{
    private readonly IUserReader _reader;
    private readonly IUserValidator _validator;
    private readonly IUserSink _sink;
    private readonly EntraFlowOptions _options;
    private readonly ILogger<ProvisioningPipeline> _logger;

    public ProvisioningPipeline(
        IUserReader reader,
        IUserValidator validator,
        IUserSink sink,
        IOptions<EntraFlowOptions> options,
        ILogger<ProvisioningPipeline> logger)
    {
        _reader = reader;
        _validator = validator;
        _sink = sink;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProvisioningReport>> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_options.InputFolder))
        {
            _logger.LogWarning("Input folder {InputFolder} does not exist; nothing to process.", _options.InputFolder);
            return [];
        }

        var files = Directory
            .EnumerateFiles(_options.InputFolder, "*.csv", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogInformation("No CSV files found in {InputFolder}.", _options.InputFolder);
            return [];
        }

        _logger.LogInformation("Found {Count} file(s) to process in {InputFolder}.", files.Count, _options.InputFolder);

        var reports = new List<ProvisioningReport>(files.Count);
        foreach (var file in files)
        {
            reports.Add(await ProcessFileAsync(file, cancellationToken));
        }

        return reports;
    }

    public async Task<ProvisioningReport> ProcessFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var sourceName = Path.GetFileNameWithoutExtension(path);
        _logger.LogInformation("Processing {File}.", path);

        var users = _reader.Read(path);
        var results = _validator.Validate(users).ToList();

        var validCount = results.Count(r => r.IsValid);
        var errorCount = results.Count - validCount;

        var outputs = await _sink.WriteAsync(sourceName, results, cancellationToken);

        _logger.LogInformation(
            "{File}: {Valid} valid, {Errors} rejected ({Total} total).",
            path, validCount, errorCount, results.Count);

        if (_options.ArchiveProcessedFiles)
        {
            ArchiveInput(path);
        }

        return new ProvisioningReport
        {
            SourceFile = path,
            ValidCount = validCount,
            ErrorCount = errorCount,
            OutputFiles = outputs,
        };
    }

    private void ArchiveInput(string path)
    {
        try
        {
            var archiveDir = Path.Combine(_options.InputFolder, "archive");
            Directory.CreateDirectory(archiveDir);
            var destination = Path.Combine(archiveDir, Path.GetFileName(path));
            File.Move(path, destination, overwrite: true);
            _logger.LogInformation("Archived {File} to {Destination}.", path, destination);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not archive {File}; leaving it in place.", path);
        }
    }
}
