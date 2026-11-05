using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Anlegen, Lesen und Bearbeiten von Fahrzeugen im <see cref="MAUI.Services.Tankbook.VehicleService"/>: Speichern aller Felder, eindeutige Namen,
/// Sortierung, Neustart und defensive Lesart unbekannter Kraftstoffarten.
/// </summary>
public class VehicleServiceTests_Manage : BaseTest
{
    private readonly VehicleFixture _fixture = new();

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
    /// Prüft, dass alle Felder gespeichert werden, der Name getrimmt wird und der Anlagezeitpunkt aus der Uhr stammt.
    /// </summary>
    [Fact]
    public async Task CreateVehicle_StoresAllFieldsAndTrimsName()
    {
        var withConsumption = await _fixture.Service.CreateVehicleAsync(new VehicleInput("  Golf  ", VehicleFuelKind.Diesel, 84350, 5.4m));
        var withoutConsumption = await _fixture.Service.CreateVehicleAsync(new VehicleInput("Zoe", VehicleFuelKind.Electric, 0, null));

        Assert.Equal(VehicleResult.Ok, withConsumption.Result);
        Assert.Equal(new Vehicle(withConsumption.Vehicle!.Id, "Golf", VehicleFuelKind.Diesel, 84350, 5.4m), withConsumption.Vehicle);
        Assert.Equal(VehicleResult.Ok, withoutConsumption.Result);
        var stored = await _fixture.Service.GetVehicleAsync(withoutConsumption.Vehicle!.Id);
        Assert.Equal(new Vehicle(withoutConsumption.Vehicle.Id, "Zoe", VehicleFuelKind.Electric, 0, null), stored);
        await using var context = _fixture.Factory.CreateDbContext();
        var entity = await context.Vehicles.SingleAsync(v => v.Name == "Golf");
        Assert.Equal(_fixture.Clock.UtcNow, entity.CreatedUtc);
        Assert.Equal("Diesel", entity.FuelKind);
    }

