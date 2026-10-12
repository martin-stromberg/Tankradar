using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die bekannten Tankstellen im <see cref="MAUI.Services.Tankbook.TankbookEntryService"/>: Favoriten zuerst, danach verwendete Namen, ohne Duplikate, höchstens 30.
/// </summary>
public class TankbookEntryServiceTests_KnownStations : BaseTest
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
    /// Prüft die Reihenfolge: Favoriten-Tankstellen nach Namen (mit Kennung und Ort), danach frei eingegebene Namen nach Namen (ohne Kennung).
    /// </summary>
    [Fact]
    public async Task GetKnownStations_FavoritesFirstThenUsedNames()
    {
        await _fixture.AddFavoriteStationAsync("00000002-0000-4000-8000-000000000000", "Beta Tankstelle", "Potsdam");
        await _fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin");
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_500, stationName: "Zuhause"));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now, 10_600, stationName: "Arbeit"));

        var stations = await _fixture.Service.GetKnownStationsAsync();

        Assert.Equal(
            [
                new KnownStation(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin"),
                new KnownStation("00000002-0000-4000-8000-000000000000", "Beta Tankstelle", "Potsdam"),
                new KnownStation(null, "Arbeit", null),
                new KnownStation(null, "Zuhause", null),
            ],
            stations);
    }

    /// <summary>
    /// Prüft, dass Namen ohne Beachtung der Groß- und Kleinschreibung nur einmal vorkommen, eine Tankstelle in mehreren Gruppen nur einmal erscheint
    /// und ein frei eingegebener Name, der einer Favoriten-Tankstelle entspricht, entfällt.
    /// </summary>
    [Fact]
    public async Task GetKnownStations_NoDuplicatesIgnoringCase()
    {
        await _fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin", "Arbeit");
        await _fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Alpha Tankstelle", "Berlin", "Urlaub");
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_500, stationName: "zuhause"));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_600, stationName: "Zuhause"));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 10_700, stationName: "ALPHA TANKSTELLE"));

        var stations = await _fixture.Service.GetKnownStationsAsync();

        Assert.Equal(2, stations.Count);
        Assert.Equal("Alpha Tankstelle", stations[0].Name);
        Assert.Equal("zuhause", stations[1].Name, ignoreCase: true);
    }

    /// <summary>
    /// Prüft die Begrenzung auf 30 Einträge.
    /// </summary>
    [Fact]
    public async Task GetKnownStations_LimitedTo30()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        for (var i = 0; i < 35; i++)
        {
            var name = string.Create(CultureInfo.InvariantCulture, $"Ort {i:00}");
            await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddMinutes(-60 + i), 10_500 + i, stationName: name));
        }

        var stations = await _fixture.Service.GetKnownStationsAsync();

        Assert.Equal(30, stations.Count);
        Assert.Equal("Ort 00", stations[0].Name);
        Assert.Equal("Ort 29", stations[^1].Name);
    }
}
