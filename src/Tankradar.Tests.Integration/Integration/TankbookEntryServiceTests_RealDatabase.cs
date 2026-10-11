using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den <see cref="TankbookEntryService"/> auf einer echten SQLite-Datei mit echter Fotoablage: Vorgänge überstehen einen Neustart exakt,
/// und das Löschen eines Fahrzeugs entfernt Vorgänge und Belegfotos.
/// </summary>
public class TankbookEntryServiceTests_RealDatabase : IDisposable
{
    private readonly TestDatabase _database = new();

    private TankbookEntryService CreateService()
    {
        var factory = _database.CreateFactory();
        return new TankbookEntryService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<TankbookEntryService>());
    }

    private VehicleService CreateVehicleService()
    {
        var factory = _database.CreateFactory();
        return new VehicleService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<VehicleService>());
    }

    /// <summary>
    /// Prüft, dass Vorgänge mit Dezimalwerten, Zeitpunkt, Art und Notiz nach einem Neustart identisch gelesen werden und der Verbrauch stimmt.
    /// </summary>
    [Fact]
    public async Task Entries_SurviveRestartOnRealDatabaseFile()
    {
        var vehicle = (await CreateVehicleService().CreateVehicleAsync(new VehicleInput("Auris", VehicleFuelKind.Hybrid, 10_000, null))).Vehicle!;
        var service = CreateService();
        var first = new TankbookEntryInput(new DateTime(2026, 5, 17, 9, 5, 0), TankbookEntryKind.Electricity, 12.345m, 0.59m, 7.28m, 10_100, false, "Zuhause", null, "Wallbox", null);
        var second = new TankbookEntryInput(new DateTime(2026, 5, 18, 14, 30, 0), TankbookEntryKind.Electricity, 16.5m, 0.399m, 6.58m, 10_200, false, "Zuhause", null, null, null);
        Assert.Equal(TankbookEntryResult.Ok, (await service.CreateEntryAsync(vehicle.Id, first)).Result);
        Assert.Equal(TankbookEntryResult.Ok, (await service.CreateEntryAsync(vehicle.Id, second)).Result);

        var restarted = await CreateService().GetEntriesAsync(vehicle.Id);

        Assert.Equal(2, restarted.Count);
        var oldest = restarted[1].Entry;
        Assert.Equal("12.345", oldest.Quantity.ToString(CultureInfo.InvariantCulture));
        Assert.Equal("0.59", oldest.UnitPrice.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(new DateTime(2026, 5, 17, 9, 5, 0), oldest.OccurredAt);
        Assert.Equal(TankbookEntryKind.Electricity, oldest.Kind);
        Assert.Equal("Wallbox", oldest.Note);
        Assert.Equal(16.50m, restarted[0].Consumption);
    }

    /// <summary>
    /// Prüft, dass das Löschen eines Fahrzeugs auf einer echten Datei dessen Vorgänge und Belegfotos entfernt, die eines anderen Fahrzeugs nicht.
    /// </summary>
    [Fact]
    public async Task DeleteVehicle_WithEntries_OnRealFile_RemovesEntriesAndPhotos()
    {
        var vehicles = CreateVehicleService();
        var golf = (await vehicles.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 10_000, null))).Vehicle!;
        var polo = (await vehicles.CreateVehicleAsync(new VehicleInput("Polo", VehicleFuelKind.Petrol, 10_000, null))).Vehicle!;
        var store = _database.CreatePhotoStore();
        var golfPhoto = (await store.ImportAsync(new MemoryStream([1, 2, 3]), ".jpg")).FileName!;
        var poloPhoto = (await store.ImportAsync(new MemoryStream([4, 5, 6]), ".png")).FileName!;
        var service = CreateService();
        var input = new TankbookEntryInput(new DateTime(2026, 5, 18, 14, 30, 0), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Alpha Tankstelle", null, null, null);
        await service.CreateEntryAsync(golf.Id, input with { ReceiptPhotoFileName = golfPhoto });
        await service.CreateEntryAsync(polo.Id, input with { ReceiptPhotoFileName = poloPhoto });

        var result = await CreateVehicleService().DeleteVehicleAsync(golf.Id, 1);

        Assert.Equal(new VehicleDeleteResult(VehicleResult.Ok, 1), result);
        Assert.Empty(await CreateService().GetEntriesAsync(golf.Id));
        Assert.Single(await CreateService().GetEntriesAsync(polo.Id));
        Assert.False(store.Exists(golfPhoto));
        Assert.True(store.Exists(poloPhoto));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
