using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den <see cref="TankbookStatisticsService"/> auf einer echten SQLite-Datei: Die Auswertung wird beim Lesen berechnet, übersteht einen Neustart
/// und verändert die gespeicherten Daten nicht (kein Schemawechsel).
/// </summary>
public class TankbookStatisticsServiceTests_RealDatabase : IDisposable
{
    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero));

    private readonly TestDatabase _database = new();

    private VehicleService CreateVehicleService()
    {
        var factory = _database.CreateFactory();
        return new VehicleService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<VehicleService>());
    }

    private TankbookEntryService CreateEntryService()
    {
        var factory = _database.CreateFactory();
        return new TankbookEntryService(factory, _database.CreateInitializer(factory), _database.CreatePhotoStore(), TimeProvider.System, new RecordingLogger<TankbookEntryService>());
    }

    private TankbookStatisticsService CreateStatisticsService()
    {
        return new TankbookStatisticsService(CreateVehicleService(), CreateEntryService(), Clock);
    }

    private async Task<Vehicle> SeedAsync()
    {
        var vehicle = (await CreateVehicleService().CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 10_000, 5.4m))).Vehicle!;
        var service = CreateEntryService();
        var entries = new[]
        {
            new TankbookEntryInput(new DateTime(2026, 4, 30, 23, 59, 0), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Alpha Tankstelle", null, null, null),
            new TankbookEntryInput(new DateTime(2026, 5, 10, 8, 0, 0), TankbookEntryKind.Fuel, 10m, 1.9m, 19m, 10_700, true, "Alpha Tankstelle", null, null, null),
            new TankbookEntryInput(new DateTime(2026, 5, 20, 8, 0, 0), TankbookEntryKind.Fuel, 20m, 1.859m, 37.18m, 11_000, false, "Alpha Tankstelle", null, null, null),
        };
        foreach (var entry in entries)
        {
            Assert.Equal(TankbookEntryResult.Ok, (await service.CreateEntryAsync(vehicle.Id, entry)).Result);
        }

        return vehicle;
    }

    /// <summary>
    /// Prüft, dass die Auswertung nach einem Neustart (neue Dienste auf derselben Datei) dieselben, erwarteten Werte liefert.
    /// </summary>
    [Fact]
    public async Task Statistics_OnRealDatabaseFile_SurvivesRestartAndMatchesEntries()
    {
        var vehicle = await SeedAsync();

        var first = (await CreateStatisticsService().GetStatisticsAsync(vehicle.Id)).Statistics!;
        var restarted = (await CreateStatisticsService().GetStatisticsAsync(vehicle.Id)).Statistics!;

        var kind = Assert.Single(restarted.Kinds);
        Assert.Equal(TankbookAverageSource.Calculated, kind.AverageSource);
        Assert.Equal(6.00m, kind.AverageConsumption);
        Assert.Equal(11.24m, kind.CostPer100Km);
        Assert.Equal(130.54m, kind.TotalCost);
        Assert.Equal([(2026, 6, 0m), (2026, 5, 56.18m), (2026, 4, 74.36m)], kind.Months.Select(month => (month.Year, month.Month, month.TotalCost)));
        Assert.Equal(first.Kinds[0].Months, kind.Months);
        Assert.Equal(first.Kinds[0].ConsumptionTrend, kind.ConsumptionTrend);
    }

    /// <summary>
    /// Prüft, dass die Auswertung die gespeicherten Vorgänge unverändert lässt und kein Schemawechsel nötig ist.
    /// </summary>
    [Fact]
    public async Task Statistics_ExistingData_NoMigrationNeeded_Unchanged()
    {
        var vehicle = await SeedAsync();
        var before = await CreateEntryService().GetEntriesAsync(vehicle.Id);

        await CreateStatisticsService().GetStatisticsAsync(vehicle.Id);
        var after = await CreateEntryService().GetEntriesAsync(vehicle.Id);

        Assert.Equal(before.Select(overview => overview.Entry), after.Select(overview => overview.Entry));
        Assert.Equal(5.4m, (await CreateVehicleService().GetVehicleAsync(vehicle.Id))!.StartConsumption);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedClock(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public override TimeZoneInfo LocalTimeZone
        {
            get { return TimeZoneInfo.Utc; }
        }
    }
}
