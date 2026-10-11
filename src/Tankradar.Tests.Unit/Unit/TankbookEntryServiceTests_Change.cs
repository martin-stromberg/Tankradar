using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Korrigieren und Löschen von Vorgängen im <see cref="MAUI.Services.Tankbook.TankbookEntryService"/> einschließlich der Belegfotos.
/// </summary>
public class TankbookEntryServiceTests_Change : BaseTest
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
    /// Prüft, dass das Korrigieren alle Angaben ändert und den Verbrauch des nächsten Vorgangs neu berechnet.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_ChangesAllFields()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var first = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_500));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 30m));
        var input = new TankbookEntryInput(_fixture.Now.AddDays(-3), TankbookEntryKind.Fuel, 20m, 1.799m, 35.98m, 10_400, true, "Beta Tankstelle", null, "Korrigiert", null);

        var result = await _fixture.Service.UpdateEntryAsync(first.Id, input);

        Assert.Equal(TankbookEntryResult.Ok, result.Result);
        var stored = await _fixture.Service.GetEntryAsync(first.Id);
        Assert.Equal(new TankbookEntry(first.Id, vehicle.Id, input.OccurredAt, TankbookEntryKind.Fuel, 20m, 1.799m, 35.98m, 10_400, true, "Beta Tankstelle", null, "Korrigiert", null), stored);
        var entries = await _fixture.Service.GetEntriesAsync(vehicle.Id);
        Assert.Equal(TankbookConsumptionState.NoPreviousFullEntry, entries[0].ConsumptionState);
    }

    /// <summary>
    /// Prüft, dass eine gespeicherte Art nach einem Wechsel der Kraftstoffart des Fahrzeugs erlaubt bleibt, eine neue unpassende Art aber nicht.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_StoredKindStaysAllowedAfterVehicleKindChange()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var entry = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500));
        await _fixture.Vehicles.UpdateVehicleAsync(vehicle.Id, new VehicleInput("Golf", VehicleFuelKind.Electric, 10_000, null));

        var keep = await _fixture.Service.UpdateEntryAsync(entry.Id, TankbookEntryFixture.Input(entry.OccurredAt, 10_600));
        var created = await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_700));

        Assert.Equal(TankbookEntryResult.Ok, keep.Result);
        Assert.Equal(TankbookEntryResult.KindNotAllowed, created.Result);
    }

    /// <summary>
    /// Prüft, dass ein ersetztes Belegfoto nach dem Speichern gelöscht wird, ein unverändertes nicht.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_ReplacedPhoto_DeletesOldFile()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var oldPhoto = _fixture.Photos.AddFile();
        var newPhoto = _fixture.Photos.AddFile();
        var entry = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_500, photo: oldPhoto));

        await _fixture.Service.UpdateEntryAsync(entry.Id, TankbookEntryFixture.Input(entry.OccurredAt, 10_500, photo: oldPhoto));
        Assert.Empty(_fixture.Photos.Deleted);

        await _fixture.Service.UpdateEntryAsync(entry.Id, TankbookEntryFixture.Input(entry.OccurredAt, 10_500, photo: newPhoto));

        Assert.Equal([oldPhoto], _fixture.Photos.Deleted);
        Assert.Equal(newPhoto, (await _fixture.Service.GetEntryAsync(entry.Id))!.ReceiptPhotoFileName);
    }

    /// <summary>
    /// Prüft, dass das Korrigieren eines unbekannten Vorgangs gemeldet wird.
    /// </summary>
    [Fact]
    public async Task UpdateEntry_Unknown_ReportsNotFound()
    {
        var result = await _fixture.Service.UpdateEntryAsync(4711, TankbookEntryFixture.Input(_fixture.Now, 10_500));

        Assert.Equal(TankbookEntryResult.EntryNotFound, result.Result);
    }

    /// <summary>
    /// Prüft, dass das Löschen die Zeile und danach das Belegfoto entfernt.
    /// </summary>
    [Fact]
    public async Task DeleteEntry_RemovesRowAndPhoto()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var photo = _fixture.Photos.AddFile();
        var entry = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_500, photo: photo));

        Assert.Equal(TankbookEntryResult.Ok, await _fixture.Service.DeleteEntryAsync(entry.Id));

        Assert.Empty(await _fixture.Service.GetEntriesAsync(vehicle.Id));
        Assert.Equal([photo], _fixture.Photos.Deleted);
    }

    /// <summary>
    /// Prüft, dass das Löschen eines unbekannten Vorgangs gemeldet wird.
    /// </summary>
    [Fact]
    public async Task DeleteEntry_Unknown_ReportsNotFound()
    {
        Assert.Equal(TankbookEntryResult.EntryNotFound, await _fixture.Service.DeleteEntryAsync(4711));
    }
}
