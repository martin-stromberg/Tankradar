using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Regel des <see cref="VehicleSelectionResolver"/>: gemerktes Fahrzeug, sonst das erste nach Name, ohne Fahrzeuge keines.
/// </summary>
public class VehicleSelectionResolverTests : BaseTest
{
    private static readonly Vehicle Golf = new(1, "Golf", VehicleFuelKind.Diesel, 10_000, null);
    private static readonly Vehicle Polo = new(2, "Polo", VehicleFuelKind.Petrol, 5_000, null);

    /// <summary>
    /// Prüft, dass die gemerkte Kennung Vorrang vor der Reihenfolge nach Namen hat.
    /// </summary>
    [Fact]
    public void Resolve_StoredVehicleExists_ReturnsStored()
    {
        Assert.Same(Polo, VehicleSelectionResolver.Resolve([Golf, Polo], Polo.Id));
    }

    /// <summary>
    /// Prüft, dass bei einer Kennung ohne Fahrzeug (z. B. gelöscht) das erste Fahrzeug nach Name gewählt wird.
    /// </summary>
    [Fact]
    public void Resolve_StoredMissing_ReturnsFirstByName()
    {
        Assert.Same(Golf, VehicleSelectionResolver.Resolve([Golf, Polo], 4711));
    }

    /// <summary>
    /// Prüft, dass ohne gemerkte Kennung das erste Fahrzeug nach Name gewählt wird.
    /// </summary>
    [Fact]
    public void Resolve_NoStored_ReturnsFirstByName()
    {
        Assert.Same(Golf, VehicleSelectionResolver.Resolve([Golf, Polo], null));
    }

    /// <summary>
    /// Prüft, dass ein einzelnes Fahrzeug ohne gemerkte Kennung gewählt wird.
    /// </summary>
    [Fact]
    public void Resolve_SingleVehicle_ReturnsIt()
    {
        Assert.Same(Polo, VehicleSelectionResolver.Resolve([Polo], null));
    }

    /// <summary>
    /// Prüft, dass ohne Fahrzeuge keines gewählt wird.
    /// </summary>
    [Fact]
    public void Resolve_NoVehicles_ReturnsNull()
    {
        Assert.Null(VehicleSelectionResolver.Resolve([], Golf.Id));
    }
}
