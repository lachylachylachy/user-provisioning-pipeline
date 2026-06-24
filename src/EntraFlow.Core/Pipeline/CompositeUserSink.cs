using EntraFlow.Core.Models;

namespace EntraFlow.Core.Pipeline;

/// <summary>
/// Fans a write out to several sinks in order (e.g. write CSV <em>and</em> provision
/// into Entra), concatenating their outcomes.
/// </summary>
public sealed class CompositeUserSink : IUserSink
{
    private readonly IReadOnlyList<IUserSink> _sinks;

    public CompositeUserSink(params IUserSink[] sinks)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        _sinks = sinks;
    }

    public async Task<IReadOnlyList<string>> WriteAsync(
        string sourceName,
        IReadOnlyList<ValidationResult> results,
        CancellationToken cancellationToken = default)
    {
        var outcomes = new List<string>();
        foreach (var sink in _sinks)
        {
            outcomes.AddRange(await sink.WriteAsync(sourceName, results, cancellationToken));
        }

        return outcomes;
    }
}
