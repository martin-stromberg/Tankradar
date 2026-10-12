using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den <see cref="MAUI.Services.Tankbook.TankbookStatisticsService"/> gegen die Test-Datenbank: Fahrzeugbezug, Zeitquelle und Aktualisierung nach Änderungen.
/// </summary>
public class TankbookStatisticsServiceTests_Read : BaseTest
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
    /// Prüft, dass ein Fahrzeug ohne Vorgänge eine Auswertung ohne Blöcke liefert.
    /// </summary>
    [Fact]
    public async Task Statistics_VehicleWithoutEntries()
    {
        var vehicle = await _fixture.CreateVehicleAsync();

        var load = await _fixture.Statistics.GetStatisticsAsync(vehicle.Id);

        Assert.Equal(TankbookStatisticsStatus.Ok, load.Status);
        Assert.NotNull(load.Statistics);
        Assert.False(load.Statistics.HasEntries);
        Assert.Equal(vehicle.Id, load.Statistics.VehicleId);
    }

    /// <summary>
    /// Prüft, dass ein unbekanntes Fahrzeug als Ergebnis und nicht als Ausnahme gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Statistics_UnknownVehicle_ReturnsVehicleNotFound()
    {
        var load = await _fixture.Statistics.GetStatisticsAsync(4711);

        Assert.Equal(TankbookStatisticsStatus.VehicleNotFound, load.Status);
        Assert.Null(load.Statistics);
    }

    /// <summary>
    /// Prüft, dass die Werte aus den gespeicherten Vorgängen berechnet werden und die Monatsreihe bis zum Monat der Testuhr läuft.
    /// </summary>
    [Fact]
    public async Task Statistics_UsesEntryServiceOrderAndTimeProvider()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 30m));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_500, 40m));
        var createdMonth = _fixture.Now.Month;

        var before = (await _fixture.Statistics.GetStatisticsAsync(vehicle.Id)).Statistics!.Kinds.Single();
        _fixture.Clock.Advance(TimeSpan.FromDays(40));
        var after = (await _fixture.Statistics.GetStatisticsAsync(vehicle.Id)).Statistics!.Kinds.Single();

        Assert.Equal(6.00m, before.AverageConsumption);
        Assert.Equal(createdMonth, before.Months[0].Month);
        Assert.Equal(_fixture.Now.Month, after.Months[0].Month);
        Assert.True(after.Months.Count > before.Months.Count);
        Assert.Equal(0, after.Months[0].EntryCount);
    }

    /// <summary>
    /// Prüft, dass Korrigieren und Löschen die Auswertung beim nächsten Lesen ändern.
    /// </summary>
    [Fact]
    public async Task Statistics_AfterEditAndDelete_ReflectsChange()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_500, 40m));
        var second = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 11_000, 30m));
        var third = await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_500, 25m));

        var initial = (await _fixture.Statistics.GetStatisticsAsync(vehicle.Id)).Statistics!.Kinds.Single();
        await _fixture.Service.UpdateEntryAsync(second.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 11_000, 40m));
        var edited = (await _fixture.Statistics.GetStatisticsAsync(vehicle.Id)).Statistics!.Kinds.Single();
        await _fixture.Service.DeleteEntryAsync(third.Id);
        var deleted = (await _fixture.Statistics.GetStatisticsAsync(vehicle.Id)).Statistics!.Kinds.Single();

        Assert.Equal(5.50m, initial.AverageConsumption);
        Assert.Equal(6.50m, edited.AverageConsumption);
        Assert.Equal(8.00m, deleted.AverageConsumption);
        Assert.Equal(1, deleted.CalculatedIntervals);
    }
}
