using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Umkreissuche Detailangaben aus früheren Detailabfragen nur bis zur Altersgrenze übernimmt.
/// </summary>
public class FuelPriceServiceTests_DetailAge : FuelPriceServiceTestBase
{
    private static StationSearchQuery Query()
    {
        return new StationSearchQuery(StationFactory.CenterLatitude, StationFactory.CenterLongitude, 25, AllFuels);
    }

    private async Task<StationInfo> SeedDetailAndSearchAfterAsync(TimeSpan delay)
    {
        var basis = StationFactory.Create(3, Clock.UtcNow, (FuelType.Diesel, 1.60m));
        Client.DetailResult = new StationInfo
        {
            Id = basis.Id,
            Name = basis.Name,
            Latitude = basis.Latitude,
            Longitude = basis.Longitude,
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            Prices = basis.Prices,
            DetailsUpdatedUtc = Clock.UtcNow,
        };
        await Service.GetStationDetailAsync(basis.Id);

        Clock.Advance(delay);
        Client.SearchResult.Add(StationFactory.Create(3, Clock.UtcNow, (FuelType.Diesel, 1.55m)));
        var result = await Service.SearchNearbyAsync(Query());
        return Assert.Single(result.Stations);
    }

    /// <summary>
    /// Prüft, dass Öffnungszeiten einer Detailabfrage von vor 23 Stunden in die Suchergebnisse übernommen werden.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_RecentDetails_AreAdopted()
    {
        var station = await SeedDetailAndSearchAfterAsync(TimeSpan.FromHours(23));

        Assert.True(station.WholeDay);
        Assert.Single(station.OpeningTimes);
    }

    /// <summary>
    /// Prüft, dass Öffnungszeiten einer Detailabfrage von vor 25 Stunden nicht mehr übernommen werden.
    /// </summary>
    [Fact]
    public async Task SearchNearbyAsync_OldDetails_AreNotAdopted()
    {
        var station = await SeedDetailAndSearchAfterAsync(TimeSpan.FromHours(25));

        Assert.Null(station.WholeDay);
        Assert.Empty(station.OpeningTimes);
        Assert.Equal(1.55m, station.Prices.Single().Price);
    }

    /// <summary>
    /// Prüft, dass die Detailansicht nach Ablauf der Cache-Dauer die Details neu abfragt, auch wenn sie lokal bekannt sind.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_OldDetails_AreQueriedAgain()
    {
        await SeedDetailAndSearchAfterAsync(TimeSpan.FromHours(25));
        var id = Client.DetailResult!.Id;
        Client.DetailResult = new StationInfo { Id = id, Name = "Neu", WholeDay = false, DetailsUpdatedUtc = Clock.UtcNow };

        var result = await Service.GetStationDetailAsync(id);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(2, Client.DetailCalls);
    }
}
