using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass Zusammenfassung und Kartenhinweis die Parameter der Anfrage (Abstand, Startart, Orte) beschreiben und nicht den späteren Zustand der Oberfläche,
/// dass Chips und Eingaben während einer laufenden Routensuche gesperrt sind und dass sich die Suche abbrechen lässt.
/// </summary>
public class MapViewModelTests_RouteRequest : MapViewModelRouteTestBase
{
    private async Task<TaskCompletionSource> StartGatedSearchAsync(bool startAddress = true)
    {
        await PrepareRouteAsync(startAddress);
        var gate = new TaskCompletionSource();
        RouteSearch.Gate = gate.Task;
        RouteSearch.Result = RouteSuccess("Berlin-Mitte", "Oranienburg, Brandenburg", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        LastSearch = ViewModel.SearchAsync();
        await WaitUntilAsync(() => RouteSearch.Requests.Count == 1);
        return gate;
    }

    private Task LastSearch { get; set; } = Task.CompletedTask;

    /// <summary>
    /// Prüft (Abweichung 2 der Abnahme), dass die Zusammenfassung den Abstand der Anfrage nennt, auch wenn der Abstand während der Suche geändert wird:
    /// Der Wechsel wird abgewiesen, und die Auswahl bleibt beim Abstand der Anfrage.
    /// </summary>
    [Fact]
    public async Task DistanceChangeDuringSearch_IsRejectedAndSummaryUsesRequestDistance()
    {
        await PrepareRouteAsync();
        ViewModel.RouteDistanceOptions.Single(option => option.RadiusKm == 2).SelectCommand.Execute(null);
        var gate = await StartGatedSearchAsync();

        ViewModel.RouteDistanceOptions.Single(option => option.RadiusKm == 5).SelectCommand.Execute(null);
        gate.SetResult();
        await LastSearch;

        Assert.Equal(2, RouteSearch.Requests.Single().DistanceKm);
        Assert.Equal(2, ViewModel.RouteDistanceKm);
        Assert.Equal([2], ViewModel.RouteDistanceOptions.Where(option => option.IsSelected).Select(option => option.RadiusKm));
        Assert.Contains("im Abstand von 2 km zur Route", ViewModel.RouteSummary, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass der Abstand der Anfrage auch dann in der Zusammenfassung steht, wenn der Abstand nach dem Ergebnis geändert wird (die Zusammenfassung gehört zum Ergebnis).
    /// </summary>
    [Fact]
    public async Task DistanceChangeAfterResult_KeepsSummaryOfTheRequest()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("Berlin-Mitte", "Oranienburg, Brandenburg");
        await SearchAsync();

        ViewModel.RouteDistanceOptions.Single(option => option.RadiusKm == 10).SelectCommand.Execute(null);

        Assert.Equal(10, ViewModel.RouteDistanceKm);
        Assert.Contains("im Abstand von 2 km zur Route", ViewModel.RouteSummary, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft (Abweichung 2), dass bei einem Start am aktuellen Standort der Name „Aktueller Standort“ erscheint, auch wenn die Startart während der Suche umgestellt werden soll.
    /// </summary>
    [Fact]
    public async Task StartKindChangeDuringSearch_IsRejectedAndTextsUseRequestStart()
    {
        Location.Result = new LocationResult(LocationStatus.Available, new GeoPosition(52.52, 13.405));
        await PrepareRouteAsync(startAddress: false);
        ViewModel.StartInput = "10115";
        var gate = new TaskCompletionSource();
        RouteSearch.Gate = gate.Task;
        RouteSearch.Result = RouteSuccess("Berlin-Mitte", "Oranienburg, Brandenburg", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        var search = ViewModel.SearchAsync();
        await WaitUntilAsync(() => RouteSearch.Requests.Count == 1);

        ViewModel.RouteStartOptions.Single(option => option.Value == RouteStartKind.Address).SelectCommand.Execute(null);
        gate.SetResult();
        await search;

        Assert.NotNull(RouteSearch.Requests.Single().StartPosition);
        Assert.Equal(RouteStartKind.CurrentLocation, ViewModel.RouteStartKind);
        Assert.True(ViewModel.RouteStartOptions[0].IsSelected);
        Assert.StartsWith("Von Aktueller Standort nach Oranienburg, Brandenburg", ViewModel.RouteSummary, StringComparison.Ordinal);
        Assert.Contains("Route von Aktueller Standort nach Oranienburg, Brandenburg eingezeichnet", ViewModel.MapHintText, StringComparison.Ordinal);
        Assert.Equal(MapOriginKind.CurrentLocation, ViewModel.Origin!.Kind);
    }

    /// <summary>
    /// Prüft, dass nach dem Ergebnis ein Umstellen der Startart oder der Eingaben den Kartenhinweis des Ergebnisses nicht verändert.
    /// </summary>
    [Fact]
    public async Task ChangesAfterResult_DoNotRewriteMapHint()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess(null, null, RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();
        var hint = ViewModel.MapHintText;
        var summary = ViewModel.RouteSummary;
        Assert.Contains(StartText, hint, StringComparison.Ordinal);
        Assert.Contains(DestinationText, hint, StringComparison.Ordinal);

        ViewModel.StartInput = "Hamburg";
        ViewModel.DestinationInput = "Kiel";
        ViewModel.RouteStartOptions.Single(option => option.Value == RouteStartKind.CurrentLocation).SelectCommand.Execute(null);

        Assert.Equal(hint, ViewModel.MapHintText);
        Assert.Equal(summary, ViewModel.RouteSummary);
    }

    /// <summary>
    /// Prüft, dass während einer laufenden Routensuche Chips und Eingaben gesperrt sind und danach wieder bedienbar werden.
    /// </summary>
    [Fact]
    public async Task InputsAreLockedWhileRouteSearchRuns()
    {
        Assert.True(ViewModel.RouteInputsEnabled);
        Assert.All(ViewModel.RouteStartOptions, option => Assert.True(option.IsEnabled));
        Assert.All(ViewModel.RouteDistanceOptions, option => Assert.True(option.IsEnabled));

        var gate = await StartGatedSearchAsync();

        Assert.True(ViewModel.IsRouteSearching);
        Assert.False(ViewModel.RouteInputsEnabled);
        Assert.All(ViewModel.RouteStartOptions, option => Assert.False(option.IsEnabled));
        Assert.All(ViewModel.RouteDistanceOptions, option => Assert.False(option.IsEnabled));

        ViewModel.ClearStartCommand.Execute(null);
        ViewModel.ClearDestinationCommand.Execute(null);
        Assert.Equal(StartText, ViewModel.StartInput);
        Assert.Equal(DestinationText, ViewModel.DestinationInput);

        gate.SetResult();
        await LastSearch;

        Assert.False(ViewModel.IsRouteSearching);
        Assert.True(ViewModel.RouteInputsEnabled);
        Assert.All(ViewModel.RouteStartOptions, option => Assert.True(option.IsEnabled));
        Assert.All(ViewModel.RouteDistanceOptions, option => Assert.True(option.IsEnabled));
        ViewModel.ClearDestinationCommand.Execute(null);
        Assert.Equal(string.Empty, ViewModel.DestinationInput);
    }

    /// <summary>
    /// Prüft, dass die Sperre nur für die Routensuche gilt: Bei der Umkreissuche sind die Routen-Chips nicht betroffen.
    /// </summary>
    [Fact]
    public async Task LockDoesNotApplyToOtherModes()
    {
        await AppearAsync();
        var gate = new TaskCompletionSource();
        Location.Gate = gate.Task;

        var search = ViewModel.SearchAsync();

        Assert.True(ViewModel.IsBusy);
        Assert.False(ViewModel.IsRouteSearching);
        Assert.True(ViewModel.RouteInputsEnabled);
        gate.SetResult();
        await search;
    }

    /// <summary>
    /// Prüft das Abbrechen: Die Suche endet ohne Ergebnis, der Fortschritt verschwindet, „Suchen“ ist frei und ein Hinweis meldet den Abbruch.
    /// </summary>
    [Fact]
    public async Task CancelCommand_StopsRunningRouteSearch()
    {
        var gate = await StartGatedSearchAsync();
        RouteSearch.ProgressToReport.Clear();

        ViewModel.CancelRouteSearchCommand.Execute(null);
        await LastSearch;

        Assert.False(ViewModel.IsBusy);
        Assert.False(ViewModel.IsRouteSearching);
        Assert.True(ViewModel.IsNotBusy);
        Assert.Null(ViewModel.RouteProgressText);
        Assert.Equal(SearchTexts.RouteCancelled, ViewModel.StatusMessage);
        Assert.Empty(ViewModel.Stations);
        Assert.False(ViewModel.HasMapRoute);
        Assert.True(ViewModel.RouteInputsEnabled);
        gate.TrySetResult();
    }

    /// <summary>
    /// Prüft, dass der Abbruchbefehl ohne laufende Routensuche nichts bewirkt.
    /// </summary>
    [Fact]
    public async Task CancelCommand_WithoutRunningSearch_DoesNothing()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();

        ViewModel.CancelRouteSearchCommand.Execute(null);

        Assert.Single(ViewModel.Stations);
        Assert.Null(ViewModel.StatusMessage);
    }
}
