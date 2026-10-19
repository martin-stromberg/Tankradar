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

    /// <summary>
    /// Die Kennungen der Fahrzeuge, deren Seite geschlossen werden sollte.
    /// </summary>
    public List<long> ClosedVehicles { get; } = [];

    /// <summary>
    /// Gibt an, ob <see cref="CloseAsync"/> eine Ausnahme auslöst (z. B. weil die Seite im aktiven Reiter nicht geschlossen werden kann).
    /// </summary>
    public bool FailOnClose { get; set; }

    /// <inheritdoc />
    public Task OpenEntriesAsync(long vehicleId)
    {
        Opened.Add(vehicleId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync(long vehicleId)
    {
        Closed++;
        ClosedVehicles.Add(vehicleId);
        return FailOnClose ? Task.FromException(new InvalidOperationException("Die Seite lässt sich nicht schließen.")) : Task.CompletedTask;
    }
}
