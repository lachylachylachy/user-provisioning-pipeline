using Microsoft.Extensions.Options;

namespace EntraFlow.Core.Options;

/// <summary>
/// An <see cref="IOptionsMonitor{T}"/> wrapping a fixed value. Useful for building
/// services from a settings snapshot (e.g. per web request) or in tests, where the
/// value is known up front rather than bound from configuration.
/// </summary>
public sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
