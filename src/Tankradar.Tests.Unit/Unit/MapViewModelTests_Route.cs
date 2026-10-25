using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Moduswahl, Sichtbarkeiten, Eingaben und das Ergebnis der Routensuche im <see cref="MAUI.ViewModels.MapViewModel"/>.
/// </summary>
public class MapViewModelTests_Route : MapViewModelRouteTestBase
{
    /// <summary>
    /// Prüft, dass der Routenmodus gewählt werden kann und die Felder der Route statt der Radiuswahl erscheinen.
    /// </summary>
    [Fact]
    public async Task SelectingRouteMode_ShowsRouteFieldsInsteadOfRadius()
    {
        await AppearAsync();

        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Route).SelectCommand.Execute(null);

        Assert.True(ViewModel.IsRouteMode);
        Assert.False(ViewModel.IsAddressMode);
        Assert.False(ViewModel.IsRadiusVisible);
        Assert.Equal(SearchTexts.RouteEmptyState, ViewModel.EmptyStateText);
        Assert.Equal(SearchTexts.RoutingAttribution, ViewModel.RoutingAttributionText);
        Assert.Contains("OpenRouteService", ViewModel.RoutingAttributionText, StringComparison.Ordinal);
        Assert.Contains("OpenStreetMap", ViewModel.RoutingAttributionText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Standardwerte: Start ist der aktuelle Standort, der Abstand 2 km, die Auswahl der Abstände 1, 2, 5 und 10 km.
    /// </summary>
    [Fact]
    public void Defaults_StartIsCurrentLocationAndDistanceTwoKilometers()
    {
        Assert.Equal(RouteStartKind.CurrentLocation, ViewModel.RouteStartKind);
        Assert.False(ViewModel.IsRouteStartAddress);
        Assert.Equal(2, ViewModel.RouteDistanceKm);
        Assert.Equal([1, 2, 5, 10], ViewModel.RouteDistanceOptions.Select(option => option.RadiusKm));
        Assert.Equal([2], ViewModel.RouteDistanceOptions.Where(option => option.IsSelected).Select(option => option.RadiusKm));
        Assert.Equal([RouteStartKind.CurrentLocation, RouteStartKind.Address], ViewModel.RouteStartOptions.Select(option => option.Value));
        Assert.True(ViewModel.RouteStartOptions[0].IsSelected);
        Assert.Equal(string.Empty, ViewModel.StartInput);
        Assert.Equal(string.Empty, ViewModel.DestinationInput);
        Assert.False(ViewModel.HasMapRoute);
        Assert.Null(ViewModel.RouteSummary);
    }

    /// <summary>
    /// Prüft die Auswahl von Startart und Abstand über die Chips.
    /// </summary>
    [Fact]
    public void SelectingChips_ChangesStartKindAndDistance()
    {
        ViewModel.RouteStartOptions.Single(option => option.Value == RouteStartKind.Address).SelectCommand.Execute(null);
        ViewModel.RouteDistanceOptions.Single(option => option.RadiusKm == 10).SelectCommand.Execute(null);

        Assert.Equal(RouteStartKind.Address, ViewModel.RouteStartKind);
        Assert.True(ViewModel.IsRouteStartAddress);
        Assert.Equal(10, ViewModel.RouteDistanceKm);
        Assert.Equal([10], ViewModel.RouteDistanceOptions.Where(option => option.IsSelected).Select(option => option.RadiusKm));
        Assert.Equal([RouteStartKind.Address], ViewModel.RouteStartOptions.Where(option => option.IsSelected).Select(option => option.Value));
    }

    /// <summary>
    /// Prüft, dass das Löschen von Start und Ziel nur das jeweilige Feld leert.
    /// </summary>
    [Fact]
    public void ClearCommands_ClearOnlyTheirField()
    {
        ViewModel.StartInput = StartText;
        ViewModel.DestinationInput = DestinationText;
        Assert.True(ViewModel.HasStartInput);
        Assert.True(ViewModel.HasDestinationInput);

        ViewModel.ClearStartCommand.Execute(null);

        Assert.False(ViewModel.HasStartInput);
        Assert.Equal(DestinationText, ViewModel.DestinationInput);

        ViewModel.ClearDestinationCommand.Execute(null);

        Assert.False(ViewModel.HasDestinationInput);
    }

    /// <summary>
    /// Prüft die erfolgreiche Suche mit Adressstart: Auftrag, Liste in Reihenfolge der Streckenposition, Karte, Route, Zusammenfassung.
    /// </summary>
    [Fact]
    public async Task Search_AddressStart_PublishesStationsRouteAndSummary()
    {
        await PrepareRouteAsync();
        ViewModel.RouteDistanceOptions.Single(option => option.RadiusKm == 5).SelectCommand.Execute(null);
        RouteSearch.Result = RouteSuccess(
            "Berlin-Mitte, Deutschland",
            "Oranienburg, Brandenburg",
            RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m),
            RouteStationAt(2, "Zweite", 21.5, 3.1, 1.80m, 1.60m));
        var versionBefore = ViewModel.MapResultVersion;

        await SearchAsync();

        var request = Assert.Single(RouteSearch.Requests);
        Assert.Null(request.StartPosition);
        Assert.Equal(StartText, request.StartText);
        Assert.Equal(DestinationText, request.DestinationText);
        Assert.Equal(5, request.DistanceKm);
        Assert.NotEmpty(request.FuelTypes);
        Assert.False(ViewModel.IsBusy);
        Assert.Null(ViewModel.StatusMessage);
        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.NotNull(ViewModel.RouteSummary);
        Assert.Contains("Berlin-Mitte, Deutschland", ViewModel.RouteSummary, StringComparison.Ordinal);
        Assert.Contains("Oranienburg, Brandenburg", ViewModel.RouteSummary, StringComparison.Ordinal);
        Assert.Contains("2 Tankstellen", ViewModel.RouteSummary, StringComparison.Ordinal);
        Assert.Contains("5 km", ViewModel.RouteSummary, StringComparison.Ordinal);
        Assert.True(ViewModel.HasMapRoute);
        Assert.True(ViewModel.MapRoute!.Points.Count >= 2);
        Assert.Equal(MapOriginKind.SearchedPlace, ViewModel.Origin!.Kind);
        Assert.Equal(2, ViewModel.MapMarkers.Count);
        Assert.True(ViewModel.MapResultVersion > versionBefore);
        Assert.Contains("Route von Berlin-Mitte, Deutschland nach Oranienburg, Brandenburg eingezeichnet, 2 Tankstellen", ViewModel.MapHintText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Entfernungsangaben der Liste: Streckenposition „ab Start“ und Abstand zur Route.
    /// </summary>
    [Fact]
    public async Task Search_ListShowsAlongPositionAndOffset()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));

