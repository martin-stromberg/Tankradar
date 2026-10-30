using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Kartenansicht des <see cref="MAUI.ViewModels.MapViewModel"/>: Markierungen, Suchposition und deren Verwerfen (die Karte ist immer sichtbar; Trefferliste siehe <see cref="MapViewModelTests_Panel"/>).
/// </summary>
public class MapViewModelTests_MapView : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Standortsuche den eigenen Standort markiert, die Markierungen alle Tankstellen der Ergebnismenge enthalten und die Karte den Ausschnitt neu einpasst (Zähler steigt).
    /// </summary>
    [Fact]
    public async Task LocationSearch_MarksOwnLocationAndAllStations()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        var before = ViewModel.MapResultVersion;

        await SearchAsync();

        Assert.NotNull(ViewModel.Origin);
        Assert.Equal(MapOriginKind.CurrentLocation, ViewModel.Origin!.Kind);
        Assert.Equal(Location.Result.Position!.Latitude, ViewModel.Origin.Latitude);
        Assert.Equal(2, ViewModel.MapMarkers.Count);
        Assert.True(ViewModel.HasMapContent);
        Assert.True(ViewModel.MapResultVersion > before);
    }

    /// <summary>
    /// Prüft, dass die Adresssuche die gesuchte Position markiert und den Standort nicht abfragt.
    /// </summary>
    [Fact]
    public async Task AddressSearch_MarksSearchedPlace_WithoutLocationQuery()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        ViewModel.AddressText = "Berlin";

        await SearchAsync();

        Assert.Equal(MapOriginKind.SearchedPlace, ViewModel.Origin!.Kind);
        Assert.Empty(Location.Calls);
    }

    /// <summary>
    /// Prüft, dass die Karte die gesamte gefilterte Ergebnismenge zeigt (nicht nur die erste Listenseite), bei Filterwechsel neu aufgebaut wird und den Ausschnitt dabei nicht neu einpasst.
    /// </summary>
    [Fact]
    public async Task Markers_CoverWholeResultSet_AndFilterRebuildsWithoutRefit()
    {
        var stations = Enumerable.Range(1, MAUI.ViewModels.MapViewModel.PageSize + 5)
            .Select(number => StationFactory.CreateCustom(number, "S" + number, number, Now, null, true, (FuelType.SuperE5, 1.70m + (number / 100m))))
            .Concat([StationFactory.CreateCustom(900, "Diesel", 900, Now, null, true, (FuelType.SuperE5, 1.99m), (FuelType.Diesel, 1.59m))])
            .ToArray();
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, stations);
        await AppearAsync();
        await SearchAsync();
        var version = ViewModel.MapResultVersion;

        Assert.Equal(MAUI.ViewModels.MapViewModel.PageSize, ViewModel.Stations.Count);
        Assert.Equal(stations.Length, ViewModel.MapMarkers.Count);

        ViewModel.FuelFilterOptions.Single(option => option.FuelType == FuelType.Diesel).SelectCommand.Execute(null);

        Assert.Single(ViewModel.MapMarkers);
        Assert.Equal(PriceLevel.Cheapest, ViewModel.MapMarkers[0].Level);
        Assert.Equal(version, ViewModel.MapResultVersion);
    }

    /// <summary>
    /// Prüft, dass die Suchposition bei Fehlschlag, Moduswechsel und fehlender Standortfreigabe verworfen wird, nie in Logs oder Meldungen steht und nicht im Hinweis „Suche rund um“ erscheint.
    /// </summary>
    [Fact]
    public async Task Origin_IsDiscardedOnFailureAndModeChange_AndNeverLogged()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
        Assert.NotNull(ViewModel.Origin);

        Prices.SearchException = new InvalidOperationException("Fehler");
        await SearchAsync();
        Assert.Null(ViewModel.Origin);
        Assert.Empty(ViewModel.MapMarkers);
        Assert.False(ViewModel.HasMapContent);

        Prices.SearchException = null;
        await SearchAsync();
        Assert.NotNull(ViewModel.Origin);
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        Assert.Null(ViewModel.Origin);

        Location.Result = LocationResult.Failure(LocationStatus.PermissionDenied);
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.CurrentLocation).SelectCommand.Execute(null);
        await SearchAsync();
        Assert.Null(ViewModel.Origin);
        Assert.DoesNotContain(StationFactory.CenterLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture), Logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass eine leere Ergebnismenge bei eingeklappter Trefferliste den Leerzustand der Suche liefert, die Karte aber die Suchposition markiert behält.
    /// </summary>
    [Fact]
    public async Task EmptyResult_ShowsMapWithOriginOnly()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None);
        Settings.Stored = Settings.Stored with { ResultView = ResultView.Map };
        await AppearAsync();

        await SearchAsync();

        Assert.True(ViewModel.ShowEmptyState);
        Assert.False(ViewModel.IsBottomSheetExpanded);
        Assert.True(ViewModel.HasMapContent);
        Assert.False(ViewModel.HasMapRoute);
        Assert.Empty(ViewModel.MapMarkers);
        Assert.NotNull(ViewModel.Origin);
    }
}
