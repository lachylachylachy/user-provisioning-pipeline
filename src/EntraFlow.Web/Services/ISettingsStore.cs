namespace EntraFlow.Web.Services;

/// <summary>Persists and retrieves the runtime-editable <see cref="AppSettings"/>.</summary>
public interface ISettingsStore
{
    /// <summary>The current settings (a clone; safe to mutate).</summary>
    AppSettings Current { get; }

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
