using Tankradar.MAUI.Models.Tankbook;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Namen der bekannten Tankstellen im Tankbuch: Marke bzw. eigener Name statt des Stammdatennamens (z. B. Inhabername).
/// </summary>
public class TankbookEntryServiceTests_KnownStationNames : BaseTest
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
    /// Prüft, dass eine Favoriten-Tankstelle mit Marke unter der Marke erscheint und der eigene Name der Marke vorgeht.
    /// </summary>
    [Fact]
    public async Task GetKnownStations_UsesBrandAndCustomName()
    {
        await _fixture.AddFavoriteStationAsync("00000002-0000-4000-8000-000000000000", "Klaus Heinrich", "Potsdam", brand: "Westfalen");
        await _fixture.AddFavoriteStationAsync(TankbookEntryFixture.AlphaStationId, "Maria Müller", "Berlin", brand: "Aral", customName: "Zuhause");
        await _fixture.AddFavoriteStationAsync("00000003-0000-4000-8000-000000000000", "Citra Tankstelle", null);

        var stations = await _fixture.Service.GetKnownStationsAsync();

        Assert.Equal(
            [
                new KnownStation("00000003-0000-4000-8000-000000000000", "Citra Tankstelle", null),
                new KnownStation("00000002-0000-4000-8000-000000000000", "Westfalen", "Potsdam", "Klaus Heinrich"),
                new KnownStation(TankbookEntryFixture.AlphaStationId, "Zuhause", "Berlin", "Maria Müller"),
            ],
            stations);
    }

    /// <summary>
    /// Prüft, dass zwei Tankstellen gleicher Marke im selben Ort durch Stammdatenname bzw. Straße unterscheidbar bleiben.
    /// </summary>
    [Fact]
    public async Task GetKnownStations_SameBrandSamePlace_CarriesDetail()
    {
        await _fixture.AddFavoriteStationAsync("00000002-0000-4000-8000-000000000000", "JET BERLIN HAUPTSTR. 1", "Berlin", brand: "JET");
        await _fixture.AddFavoriteStationAsync("00000003-0000-4000-8000-000000000000", "JET", "Berlin", brand: "JET");
        await using (var context = _fixture.Factory.CreateDbContext())
        {
            var station = context.Stations.Single(s => s.Id == "00000003-0000-4000-8000-000000000000");
            station.Street = "Nebenweg";
            station.HouseNumber = "22";
            await context.SaveChangesAsync();
        }

        var stations = await _fixture.Service.GetKnownStationsAsync();

        Assert.Equal(
            [
                new KnownStation("00000002-0000-4000-8000-000000000000", "JET", "Berlin", "JET BERLIN HAUPTSTR. 1"),
                new KnownStation("00000003-0000-4000-8000-000000000000", "JET", "Berlin", "Nebenweg 22"),
            ],
            stations);
    }
}