    /// <summary>
    /// Prüft, dass ein Name ohne Beachtung der Groß- und Kleinschreibung nur einmal vergeben werden kann.
    /// </summary>
    [Fact]
    public async Task CreateVehicle_DuplicateNameIgnoringCase_IsRejected()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Petrol, 0, null));

        var result = await _fixture.Service.CreateVehicleAsync(new VehicleInput("golf", VehicleFuelKind.Diesel, 10, null));

        Assert.Equal(VehicleResult.DuplicateName, result.Result);
        Assert.Null(result.Vehicle);
        Assert.Single(await _fixture.Service.GetVehiclesAsync());
    }

    /// <summary>
    /// Prüft, dass eine ungültige Eingabe ohne Speichern abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task CreateVehicle_InvalidInput_IsRejectedWithoutSaving()
    {
        var result = await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Petrol, -1, null));

        Assert.Equal(VehicleResult.InvalidOdometer, result.Result);
        Assert.Empty(await _fixture.Service.GetVehiclesAsync());
    }

    /// <summary>
    /// Prüft die deutsche Sortierung ohne Beachtung der Groß- und Kleinschreibung.
    /// </summary>
    [Fact]
    public async Task GetVehicles_AreOrderedByNameGerman()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Bus", VehicleFuelKind.Diesel, 0, null));
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Äxtewagen", VehicleFuelKind.Petrol, 0, null));
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("audi", VehicleFuelKind.Hybrid, 0, null));

        var names = (await _fixture.Service.GetVehiclesAsync()).Select(v => v.Name);

        Assert.Equal(["audi", "Äxtewagen", "Bus"], names);
    }

    /// <summary>
    /// Prüft, dass alle Felder geändert werden, auch beim Wechsel zu Elektro und Hybrid.
    /// </summary>
    [Fact]
    public async Task UpdateVehicle_ChangesAllFields()
    {
        var id = (await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 84350, 5.4m))).Vehicle!.Id;

        var toElectric = await _fixture.Service.UpdateVehicleAsync(id, new VehicleInput(" Golf Variant ", VehicleFuelKind.Electric, 90000, 16.5m));
        Assert.Equal(VehicleResult.Ok, toElectric);
        Assert.Equal(new Vehicle(id, "Golf Variant", VehicleFuelKind.Electric, 90000, 16.5m), await _fixture.Service.GetVehicleAsync(id));

        var toHybrid = await _fixture.Service.UpdateVehicleAsync(id, new VehicleInput("Golf Hybrid", VehicleFuelKind.Hybrid, 1, null));
        Assert.Equal(VehicleResult.Ok, toHybrid);
        Assert.Equal(new Vehicle(id, "Golf Hybrid", VehicleFuelKind.Hybrid, 1, null), await _fixture.Service.GetVehicleAsync(id));
    }

    /// <summary>
    /// Prüft, dass der eigene Name in anderer Schreibweise erlaubt, der Name eines anderen Fahrzeugs aber abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task UpdateVehicle_OwnNameOtherCase_IsAllowed_OtherVehiclesName_IsRejected()
    {
        var golf = (await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 0, null))).Vehicle!.Id;
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Polo", VehicleFuelKind.Petrol, 0, null));

        Assert.Equal(VehicleResult.Ok, await _fixture.Service.UpdateVehicleAsync(golf, new VehicleInput("GOLF", VehicleFuelKind.Diesel, 0, null)));
        Assert.Equal(VehicleResult.DuplicateName, await _fixture.Service.UpdateVehicleAsync(golf, new VehicleInput("polo", VehicleFuelKind.Diesel, 0, null)));
        Assert.Equal("GOLF", (await _fixture.Service.GetVehicleAsync(golf))!.Name);
    }

    /// <summary>
    /// Prüft, dass das Ändern eines unbekannten Fahrzeugs gemeldet wird.
    /// </summary>
    [Fact]
    public async Task UpdateVehicle_Unknown_ReportsNotFound()
    {
        var result = await _fixture.Service.UpdateVehicleAsync(4711, new VehicleInput("Golf", VehicleFuelKind.Diesel, 0, null));

        Assert.Equal(VehicleResult.VehicleNotFound, result);
    }

    /// <summary>
    /// Prüft, dass ein neuer Dienst auf derselben Datenbank denselben Stand liest.
    /// </summary>
    [Fact]
    public async Task Vehicles_SurviveRestart()
    {
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 84350, 5.4m));
        await _fixture.Service.CreateVehicleAsync(new VehicleInput("Zoe", VehicleFuelKind.Electric, 12000, null));
        var before = await _fixture.Service.GetVehiclesAsync();

        var after = await _fixture.CreateService().GetVehiclesAsync();

        Assert.Equal(before, after);
    }

    /// <summary>
    /// Prüft, dass eine unbekannte gespeicherte Kraftstoffart als Benzin gelesen und ohne Fahrzeugnamen protokolliert wird.
    /// </summary>
    [Fact]
    public async Task GetVehicles_UnknownStoredFuelKind_FallsBackToPetrolAndLogsWithoutName()
    {
        await using (var context = _fixture.Factory.CreateDbContext())
        {
            context.Vehicles.Add(new VehicleEntity { Name = "Geheimauto", FuelKind = "Wasserstoff", StartOdometerKm = 5, CreatedUtc = _fixture.Clock.UtcNow });
            await context.SaveChangesAsync();
        }

        var vehicle = Assert.Single(await _fixture.Service.GetVehiclesAsync());

        Assert.Equal(VehicleFuelKind.Petrol, vehicle.FuelKind);
        Assert.Contains("Unbekannte Kraftstoffart", _fixture.Logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimauto", _fixture.Logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ein zwischen Namensprüfung und Speichern gleichzeitig angelegter gleicher Name über den eindeutigen Index als doppelter Name gemeldet wird.
    /// </summary>
    [Fact]
    public async Task CreateVehicle_ConcurrentSameName_ReportsDuplicateName()
    {
        _fixture.Factory.Interceptor = new RivalInsertInterceptor(_fixture.Factory, "GOLF");

        var result = await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 0, null));

        Assert.Equal(VehicleResult.DuplicateName, result.Result);
        Assert.Null(result.Vehicle);
        Assert.Equal(["GOLF"], (await _fixture.Service.GetVehiclesAsync()).Select(v => v.Name));
    }

    private sealed class RivalInsertInterceptor : SaveChangesInterceptor
    {
        private readonly InMemoryDbContextFactory _factory;
        private readonly string _rivalName;
        private bool _fired;

        public RivalInsertInterceptor(InMemoryDbContextFactory factory, string rivalName)
        {
            _factory = factory;
            _rivalName = rivalName;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_fired)
            {
                _fired = true;
                using var rival = _factory.CreateDbContext();
                rival.Vehicles.Add(new VehicleEntity { Name = _rivalName, FuelKind = "Petrol", CreatedUtc = DateTime.UtcNow });
                rival.SaveChanges();
            }

            return ValueTask.FromResult(result);
        }
    }

    /// <summary>
    /// Prüft, dass der Start-Kilometerstand nicht über den niedrigsten Kilometerstand der Vorgänge gesetzt werden kann; gleich ist erlaubt.
    /// </summary>
    [Fact]
    public async Task UpdateVehicle_StartOdometerAboveFirstEntry_IsRejected()
    {
        var id = (await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 1000, null))).Vehicle!.Id;
        await _fixture.AddEntriesAsync(id, 2, odometerKm: 1500);

        Assert.Equal(VehicleResult.StartOdometerAboveEntries, await _fixture.Service.UpdateVehicleAsync(id, new VehicleInput("Golf", VehicleFuelKind.Diesel, 1501, null)));
        Assert.Equal(1000, (await _fixture.Service.GetVehicleAsync(id))!.StartOdometerKm);
        Assert.Equal(VehicleResult.Ok, await _fixture.Service.UpdateVehicleAsync(id, new VehicleInput("Golf", VehicleFuelKind.Diesel, 1500, null)));
    }

    /// <summary>
    /// Prüft, dass ein Wechsel der Kraftstoffart auch bei vorhandenen Vorgängen erlaubt ist.
    /// </summary>
    [Fact]
    public async Task UpdateVehicle_FuelKindChangeWithEntries_IsAllowed()
    {
        var id = (await _fixture.Service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 1000, null))).Vehicle!.Id;
        await _fixture.AddEntriesAsync(id, 1, odometerKm: 1500);

        Assert.Equal(VehicleResult.Ok, await _fixture.Service.UpdateVehicleAsync(id, new VehicleInput("Golf", VehicleFuelKind.Electric, 1000, null)));

        Assert.Equal(VehicleFuelKind.Electric, (await _fixture.Service.GetVehicleAsync(id))!.FuelKind);
    }
}
