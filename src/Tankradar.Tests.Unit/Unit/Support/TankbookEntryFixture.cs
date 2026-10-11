using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testaufbau für Tank- und Ladevorgänge: echte <see cref="VehicleService"/> und <see cref="TankbookEntryService"/> auf In-Memory-SQLite mit angelegtem Schema,
/// steuerbarer Uhr, Fotoablage-Double und aufzeichnenden Loggern.
/// </summary>
public sealed class TankbookEntryFixture : IDisposable
{
    /// <summary>
    /// Kennung der Favoriten-Tankstelle „Alpha Tankstelle“ in Berlin.
    /// </summary>
    public const string AlphaStationId = "00000001-0000-4000-8000-000000000000";

    /// <summary>
    /// Legt Datenbank, Schema und Dienste an.
    /// </summary>
    public TankbookEntryFixture()
    {
        using (var context = Factory.CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        Vehicles = new VehicleService(Factory, new NoOpDatabaseInitializer(), Photos, Clock, VehicleLogger);
        Service = CreateService();
    }

    /// <summary>
    /// Die Kontext-Factory der Datenbank.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public InMemoryDbContextFactory Factory { get; } = new();

    /// <summary>
    /// Die Testuhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Die Fotoablage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public FakeReceiptPhotoStore Photos { get; } = new();

    /// <summary>
    /// Der Logger des Dienstes der Vorgänge.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public RecordingLogger<TankbookEntryService> Logger { get; } = new();

    /// <summary>
    /// Der Logger des Fahrzeugdienstes.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public RecordingLogger<VehicleService> VehicleLogger { get; } = new();

    /// <summary>
    /// Der Fahrzeugdienst.
    /// </summary>
    public VehicleService Vehicles { get; }

    /// <summary>
    /// Der Dienst der Vorgänge.
    /// </summary>
    public TankbookEntryService Service { get; }

    /// <summary>
    /// Die aktuelle lokale Zeit der Testuhr, minutengenau.
    /// </summary>
    public DateTime Now
    {
        get { return TankbookEntryValidator.TruncateToMinute(Clock.GetLocalNow().DateTime); }
    }

    /// <summary>
    /// Erstellt einen weiteren Dienst auf derselben Datenbank (simuliert einen Neustart der App).
    /// </summary>
    /// <returns>Der neue Dienst.</returns>
    public TankbookEntryService CreateService()
    {
        return new TankbookEntryService(Factory, new NoOpDatabaseInitializer(), Photos, Clock, Logger);
    }

    /// <summary>
    /// Erstellt ein Formular auf dem Dienst dieser Umgebung.
    /// </summary>
    /// <param name="picker">Die Fotoauswahl.</param>
    /// <param name="announcer">Die Ansage.</param>
    /// <param name="logger">Der Logger.</param>
    /// <returns>Das Formular.</returns>
    public TankbookEntryFormViewModel CreateForm(FakeReceiptPhotoPicker picker, FakeScreenReaderAnnouncer announcer, RecordingLogger<TankbookEntryFormViewModel> logger)
    {
        return new TankbookEntryFormViewModel(Service, Photos, picker, Clock, announcer, logger);
    }

    /// <summary>
    /// Legt ein Fahrzeug an.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <param name="startOdometerKm">Der Start-Kilometerstand.</param>
    /// <returns>Das Fahrzeug.</returns>
    public async Task<Vehicle> CreateVehicleAsync(string name = "Golf", VehicleFuelKind fuelKind = VehicleFuelKind.Diesel, int startOdometerKm = 10_000)
    {
        return (await Vehicles.CreateVehicleAsync(new VehicleInput(name, fuelKind, startOdometerKm, null))).Vehicle!;
    }

    /// <summary>
    /// Baut eine gültige Eingabe.
    /// </summary>
    /// <param name="occurredAt">Der Zeitpunkt.</param>
    /// <param name="odometerKm">Der Kilometerstand.</param>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="kind">Die Art.</param>
    /// <param name="isPartial">Gibt an, ob es eine Teilbetankung ist.</param>
    /// <param name="stationName">Der Name der Tankstelle.</param>
    /// <param name="photo">Der Dateiname des Belegfotos.</param>
    /// <returns>Die Eingabe (Preis 1,859 €, Gesamtbetrag berechnet).</returns>
    public static TankbookEntryInput Input(
        DateTime occurredAt,
        int odometerKm,
        decimal quantity = 40m,
        TankbookEntryKind kind = TankbookEntryKind.Fuel,
        bool isPartial = false,
        string stationName = "Alpha Tankstelle",
        string? photo = null)
    {
        var unitPrice = 1.859m;
        var total = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        return new TankbookEntryInput(occurredAt, kind, quantity, unitPrice, total, odometerKm, isPartial, stationName, null, null, photo);
    }

    /// <summary>
    /// Legt einen Vorgang an und erwartet Erfolg.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="input">Die Eingabe.</param>
    /// <returns>Der Vorgang.</returns>
    public async Task<TankbookEntry> CreateEntryAsync(long vehicleId, TankbookEntryInput input)
    {
        var result = await Service.CreateEntryAsync(vehicleId, input);
        Assert.Equal(TankbookEntryResult.Ok, result.Result);
        return result.Entry!;
    }

    /// <summary>
    /// Liest einen gespeicherten Vorgang über die Liste des Fahrzeugs.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="entryId">Die Kennung des Vorgangs.</param>
    /// <returns>Der Vorgang.</returns>
    public async Task<TankbookEntry> ReadEntryAsync(long vehicleId, long entryId)
    {
        return (await Service.GetEntriesAsync(vehicleId)).Single(overview => overview.Entry.Id == entryId).Entry;
    }

    /// <summary>
    /// Legt eine Tankstelle im Preis-Cache an und ordnet sie einer Favoritengruppe zu.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="name">Der Name.</param>
    /// <param name="place">Der Ort.</param>
    /// <param name="groupName">Der Name der Favoritengruppe.</param>
    /// <returns>Ein Task, der nach dem Speichern abgeschlossen ist.</returns>
    public async Task AddFavoriteStationAsync(string stationId, string name, string? place, string groupName = "Arbeit")
    {
        await using var context = Factory.CreateDbContext();
        var group = context.FavoriteGroups.FirstOrDefault(g => g.Name == groupName);
        if (group is null)
        {
            group = new FavoriteGroupEntity { Name = groupName, CreatedUtc = Clock.UtcNow };
            context.FavoriteGroups.Add(group);
        }

        if (!context.Stations.Any(s => s.Id == stationId))
        {
            context.Stations.Add(new StationEntity { Id = stationId, Name = name, Place = place, Latitude = 52.5, Longitude = 13.4 });
        }

        context.FavoriteEntries.Add(new FavoriteEntryEntity { Group = group, StationId = stationId, Priority = nameof(FavoritePriority.None), AddedUtc = Clock.UtcNow });
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Factory.Dispose();
    }
}
