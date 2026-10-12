using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testdouble für <see cref="ITankbookNavigator"/>, das Aufrufe protokolliert.
/// </summary>
public sealed class FakeTankbookNavigator : ITankbookNavigator
{
    /// <summary>
    /// Die Kennungen der Fahrzeuge, deren Vorgänge geöffnet wurden.
    /// </summary>
    public List<long> Opened { get; } = [];

    /// <summary>
    /// Die Zahl der Aufrufe von <see cref="CloseAsync"/>.
    /// </summary>
    public int Closed { get; private set; }

    /// <inheritdoc />
    public Task OpenEntriesAsync(long vehicleId)
    {
        Opened.Add(vehicleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync()
    {
        Closed++;
        return Task.CompletedTask;
    }
}
