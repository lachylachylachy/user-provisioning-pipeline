using System.Text.Json;
using Microsoft.Extensions.Options;

namespace EntraFlow.Web.Services;

/// <summary>
/// File-backed settings store (<c>settings.json</c>). The Entra client secret is
/// protected at rest via <see cref="ISecretProtector"/>. Reads return a clone so
/// callers cannot mutate the cached instance.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _path;
    private readonly ISecretProtector _protector;
    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly Lock _gate = new();
    private AppSettings _current;

    public JsonSettingsStore(
        IOptions<StorageOptions> storage,
        ISecretProtector protector,
        ILogger<JsonSettingsStore> logger)
    {
        _path = storage.Value.SettingsFile;
        _protector = protector;
        _logger = logger;
        _current = Load();
    }

    public AppSettings Current
    {
        get
        {
            lock (_gate)
            {
                return _current.Clone();
            }
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var toPersist = settings.Clone();
        toPersist.Entra.ClientSecret = string.IsNullOrEmpty(settings.Entra.ClientSecret)
            ? ""
            : _protector.Protect(settings.Entra.ClientSecret);

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(toPersist, JsonOptions);
        await File.WriteAllTextAsync(_path, json, cancellationToken);

        lock (_gate)
        {
            _current = settings.Clone();
        }
    }

    private AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(_path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            if (!string.IsNullOrEmpty(loaded.Entra.ClientSecret))
            {
                loaded.Entra.ClientSecret = _protector.Unprotect(loaded.Entra.ClientSecret);
            }

            return loaded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}; starting with defaults.", _path);
            return new AppSettings();
        }
    }
}
