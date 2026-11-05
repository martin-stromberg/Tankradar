using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Nachladen des sichtbaren Kartenausschnitts im <see cref="MapViewModel"/>: Schaltfläche nur nach einer Bewegung und nur in den Sucharten Standort und Adresse,
/// keine automatische Abfrage, Hinweis bei zu großem Ausschnitt, eine Abfrage mit Ergebnisersetzung ohne Einpassen und Verwerfen beim Wechsel der Suchart.
/// </summary>
public class MapViewModelTests_AreaReload : MapViewModelTestBase
{
    private void Reload()
    {
        ViewModel.ReloadVisibleAreaCommand.Execute(null);
    }

    private async Task<MapViewport> SearchAndFitAsync()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
        var fitted = MapViewport.Create(StationFactory.CenterLatitude, StationFactory.CenterLongitude, 14, 400, 700);
        ViewModel.OnViewportFitted(fitted);
        return fitted;
    }

    private async Task<MapViewport> ReloadSameAreaAsync()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync();
        Reload();
        await ViewModel.LastSearchTask;
        return viewport;
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche nach dem Einpassen einer neuen Suche nicht erscheint: Der eingepasste Ausschnitt gilt als Ausschnitt der Suche.
    /// </summary>
    [Fact]
    public async Task OnViewportFitted_KeepsButtonHidden_AfterNewSearch()
    {
        await SearchAndFitAsync();

        Assert.False(ViewModel.ShowAreaReloadButton);
        Assert.False(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche erscheint, sobald der Anwender den Ausschnitt gegenüber dem eingepassten Ausschnitt der Suche verschiebt.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_ShowsButton_WhenMovedFromFittedSearchView()
    {
        var fitted = await SearchAndFitAsync();

        ViewModel.OnViewportChanged(fitted.PanByPixels(120, 0));

        Assert.True(ViewModel.ShowAreaReloadButton);
        Assert.True(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
        Assert.Null(ViewModel.AreaReloadHint);
    }

    /// <summary>
    /// Prüft, dass „Zurücksetzen“ (die Karte meldet wieder den eingepassten Ausschnitt der Suche) die Schaltfläche ausblendet.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_HidesButton_WhenBackAtFittedSearchView()
    {
        var fitted = await SearchAndFitAsync();
        ViewModel.OnViewportChanged(fitted.ZoomBy(-1));

        ViewModel.OnViewportChanged(fitted);

        Assert.False(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass eine neue Suche die Schaltfläche ausblendet, auch wenn der Anwender den Ausschnitt zuvor bewegt hatte.
    /// </summary>
    [Fact]
    public async Task Search_HidesButton_AfterMovedView()
    {
        var fitted = await SearchAndFitAsync();
        ViewModel.OnViewportChanged(fitted.PanByPixels(120, 0));

        await SearchAsync();

        Assert.False(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche nach dem Nachladen für denselben Ausschnitt verschwindet.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_HidesButton_AfterReloadOfSameArea()
    {
        await ReloadSameAreaAsync();

        Assert.False(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass eine reine Größenänderung der Karte (gleiche Mitte und Zoomstufe) die Schaltfläche nicht einblendet.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_IgnoresResizeOnly()
    {
        var viewport = await ReloadSameAreaAsync();

        ViewModel.OnViewportChanged(viewport.Resize(500, 800));

        Assert.False(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass Verschieben nach dem Nachladen die Schaltfläche wieder einblendet.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_ShowsButton_AfterPan()
    {
        var viewport = await ReloadSameAreaAsync();

        ViewModel.OnViewportChanged(viewport.PanByPixels(120, 0));

        Assert.True(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass Zoomen nach dem Nachladen die Schaltfläche wieder einblendet.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_ShowsButton_AfterZoom()
    {
        var viewport = await ReloadSameAreaAsync();

        ViewModel.OnViewportChanged(viewport.ZoomBy(1));

        Assert.True(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass Bewegungen der Karte allein keine Abfrage auslösen (ADR 0014: Abfragen nur auf ausdrückliche Aktion).
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_DoesNotQuery()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync();

        ViewModel.OnViewportChanged(viewport.PanByPixels(50, 50));
        ViewModel.OnViewportChanged(viewport.ZoomBy(2));
        ViewModel.OnViewportChanged(viewport.ZoomBy(-1));

        Assert.True(ViewModel.ShowAreaReloadButton);
        Assert.Single(Prices.Queries);
        Assert.Single(Location.Calls);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass im Routenmodus keine Schaltfläche erscheint und das Nachladen nichts abfragt.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_HidesButton_InRouteMode()
    {
        await AppearAndViewportAsync(SearchMode.Route, search: false);
        Assert.False(ViewModel.ShowAreaReloadButton);
        Assert.False(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));

        Reload();
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche in der Adresssuche nach einer Bewegung erscheint.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_ShowsButton_InAddressMode()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());

        await AppearAndViewportAsync(SearchMode.Address);

        Assert.True(ViewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass bei einem Ausschnitt über 25 km Radius die Schaltfläche mit dem Hinweis „Bitte näher heranzoomen“ erscheint, aber nicht bedienbar ist und nichts abfragt.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_DisablesButtonWithHint_WhenTooLarge()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync(zoom: 8);

        Assert.True(ViewModel.ShowAreaReloadButton);
        Assert.Equal(MapTexts.ReloadAreaTooLarge, ViewModel.AreaReloadHint);
        Assert.True(ViewModel.HasAreaReloadHint);
        Assert.False(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));

        Reload();
        Assert.Single(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass nach einem zu großen Ausschnitt das Heranzoomen den Hinweis entfernt und die Schaltfläche wieder bedienbar macht.
    /// </summary>
    [Fact]
    public async Task OnViewportChanged_ClearsTooLargeHint_AfterZoomIn()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync(zoom: 8);

        ViewModel.OnViewportChanged(viewport.ZoomBy(5));

        Assert.Null(ViewModel.AreaReloadHint);
        Assert.True(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass das Nachladen genau eine Umkreisabfrage um die Mitte des Ausschnitts sendet, Liste, Markierungen und Zähler ersetzt, die Suchposition behält
    /// und den Ausschnitt nicht neu einpasst (<see cref="MapFitMode.KeepViewport"/>).
    /// </summary>
    [Fact]
    public async Task ReloadVisibleArea_QueriesOnce_ReplacesResult_KeepsViewport()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
        var origin = ViewModel.Origin;
        Assert.Equal(MapFitMode.FitToResult, ViewModel.MapFitMode);
        var viewport = MapViewport.Create(52.60, 13.50, 13, 400, 700);
        ViewModel.OnViewportChanged(viewport);
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, StationFactory.CreateCustom(7, "Bereich", 0.5, Now, true, true, (FuelType.SuperE5, 1.70m)));

        Reload();
        await ViewModel.LastSearchTask;

        Assert.Equal(2, Prices.Queries.Count);
        var expected = MapAreaQuery.From(viewport.GetBounds(), [FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel])!;
        var query = Prices.Queries[^1];
        Assert.Equal(expected.Latitude, query.Latitude, 6);
        Assert.Equal(expected.Longitude, query.Longitude, 6);
        Assert.Equal(5, query.RadiusKm);
        Assert.Equal(["Bereich"], ViewModel.Stations.Select(station => station.Name));
        Assert.Single(ViewModel.MapMarkers);
        Assert.Equal(1, ViewModel.TotalStationCount);
        Assert.Same(origin, ViewModel.Origin);
        Assert.Equal(MapFitMode.KeepViewport, ViewModel.MapFitMode);
        Assert.False(ViewModel.ShowAreaReloadButton);
        Assert.False(ViewModel.IsAreaReloading);
        Assert.Single(Location.Calls);
    }

    /// <summary>
    /// Prüft, dass eine neue Suche nach dem Nachladen den Ausschnitt wieder auf das Ergebnis einpasst.
    /// </summary>
    [Fact]
    public async Task Search_NewSearchAfterReload_FitsToResult()
    {
        await ReloadSameAreaAsync();
        Assert.Equal(MapFitMode.KeepViewport, ViewModel.MapFitMode);

        await SearchAsync();

        Assert.Equal(MapFitMode.FitToResult, ViewModel.MapFitMode);
    }

    /// <summary>
    /// Prüft, dass ein Wechsel der Suchart ein laufendes Nachladen abbricht, die Schaltfläche ausblendet und das späte Ergebnis verwirft.
    /// </summary>
    [Fact]
    public async Task ChangeMode_DiscardsReload()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAndViewportAsync();
        var gate = new TaskCompletionSource();
        Prices.SearchGate = gate.Task;

        Reload();
        Assert.True(ViewModel.IsAreaReloading);
        Assert.True(ViewModel.IsBusy);

        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        gate.SetResult();
        await ViewModel.LastSearchTask;

        Assert.False(ViewModel.IsAreaReloading);
        Assert.False(ViewModel.IsBusy);
        Assert.False(ViewModel.ShowAreaReloadButton);
        Assert.Null(ViewModel.AreaReloadHint);
        Assert.Empty(ViewModel.Stations);
        Assert.Null(ViewModel.Origin);
        Assert.Null(ViewModel.StatusMessage);
    }
}
