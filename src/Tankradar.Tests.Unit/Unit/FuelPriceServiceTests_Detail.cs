using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Abruf einzelner Tankstellen mit Cache, Offline-Rückfall und abgeleiteten Hinweisen.
/// </summary>
public class FuelPriceServiceTests_Detail : FuelPriceServiceTestBase
{
    private StationInfo CreateDetail(bool? wholeDay = true)
    {
        var basis = StationFactory.Create(3, Clock.UtcNow, (FuelType.Diesel, 1.60m));
        return new StationInfo
        {
            Id = basis.Id,
            Name = basis.Name,
            Latitude = basis.Latitude,
            Longitude = basis.Longitude,
            WholeDay = wholeDay,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            Prices = basis.Prices,
            DetailsUpdatedUtc = Clock.UtcNow,
        };
    }

    /// <summary>
    /// Prüft, dass der Detailabruf live liefert, speichert und den Hinweis „Automatentankstelle“ ableitbar macht.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_Online_ReturnsLiveAndPersists()
    {
        Client.DetailResult = CreateDetail();

        var result = await Service.GetStationDetailAsync(Client.DetailResult.Id);

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.True(StationHints.IsAutomatedStation(result.Station!.WholeDay, result.Station.OpeningTimes));
        var stored = await Repository.GetStationAsync(Client.DetailResult.Id);
        Assert.Equal("Mo-So", Assert.Single(stored!.OpeningTimes).Text);
        Assert.True(stored.WholeDay);
    }

    /// <summary>
    /// Prüft, dass ein zweiter Abruf innerhalb der Cache-Dauer lokal bedient wird.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_WithinCacheLifetime_UsesCache()
    {
        Client.DetailResult = CreateDetail();
        await Service.GetStationDetailAsync(Client.DetailResult.Id);
        Clock.Advance(TimeSpan.FromMinutes(2));

        var result = await Service.GetStationDetailAsync(Client.DetailResult.Id);

        Assert.Equal(PriceDataSource.Cache, result.Source);
        Assert.Equal(1, Client.DetailCalls);
    }

    /// <summary>
    /// Prüft, dass offline die zuletzt bekannten Daten mit Alter geliefert werden.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_Offline_ReturnsKnownStation()
    {
        Client.DetailResult = CreateDetail();
        await Service.GetStationDetailAsync(Client.DetailResult.Id);
        Clock.Advance(TimeSpan.FromMinutes(90));
        Connection.IsOnline = false;

        var result = await Service.GetStationDetailAsync(Client.DetailResult.Id);

        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
        Assert.Equal(PriceFailure.Offline, result.Failure);
        Assert.Equal("vor 90 Min.", PriceFreshness.FormatAge(result.Station!.Prices.Single().RetrievedUtc, Clock.UtcNow));
        Assert.True(PriceFreshness.IsStale(result.Station.Prices.Single().RetrievedUtc, Clock.UtcNow));
    }

    /// <summary>
    /// Prüft, dass eine unbekannte Station offline ohne Ausnahme als nicht vorhanden gemeldet wird.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_OfflineUnknownStation_ReturnsNullStation()
    {
        Connection.IsOnline = false;

        var result = await Service.GetStationDetailAsync("99999999-0000-4000-8000-000000000000");

        Assert.Null(result.Station);
        Assert.Equal(PriceDataSource.OfflineFallback, result.Source);
    }

    /// <summary>
    /// Prüft, dass ein Abruffehler zum Rückfall führt.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_ApiFailure_FallsBackToKnownData()
    {
        Client.DetailResult = CreateDetail();
        await Service.GetStationDetailAsync(Client.DetailResult.Id);
        Clock.Advance(TimeSpan.FromMinutes(30));
        Client.Failure = new PriceApiException(PriceFailure.Unreachable, "nicht erreichbar");

        var result = await Service.GetStationDetailAsync(Client.DetailResult.Id);

        Assert.Equal(PriceFailure.Unreachable, result.Failure);
        Assert.NotNull(result.Station);
    }

    /// <summary>
    /// Prüft, dass eine ungültige Kennung abgewiesen wird, bevor etwas abgerufen wird.
    /// </summary>
    [Fact]
    public async Task GetStationDetailAsync_InvalidId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Service.GetStationDetailAsync("abc"));

        Assert.Equal(0, Client.DetailCalls);
    }
}
