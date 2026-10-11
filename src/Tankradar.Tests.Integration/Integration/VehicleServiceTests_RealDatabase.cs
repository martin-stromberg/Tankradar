using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft den <see cref="VehicleService"/> auf einer echten SQLite-Datei: Fahrzeuge überstehen einen Neustart unverändert, und das Löschen nimmt die Vorgänge mit.
/// </summary>
public class VehicleServiceTests_RealDatabase : IDisposable
{
    private readonly TestDatabase _database = new();

    private VehicleService CreateService()
    {
        var factory = _database.CreateFactory();
        return new VehicleService(factory, _database.CreateInitializer(factory), TimeProvider.System, new RecordingLogger<VehicleService>());
    }

    /// <summary>
    /// Prüft, dass ein Elektrofahrzeug mit 16,55 kWh/100 km und ein Hybrid ohne Verbrauch nach einem Neustart identisch gelesen werden (Dezimalwert exakt).
    /// </summary>
    [Fact]
    public async Task Vehicles_SurviveRestartOnRealDatabaseFile()
    {
        var service = CreateService();
        var zoe = (await service.CreateVehicleAsync(new VehicleInput("Zoe", VehicleFuelKind.Electric, 12000, 16.55m))).Vehicle!;
        var auris = (await service.CreateVehicleAsync(new VehicleInput("Auris", VehicleFuelKind.Hybrid, 120000, null))).Vehicle!;

        var restarted = await CreateService().GetVehiclesAsync();

        Assert.Equal([auris, zoe], restarted);
        Assert.Equal("16.55", restarted[1].StartConsumption!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Prüft das Mitlöschen der Vorgänge über den Dienst auf einer echten Datei.
    /// </summary>
    [Fact]
    public async Task DeleteVehicle_WithEntries_OnRealFile_RemovesEntries()
    {
        var service = CreateService();
        var golf = (await service.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 84350, 5.4m))).Vehicle!.Id;
        var polo = (await service.CreateVehicleAsync(new VehicleInput("Polo", VehicleFuelKind.Petrol, 1000, null))).Vehicle!.Id;
        await using (var context = _database.CreateFactory().CreateDbContext())
        {
            context.TankbookEntries.AddRange(
                new TankbookEntryEntity { VehicleId = golf, CreatedUtc = DateTime.UtcNow },
                new TankbookEntryEntity { VehicleId = golf, CreatedUtc = DateTime.UtcNow },
                new TankbookEntryEntity { VehicleId = polo, CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        Assert.Equal(2, await service.CountEntriesAsync(golf));
        Assert.Equal(VehicleResult.Ok, await CreateService().DeleteVehicleAsync(golf));

        await using var check = _database.CreateFactory().CreateDbContext();
        Assert.Equal([polo], await check.TankbookEntries.Select(e => e.VehicleId).ToListAsync());
        Assert.Equal(["Polo"], (await CreateService().GetVehiclesAsync()).Select(v => v.Name));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
