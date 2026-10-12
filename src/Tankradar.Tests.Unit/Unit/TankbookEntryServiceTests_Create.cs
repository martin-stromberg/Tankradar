using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Anlegen von Vorgängen im <see cref="MAUI.Services.Tankbook.TankbookEntryService"/>.
/// </summary>
public class TankbookEntryServiceTests_Create : BaseTest
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
    /// Prüft, dass alle Angaben gespeichert werden (Dezimalwerte exakt, Text getrimmt, Anlagezeitpunkt aus der Uhr).
    /// </summary>
    [Fact]
    public async Task CreateEntry_StoresAllFields()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var occurred = _fixture.Now.AddHours(-2);
        var input = new TankbookEntryInput(occurred, TankbookEntryKind.Fuel, 40.125m, 1.859m, 74.59m, 10_500, true, " Alpha Tankstelle ", TankbookEntryFixture.AlphaStationId, "Urlaub", "abc.jpg");

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, input);

        Assert.Equal(TankbookEntryResult.Ok, result.Result);
        await using var context = _fixture.Factory.CreateDbContext();
        var row = await context.TankbookEntries.SingleAsync();
        Assert.Equal(vehicle.Id, row.VehicleId);
        Assert.Equal(occurred, row.OccurredAt);
        Assert.Equal("Fuel", row.Kind);
        Assert.Equal(40.125m, row.Quantity);
        Assert.Equal(1.859m, row.UnitPrice);
        Assert.Equal(74.59m, row.TotalAmount);
        Assert.Equal(10_500, row.OdometerKm);
        Assert.True(row.IsPartial);
        Assert.Equal("Alpha Tankstelle", row.StationName);
        Assert.Equal(TankbookEntryFixture.AlphaStationId, row.StationId);
        Assert.Equal("Urlaub", row.Note);
        Assert.Equal("abc.jpg", row.ReceiptPhotoFileName);
        Assert.Equal(_fixture.Clock.UtcNow, row.CreatedUtc);
        Assert.Equal(row.Id, result.Entry!.Id);
    }

    /// <summary>
    /// Prüft, dass ein unbekanntes Fahrzeug gemeldet wird.
    /// </summary>
    [Fact]
    public async Task CreateEntry_UnknownVehicle_ReportsNotFound()
    {
        var result = await _fixture.Service.CreateEntryAsync(4711, TankbookEntryFixture.Input(_fixture.Now, 10_500));

        Assert.Equal(TankbookEntryResult.VehicleNotFound, result.Result);
    }

    /// <summary>
    /// Prüft, dass eine Art abgelehnt wird, die nicht zur Kraftstoffart passt (Ladevorgang beim Diesel).
    /// </summary>
    [Fact]
    public async Task CreateEntry_KindNotAllowed_IsRejected()
    {
        var vehicle = await _fixture.CreateVehicleAsync();

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_500, kind: TankbookEntryKind.Electricity));

        Assert.Equal(TankbookEntryResult.KindNotAllowed, result.Result);
        Assert.Empty(await _fixture.Service.GetEntriesAsync(vehicle.Id));
    }

    /// <summary>
    /// Prüft, dass eine ungültige Eingabe (Abweichung der Beträge) ohne Speichern abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task CreateEntry_InvalidInput_IsRejectedWithoutSaving()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var input = TankbookEntryFixture.Input(_fixture.Now, 10_500) with { TotalAmount = 99m };

        var result = await _fixture.Service.CreateEntryAsync(vehicle.Id, input);

        Assert.Equal(TankbookEntryResult.AmountMismatch, result.Result);
        Assert.Null(result.Entry);
        Assert.Empty(await _fixture.Service.GetEntriesAsync(vehicle.Id));
    }
}
