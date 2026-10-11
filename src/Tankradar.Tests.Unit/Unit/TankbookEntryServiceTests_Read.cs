using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Lesen der Vorgänge im <see cref="MAUI.Services.Tankbook.TankbookEntryService"/>: Ordnung, Verbrauch, Fahrzeugbezug und Bereinigung verwaister Fotos.
/// </summary>
public class TankbookEntryServiceTests_Read : BaseTest
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
    /// Prüft, dass die Vorgänge neueste zuerst mit berechnetem Verbrauch geliefert werden.
    /// </summary>
    [Fact]
    public async Task GetEntries_NewestFirstWithConsumption()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_500, 40m));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 30m));

        var entries = await _fixture.Service.GetEntriesAsync(vehicle.Id);

        Assert.Equal([11_000, 10_500], entries.Select(entry => entry.Entry.OdometerKm));
        Assert.Equal(6.00m, entries[0].Consumption);
        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, entries[1].ConsumptionState);
        Assert.Equal(await _fixture.Service.GetEntryAsync(entries[0].Entry.Id), entries[0].Entry);
    }

    /// <summary>
    /// Prüft, dass nur die Vorgänge des angefragten Fahrzeugs geliefert werden.
    /// </summary>
    [Fact]
    public async Task GetEntries_OtherVehiclesAreIgnored()
    {
        var golf = await _fixture.CreateVehicleAsync("Golf");
        var polo = await _fixture.CreateVehicleAsync("Polo");
        await _fixture.CreateEntryAsync(golf.Id, TankbookEntryFixture.Input(_fixture.Now, 10_500));
        await _fixture.CreateEntryAsync(polo.Id, TankbookEntryFixture.Input(_fixture.Now, 12_000));

        var entries = await _fixture.Service.GetEntriesAsync(golf.Id);

        Assert.Equal([10_500], entries.Select(entry => entry.Entry.OdometerKm));
        Assert.Empty(await _fixture.Service.GetEntriesAsync(4711));
        Assert.Null(await _fixture.Service.GetEntryAsync(4711));
    }

    /// <summary>
    /// Prüft, dass beim ersten Lesen je Dienst nicht referenzierte Fotos entfernt werden, referenzierte bleiben und später nicht erneut bereinigt wird.
    /// </summary>
    [Fact]
    public async Task GetEntries_FirstCall_DeletesUnreferencedPhotos()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var kept = _fixture.Photos.AddFile();
        var orphan = _fixture.Photos.AddFile(".png");
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_500, photo: kept));

        await _fixture.Service.GetEntriesAsync(vehicle.Id);
        var later = _fixture.Photos.AddFile();
        await _fixture.Service.GetEntriesAsync(vehicle.Id);

        Assert.Equal(new[] { kept, later }.Order(), _fixture.Photos.Files.Order());
        Assert.DoesNotContain(orphan, _fixture.Photos.Files);
        Assert.Single(_fixture.Photos.UnreferencedCleanups);
    }
}
