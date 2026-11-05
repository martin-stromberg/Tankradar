using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Bereich „In der Nähe“ der Startseite: Sichtbarkeit, Einträge, Filter, Meldungen und Öffnen der Detailansicht.
/// </summary>
public class NearbySectionViewModelTests_Section : FavoritesViewModelTestBase
{
    private static readonly LocationResult Here = LocationResult.Success(new GeoPosition(StationFactory.CenterLatitude, StationFactory.CenterLongitude));

    private static AppSettings Enabled => AppSettings.CreateDefault() with { ShowNearby = true };

    private IReadOnlyList<StationInfo> SetResult(int count, PriceDataSource source = PriceDataSource.Live, PriceFailure failure = PriceFailure.None)
    {
        var stations = Enumerable.Range(1, count)
            .Select(n => StationFactory.Create(n, FreshTime, (FuelType.SuperE5, 1.80m + (n / 100m)), (FuelType.Diesel, 1.60m + (n / 100m))))
            .Reverse()
            .ToList();
        Prices.Result = new StationSearchResult(stations, source, failure);
        return stations;
    }

    /// <summary>
    /// Prüft, dass der Bereich bei ausgeschalteter Einstellung unsichtbar bleibt und nichts abruft.
    /// </summary>
    [Fact]
    public async Task Hidden_WhenSettingOff()
    {
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(AppSettings.CreateDefault(), Here);

        Assert.False(nearby.IsVisible);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass der Bereich bei der Standortnutzung „Nie“ unsichtbar bleibt, auch wenn er eingeschaltet ist.
    /// </summary>
    [Fact]
    public async Task Hidden_WhenGpsNever()
    {
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(Enabled with { GpsUsage = GpsUsage.Never }, Here);

        Assert.False(nearby.IsVisible);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass der Bereich bei eingeschalteter Einstellung und erlaubter Standortnutzung sichtbar ist.
    /// </summary>
    /// <param name="usage">Die erlaubende Standortnutzung.</param>
    [Theory]
    [InlineData(GpsUsage.Always)]
    [InlineData(GpsUsage.WhileInUse)]
    public async Task Visible_WhenEnabledAndAllowed(GpsUsage usage)
    {
        SetResult(2);
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(Enabled with { GpsUsage = usage }, Here);

        Assert.True(nearby.IsVisible);
        Assert.Equal(2, nearby.Entries.Count);
    }

    /// <summary>
    /// Prüft, dass höchstens zehn Einträge nach Entfernung erscheinen und die Umkreissuche mit festem Radius und gewählten Sorten erfolgt.
    /// </summary>
    [Fact]
    public async Task Load_BuildsEntriesSortedByDistanceMaxTen()
    {
        SetResult(12);
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(Enabled, Here);

        Assert.Equal(10, nearby.Entries.Count);
        Assert.Equal(Enumerable.Range(1, 10).Select(n => $"Station {n}"), nearby.Entries.Select(e => e.Name));
        var query = Assert.Single(Prices.Queries);
        Assert.Equal(SearchRadius.Default, query.RadiusKm);
        Assert.Equal([FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel], query.FuelTypes);
        Assert.Equal(["Alle", "Super E5", "Super E10", "Diesel"], nearby.FilterOptions.Select(o => o.Label));
    }

    /// <summary>
    /// Prüft, dass der Filter nach Sorte die Liste ändert, ohne erneut abzurufen.
    /// </summary>
    [Fact]
    public async Task Filter_ByFuelType_ChangesListWithoutNewRequest()
    {
        var stations = SetResult(3).ToList();
        stations[0] = StationFactory.Create(3, FreshTime, (FuelType.SuperE5, 1.85m));
        Prices.Result = new StationSearchResult(stations, PriceDataSource.Live, PriceFailure.None);
        var nearby = CreateViewModel().Nearby;
        await nearby.LoadAsync(Enabled, Here);
        Assert.Equal(3, nearby.Entries.Count);

        nearby.FilterOptions.Single(o => o.FuelType == FuelType.Diesel).SelectCommand.Execute(null);

        Assert.Equal(["Station 1", "Station 2"], nearby.Entries.Select(e => e.Name));
        Assert.Single(Prices.Queries);

        nearby.FilterOptions[0].SelectCommand.Execute(null);

        Assert.Equal(3, nearby.Entries.Count);
    }

    /// <summary>
    /// Prüft, dass ohne verfügbaren Standort die passende Meldung statt Einträgen erscheint und nichts abgerufen wird.
    /// </summary>
    [Fact]
    public async Task NoLocation_ShowsLocationMessage()
    {
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(Enabled, LocationResult.Failure(LocationStatus.PermissionDenied));

        Assert.Equal(SearchTexts.LocationPermissionDenied, nearby.StatusMessage);
        Assert.Empty(nearby.Entries);
        Assert.Empty(Prices.Queries);
        Assert.False(nearby.ShowEmptyHint);
    }

    /// <summary>
    /// Prüft, dass ein Offline-Stand angezeigt und als solcher gekennzeichnet wird.
    /// </summary>
    [Fact]
    public async Task Offline_ShowsLastKnownEntries()
    {
        SetResult(2, PriceDataSource.OfflineFallback, PriceFailure.Offline);
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(Enabled, Here);

        Assert.Equal(2, nearby.Entries.Count);
        Assert.True(nearby.IsOfflineResult);
        Assert.False(nearby.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass das Antippen die Detailansicht öffnet und die Einträge keine Favoritenangaben tragen.
    /// </summary>
    [Fact]
    public async Task Entries_OpenDetailAndOfferNoFavoriteActions()
    {
        SetResult(1);
        var nearby = CreateViewModel().Nearby;
        await nearby.LoadAsync(Enabled, Here);
        var entry = nearby.Entries[0];

        entry.OpenCommand.Execute(null);
        await nearby.LastOpenTask;

        Assert.Equal("Station 1", Assert.Single(StationNavigator.Opened).Name);
        Assert.False(entry.HasNote);
        Assert.Equal("Nearby.Station", entry.AutomationPrefix);
        Assert.DoesNotContain(typeof(HomeStationViewModel).GetProperties(), property => property.Name.Contains("Favorite", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft, dass ohne Treffer der Hinweis „Keine Tankstellen in der Nähe“ erscheint.
    /// </summary>
    [Fact]
    public async Task NoResults_ShowsHint()
    {
        SetResult(0);
        var nearby = CreateViewModel().Nearby;

        await nearby.LoadAsync(Enabled, Here);

        Assert.True(nearby.ShowEmptyHint);
        Assert.Equal(FavoritesTexts.NearbyEmpty, nearby.EmptyHintText);
    }

    /// <summary>
    /// Prüft, dass die Startseite bei ausgeschaltetem Bereich und ohne Favoriten den Standort nicht anfragt.
    /// </summary>
    [Fact]
    public async Task Disabled_DoesNotRequestLocation()
    {
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Empty(Location.Calls);
        Assert.False(viewModel.Nearby.IsVisible);
    }

    /// <summary>
    /// Prüft, dass die Startseite bei eingeschaltetem Bereich auch ohne Favoriten den Standort einmal anfragt.
    /// </summary>
    [Fact]
    public async Task Enabled_RequestsLocationOnceEvenWithoutFavorites()
    {
        Settings.Stored = Enabled;
        SetResult(2);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Single(Location.Calls);
        Assert.Equal(2, viewModel.Nearby.Entries.Count);
        Assert.Empty(Prices.RefreshRequests);
    }
}