        await SearchAsync();

        var item = Assert.Single(ViewModel.Stations);
        Assert.Equal(4.2, item.DistanceKm);
        Assert.Equal("4,2 km ab Start", item.DistanceText);
        Assert.Equal("0,8 km abseits der Route", item.RouteOffsetText);
        Assert.True(item.HasRouteOffset);
    }

    /// <summary>
    /// Prüft Sortierungen und Filter im Routenmodus ohne neuen Aufruf des Routensuchdienstes.
    /// </summary>
    [Fact]
    public async Task SortAndFilter_ReapplyToRouteResultWithoutNewSearch()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess(
            "A",
            "B",
            RouteStationAt(1, "Zeta", 4.0, 0.5, 1.90m, 1.50m),
            RouteStationAt(2, "Alpha", 12.0, 0.5, 1.70m, 1.70m),
            RouteStationAt(3, "Mitte", 8.0, 0.5, 1.80m, 1.60m));
        await SearchAsync();

        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Distance).SelectCommand.Execute(null);
        Assert.Equal(["Zeta", "Mitte", "Alpha"], ViewModel.Stations.Select(item => item.Name));

        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Name).SelectCommand.Execute(null);
        Assert.Equal(["Alpha", "Mitte", "Zeta"], ViewModel.Stations.Select(item => item.Name));

        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Price).SelectCommand.Execute(null);
        Assert.Equal(["Alpha", "Mitte", "Zeta"], ViewModel.Stations.Select(item => item.Name));

        ViewModel.FuelFilterOptions.Single(option => option.FuelType == FuelType.Diesel).SelectCommand.Execute(null);
        Assert.Equal(["Zeta", "Mitte", "Alpha"], ViewModel.Stations.Select(item => item.Name));
        Assert.Equal(3, ViewModel.Stations.Count);
        Assert.Single(RouteSearch.Requests);
    }

    /// <summary>
    /// Prüft den Leerzustand: erfolgreiche Suche ohne Tankstellen zeigt den Hinweis auf einen größeren Abstand, aber die Route.
    /// </summary>
    [Fact]
    public async Task Search_WithoutStations_ShowsEmptyStateAndRoute()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B");

        await SearchAsync();

        Assert.True(ViewModel.ShowEmptyState);
        Assert.Equal(SearchTexts.RouteEmptyState, ViewModel.EmptyStateText);
        Assert.True(ViewModel.HasMapRoute);
        Assert.True(ViewModel.HasMapContent);
        Assert.Empty(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft den Start am aktuellen Standort: Die Position geht nur in den Auftrag, der Starttext nicht.
    /// </summary>
    [Fact]
    public async Task Search_CurrentLocationStart_PassesPositionAndNoText()
    {
        await PrepareRouteAsync(startAddress: false);
        ViewModel.StartInput = "sollte ignoriert werden";
        RouteSearch.Result = RouteSuccess(null, "Oranienburg, Brandenburg", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));

        await SearchAsync();

        var request = Assert.Single(RouteSearch.Requests);
        Assert.NotNull(request.StartPosition);
        Assert.Null(request.StartText);
        Assert.Single(Location.Calls);
        Assert.Equal(MapOriginKind.CurrentLocation, ViewModel.Origin!.Kind);
        Assert.Contains(SearchTexts.RouteStartCurrentLocation, ViewModel.RouteSummary, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass das Wechseln der Suchart Route, Zusammenfassung und Ergebnisse verwirft.
    /// </summary>
    [Fact]
    public async Task SwitchingMode_DiscardsRouteResult()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();
        Assert.True(ViewModel.HasMapRoute);

        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);

        Assert.False(ViewModel.HasMapRoute);
        Assert.Null(ViewModel.MapRoute);
        Assert.Null(ViewModel.RouteSummary);
        Assert.Empty(ViewModel.Stations);
        Assert.Null(ViewModel.Origin);
        Assert.True(ViewModel.IsRadiusVisible);
        Assert.Equal(SearchTexts.EmptyState, ViewModel.EmptyStateText);
        Assert.Equal(MapTexts.NoResults, ViewModel.MapHintText);
    }

    /// <summary>
    /// Prüft, dass Standort- und Adresssuche vom Routenmodus unberührt bleiben.
    /// </summary>
    [Fact]
    public async Task OtherModes_StillSearchWithoutRouteService()
    {
        await AppearAsync();
        Prices.Result = Result(MAUI.Models.Pricing.PriceDataSource.Live, MAUI.Models.Pricing.PriceFailure.None, TwoStations());

        await SearchAsync();

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Empty(RouteSearch.Requests);
        Assert.False(ViewModel.HasMapRoute);
        Assert.Single(Prices.Queries);
    }

    /// <summary>
    /// Prüft den Fortschritt während der laufenden Suche und dass er danach gelöscht ist.
    /// </summary>
    [Fact]
    public async Task Search_ShowsProgressWhileRunningAndClearsItAfterwards()
    {
        await PrepareRouteAsync();
        var gate = new TaskCompletionSource();
        RouteSearch.Gate = gate.Task;
        RouteSearch.ProgressToReport.Add(new RouteSearchProgress(RouteSearchPhase.SearchingStations, 2, 5));
        RouteSearch.Result = RouteSuccess("A", "B");

        var search = ViewModel.SearchAsync();
        await WaitUntilAsync(() => ViewModel.HasRouteProgress);

        Assert.True(ViewModel.IsBusy);
        Assert.Equal("Tankstellen werden gesucht (2 von 5) …", ViewModel.RouteProgressText);

        gate.SetResult();
        await search;

        Assert.False(ViewModel.IsBusy);
        Assert.Null(ViewModel.RouteProgressText);
        Assert.False(ViewModel.HasRouteProgress);
    }

    /// <summary>
    /// Prüft, dass das Verlassen der Seite eine laufende Routensuche abbricht und nichts mehr veröffentlicht.
    /// </summary>
    [Fact]
    public async Task OnDisappearing_CancelsRunningRouteSearch()
    {
        await PrepareRouteAsync();
        var gate = new TaskCompletionSource();
        RouteSearch.Gate = gate.Task;
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));

        var search = ViewModel.SearchAsync();
        await WaitUntilAsync(() => RouteSearch.Requests.Count == 1);
        ViewModel.OnDisappearing();
        await search;

        Assert.False(ViewModel.IsBusy);
        Assert.Empty(ViewModel.Stations);
        Assert.False(ViewModel.HasMapRoute);
    }

    /// <summary>
    /// Prüft (Abweichung 3 der Abnahme), dass nach dem Verlassen der Seite während der Suche der Fortschrittstext geleert und „Suchen“ wieder freigegeben ist,
    /// auch wenn der abgebrochene Lauf selbst nicht mehr aufräumt.
    /// </summary>
    [Fact]
    public async Task OnDisappearing_DuringRouteSearch_ClearsProgressAndReleasesSearch()
    {
        await PrepareRouteAsync();
        var gate = new TaskCompletionSource();
        RouteSearch.Gate = gate.Task;
        RouteSearch.ProgressToReport.Add(new RouteSearchProgress(RouteSearchPhase.SearchingStations, 1, 2));
        RouteSearch.Result = RouteSuccess("A", "B");

        var search = ViewModel.SearchAsync();
        await WaitUntilAsync(() => ViewModel.HasRouteProgress);
        Assert.True(ViewModel.IsBusy);
        Assert.False(ViewModel.IsNotBusy);

        ViewModel.OnDisappearing();

        Assert.Null(ViewModel.RouteProgressText);
        Assert.False(ViewModel.HasRouteProgress);
        Assert.False(ViewModel.IsBusy);
        Assert.True(ViewModel.IsNotBusy);
        await search;
        Assert.Null(ViewModel.RouteProgressText);
        Assert.False(ViewModel.IsBusy);
        Assert.Null(ViewModel.StatusMessage);
    }
}
