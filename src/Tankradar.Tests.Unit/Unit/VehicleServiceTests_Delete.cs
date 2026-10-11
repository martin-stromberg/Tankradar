using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Zählen der Vorgänge und Löschen von Fahrzeugen im <see cref="MAUI.Services.Tankbook.VehicleService"/> einschließlich des Mitlöschens der Vorgänge.
/// </summary>
public class VehicleServiceTests_Delete : BaseTest
{
    private readonly VehicleFixture _fixture = new();

    private async Task<long> CreateAsync(string name)
    {
        return (await _fixture.Service.CreateVehicleAsync(new VehicleInput(name, VehicleFuelKind.Diesel, 1000, null))).Vehicle!.Id;
    }

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
    /// Prüft, dass nur die Vorgänge des jeweiligen Fahrzeugs gezählt werden und ein unbekanntes Fahrzeug 0 ergibt.
    /// </summary>
    [Fact]
    public async Task CountEntries_ReturnsOnlyEntriesOfVehicle()
    {
        var golf = await CreateAsync("Golf");
        var polo = await CreateAsync("Polo");
        await _fixture.AddEntriesAsync(golf, 3);
        await _fixture.AddEntriesAsync(polo, 1);

        Assert.Equal(3, await _fixture.Service.CountEntriesAsync(golf));
        Assert.Equal(1, await _fixture.Service.CountEntriesAsync(polo));
        Assert.Equal(0, await _fixture.Service.CountEntriesAsync(4711));
    }

    /// <summary>
    /// Prüft das Löschen eines Fahrzeugs ohne Vorgänge.
    /// </summary>
    [Fact]
    public async Task DeleteVehicle_WithoutEntries_RemovesVehicle()
    {
        var golf = await CreateAsync("Golf");

        Assert.Equal(VehicleResult.Ok, await _fixture.Service.DeleteVehicleAsync(golf));

        Assert.Empty(await _fixture.Service.GetVehiclesAsync());
    }

    /// <summary>
    /// Prüft, dass beim Löschen die eigenen Vorgänge mitgelöscht werden und die Vorgänge eines anderen Fahrzeugs bleiben.
    /// </summary>
    [Fact]
    public async Task DeleteVehicle_WithEntries_DeletesItsEntriesOnly()
    {
        var golf = await CreateAsync("Golf");
        var polo = await CreateAsync("Polo");
        await _fixture.AddEntriesAsync(golf, 3);
        await _fixture.AddEntriesAsync(polo, 2);

        Assert.Equal(VehicleResult.Ok, await _fixture.Service.DeleteVehicleAsync(golf));

        Assert.Equal(0, await _fixture.Service.CountEntriesAsync(golf));
        Assert.Equal(2, await _fixture.Service.CountEntriesAsync(polo));
        Assert.Equal(2, await _fixture.CountAllEntriesAsync());
        Assert.Equal(["Polo"], (await _fixture.Service.GetVehiclesAsync()).Select(v => v.Name));
    }

    /// <summary>
    /// Prüft, dass das Löschen eines unbekannten Fahrzeugs gemeldet wird.
    /// </summary>
    [Fact]
    public async Task DeleteVehicle_Unknown_ReportsNotFound()
    {
        Assert.Equal(VehicleResult.VehicleNotFound, await _fixture.Service.DeleteVehicleAsync(4711));
    }

    /// <summary>
    /// Prüft, dass der Name nach dem Löschen wieder vergeben werden kann.
    /// </summary>
    [Fact]
    public async Task DeleteVehicle_FreesName()
    {
        var golf = await CreateAsync("Golf");
        await _fixture.Service.DeleteVehicleAsync(golf);

        var again = await _fixture.Service.CreateVehicleAsync(new VehicleInput("GOLF", VehicleFuelKind.Petrol, 0, null));

        Assert.Equal(VehicleResult.Ok, again.Result);
    }
}
