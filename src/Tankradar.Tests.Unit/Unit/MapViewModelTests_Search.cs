using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Ablauf der Suche im <see cref="MapViewModel"/>: Reihenfolge der Aufrufe, Abbruchbedingungen, Anfrage und Statushinweise.
/// </summary>
public class MapViewModelTests_Search : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass der Radius vorbelegt 5 ist und die Seite beim Erscheinen weder Standort noch Preisdienst abfragt.
    /// </summary>
    [Fact]
    public async Task Appearing_DoesNotQueryLocationOrPrices()
    {
        await AppearAsync();

        Assert.Equal("5", ViewModel.RadiusText);
        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.ShowEmptyState);
    }

    /// <summary>
    /// Prüft, dass ein ungültiger Radius weder den Standort noch den Preisdienst aufruft und eine Meldung zeigt.
    /// </summary>
    /// <param name="radius">Die Eingabe.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("26")]
    [InlineData("-3")]
    [InlineData("5.5")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task InvalidRadius_CallsNeitherLocationNorPrices(string radius)
    {
        await AppearAsync();
        ViewModel.RadiusText = radius;

        await SearchAsync();

        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.Equal(SearchTexts.RadiusInvalid, ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass die Anfrage Radius, Position und alle in den Einstellungen gewählten Sorten enthält.
    /// </summary>
    [Fact]
    public async Task Success_SendsRadiusPositionAndAllSelectedFuelTypes()
    {
        Settings.Stored = Settings.Stored with { FuelTypes = SearchTestData.Only(FuelType.Diesel, FuelType.SuperE5) };
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        ViewModel.RadiusText = "25";

        await SearchAsync();

        var query = Assert.Single(Prices.Queries);
        Assert.Equal(25, query.RadiusKm);
        Assert.Equal(StationFactory.CenterLatitude, query.Latitude);
        Assert.Equal(StationFactory.CenterLongitude, query.Longitude);
        Assert.Equal([FuelType.Diesel, FuelType.SuperE5], query.FuelTypes);
        Assert.Equal([GpsUsage.WhileInUse], Location.Calls);
        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Null(ViewModel.StatusMessage);
        Assert.True(ViewModel.HasResults);
    }

    /// <summary>
    /// Prüft, dass ein gültiger Radius von 1 und der Standardradius ohne Änderung gesendet werden und nie ein Radius über 25.
    /// </summary>
    /// <param name="radius">Die Eingabe.</param>
    /// <param name="expected">Der erwartete gesendete Radius.</param>
    [Theory]
    [InlineData("1", 1)]
    [InlineData("5", 5)]
    [InlineData("25", 25)]
    public async Task ValidRadius_IsSentUnchangedAndNeverAbove25(string radius, int expected)
    {
        await AppearAsync();
        ViewModel.RadiusText = radius;

        await SearchAsync();

        var query = Assert.Single(Prices.Queries);
        Assert.Equal(expected, query.RadiusKm);
        Assert.InRange(query.RadiusKm, 1, 25);
    }

    /// <summary>
    /// Prüft, dass „Nie“ an den Standortdienst weitergegeben wird und ohne Standort keine Preisabfrage stattfindet.
    /// </summary>
    [Fact]
    public async Task GpsNever_PassesSettingAndSkipsPriceService()
    {
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };
        Location.Result = LocationResult.Failure(LocationStatus.DisabledBySetting);
        await AppearAsync();

        await SearchAsync();

        Assert.Equal([GpsUsage.Never], Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.Equal(SearchTexts.LocationDisabledBySetting, ViewModel.StatusMessage);
        Assert.Empty(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft den Hinweistext je Standortstatus; die Liste bleibt leer, der Leerzustand wird nicht gezeigt.
    /// </summary>
    /// <param name="status">Der Standortstatus.</param>
    /// <param name="expected">Der erwartete Text.</param>
    [Theory]
    [InlineData(LocationStatus.DisabledBySetting, SearchTexts.LocationDisabledBySetting)]
    [InlineData(LocationStatus.PermissionDenied, SearchTexts.LocationPermissionDenied)]
    [InlineData(LocationStatus.Unavailable, SearchTexts.LocationUnavailable)]
    public async Task LocationProblems_ShowMatchingHintAndEmptyList(LocationStatus status, string expected)
    {
        Location.Result = LocationResult.Failure(status);
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(expected, ViewModel.StatusMessage);
        Assert.Empty(ViewModel.Stations);
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.ShowEmptyState);
    }

    /// <summary>
    /// Prüft, dass ein früherer Treffer nach einer Suche ohne Standort verschwindet und eine erfolgreiche Suche den Hinweis wieder entfernt.
    /// </summary>
    [Fact]
    public async Task NewSearch_ReplacesPreviousResultAndStatus()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
        Location.Result = LocationResult.Failure(LocationStatus.PermissionDenied);

        await SearchAsync();

        Assert.Empty(ViewModel.Stations);
        Assert.Equal(SearchTexts.LocationPermissionDenied, ViewModel.StatusMessage);

        Location.Result = LocationResult.Success(new GeoPosition(52.52, 13.405));
        await SearchAsync();

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Null(ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein leeres Ergebnis ohne Fehler den Leerzustand zeigt.
    /// </summary>
    [Fact]
    public async Task NoStations_WithoutFailure_ShowsEmptyState()
    {
        await AppearAsync();

        await SearchAsync();

        Assert.True(ViewModel.ShowEmptyState);
        Assert.Null(ViewModel.StatusMessage);
        Assert.False(ViewModel.HasResults);
    }

    /// <summary>
    /// Prüft, dass der Suchbefehl die Suche startet.
    /// </summary>
    [Fact]
    public async Task SearchCommand_StartsSearch()
    {
        await AppearAsync();

        ViewModel.SearchCommand.Execute(null);
        await ViewModel.LastSearchTask;

        Assert.Single(Prices.Queries);
    }
}
