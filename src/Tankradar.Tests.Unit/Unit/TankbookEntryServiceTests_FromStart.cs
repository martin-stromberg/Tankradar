using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass der <see cref="MAUI.Services.Tankbook.TankbookEntryService"/> den Start-Kilometerstand des Fahrzeugs als Basis des ersten vollen Vorgangs nutzt,
/// und dass die Auswertung daraus den gekennzeichneten Durchschnitt bildet.
/// </summary>
public class TankbookEntryServiceTests_FromStart : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixture.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass der erste volle Vorgang eines Fahrzeugs einen Verbrauch ab Startstand liefert, Teilbetankungen davor mitgezählt.
    /// </summary>
    [Fact]
    public async Task GetEntries_FirstFull_CalculatedFromVehicleStart()
    {
        var vehicle = await _fixture.CreateVehicleAsync(startOdometerKm: 10_000);
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_200, 10m, isPartial: true));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_500, 20m));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 25m));

        var entries = await _fixture.Service.GetEntriesAsync(vehicle.Id);

        Assert.Equal(5.00m, entries[0].Consumption);
        Assert.False(entries[0].IsFromStart);
        Assert.Equal(6.00m, entries[1].Consumption);
        Assert.True(entries[1].IsFromStart);
        Assert.Equal(TankbookConsumptionState.Partial, entries[2].ConsumptionState);
    }

    /// <summary>
    /// Prüft, dass bei einem Elektrofahrzeug der Strom ab Startstand berechnet wird.
    /// </summary>
    [Fact]
    public async Task GetEntries_ElectricVehicle_CalculatesElectricityFromStart()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Taycan", VehicleFuelKind.Electric, 5_000);
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 5_200, 30m, TankbookEntryKind.Electricity));

        var entry = Assert.Single(await _fixture.Service.GetEntriesAsync(vehicle.Id));

        Assert.Equal(15.00m, entry.Consumption);
        Assert.True(entry.IsFromStart);
    }

    /// <summary>
    /// Prüft, dass ein Hybridfahrzeug nur den Kraftstoff ab Startstand berechnet.
    /// </summary>
    [Fact]
    public async Task GetEntries_HybridVehicle_ElectricityHasNoStartBasis()
    {
        var vehicle = await _fixture.CreateVehicleAsync("Prius", VehicleFuelKind.Hybrid, 10_000);
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_500, 30m));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_600, 20m, TankbookEntryKind.Electricity));

        var entries = await _fixture.Service.GetEntriesAsync(vehicle.Id);

        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, entries[0].ConsumptionState);
        Assert.True(entries[1].IsFromStart);
    }

    /// <summary>
    /// Prüft, dass die Auswertung eines Fahrzeugs mit einem vollen Vorgang den gekennzeichneten Durchschnitt ab Startstand zeigt.
    /// </summary>
    [Fact]
    public async Task Statistics_OneFullEntry_ShowsFromStartAverage()
    {
        var vehicle = await _fixture.CreateVehicleAsync(startOdometerKm: 10_000);
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500, 30m));

        var load = await _fixture.Statistics.GetStatisticsAsync(vehicle.Id);

        var kind = Assert.Single(load.Statistics!.Kinds);
        Assert.Equal(TankbookAverageSource.FromStart, kind.AverageSource);
        Assert.Equal(6.00m, kind.AverageConsumption);
    }
}
