using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IDelay"/> für Tests: wartet nicht wirklich, sondern merkt sich die angeforderten Wartezeiten.
/// </summary>
public sealed class RecordingDelay : IDelay
{
    /// <summary>
    /// Die angeforderten Wartezeiten in Reihenfolge.
    /// </summary>
    public List<TimeSpan> Delays { get; } = [];

    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        Delays.Add(delay);
        return Task.CompletedTask;
    }
}
