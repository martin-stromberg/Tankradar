using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Suchart „Route“ nur bei aktivierter Routensuche angeboten wird und ein Wechsel des Schalters ohne Neustart wirkt (auch bei offener Seite).
/// </summary>
public class MapViewModelTests_RouteAvailability : MapViewModelRouteTestBase
{
    private static readonly SearchMode[] WithoutRoute = [SearchMode.CurrentLocation, SearchMode.Address];
    private static readonly SearchMode[] WithRoute = [SearchMode.CurrentLocation, SearchMode.Address, SearchMode.Route];

    private IEnumerable<SearchMode> Modes()
    {
        return ViewModel.ModeOptions.Select(option => option.Value);
    }

    /// <summary>
    /// Prüft, dass vor dem Erscheinen der Seite (Zustand unbekannt) und bei ausgeschalteter Routensuche kein Modus „Route“ angeboten wird.
    /// </summary>
    [Fact]
    public async Task ModeOptions_RouteSearchOff_HidesRouteMode()
    {
        Assert.Equal(WithoutRoute, Modes());

        RouteAccess.Switched = false;
        await AppearAsync();

        Assert.Equal(WithoutRoute, Modes());
        Assert.False(ViewModel.IsRouteMode);
    }

    /// <summary>
    /// Prüft, dass ein Schalter ohne Schlüssel (z. B. nach einer Wiederherstellung ohne Schlüssel) den Modus „Route“ nicht freischaltet.
    /// </summary>
    [Fact]
    public async Task ModeOptions_SwitchOnButNoKey_HidesRouteMode()
    {
        RouteAccess.Key = null;
        RouteAccess.Switched = true;

        await AppearAsync();

        Assert.Equal(WithoutRoute, Modes());
    }

    /// <summary>
    /// Prüft, dass bei aktivierter Routensuche der Modus „Route“ nach dem Erscheinen der Seite angeboten wird.
    /// </summary>
    [Fact]
    public async Task ModeOptions_RouteSearchOn_ShowsRouteMode()
    {
        await AppearAsync();

        Assert.Equal(WithRoute, Modes());
        Assert.Equal(SearchTexts.ModeRoute, ViewModel.ModeOptions[2].Label);
    }

    /// <summary>
    /// Prüft, dass das Aktivieren bei offener Seite den Modus sofort einblendet (die Änderung der Liste wird gemeldet) und das Deaktivieren ihn wieder entfernt.
    /// </summary>
    [Fact]
    public async Task SwitchChangedWhilePageIsOpen_UpdatesModeOptions()
    {
        RouteAccess.Switched = false;
        await AppearAsync();
        var notifications = new List<string?>();
        ViewModel.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        RouteAccess.Switched = true;
        RouteAccess.RaiseChanged();
        await WaitUntilAsync(() => Modes().SequenceEqual(WithRoute));

        Assert.Contains(nameof(ViewModel.ModeOptions), notifications);

        RouteAccess.Switched = false;
        RouteAccess.RaiseChanged();
        await WaitUntilAsync(() => Modes().SequenceEqual(WithoutRoute));
    }

    /// <summary>
    /// Prüft, dass ein Wechsel des Schalters, während die Seite verdeckt war, beim nächsten Erscheinen wirkt, ohne dass ein Ereignis nötig ist.
    /// </summary>
    [Fact]
    public async Task SwitchChangedWhilePageIsHidden_TakesEffectOnAppearing()
    {
        RouteAccess.Switched = false;
        await AppearAsync();
        ViewModel.OnDisappearing();

        RouteAccess.Switched = true;
        RouteAccess.RaiseChanged();
        Assert.Equal(WithoutRoute, Modes());

        await AppearAsync();

        Assert.Equal(WithRoute, Modes());
    }

    /// <summary>
    /// Prüft, dass die Seite das Änderungsereignis nur einmal abonniert und beim Verlassen abmeldet.
    /// </summary>
    [Fact]
    public async Task ChangedEvent_IsSubscribedOnceAndReleasedOnDisappearing()
    {
        await AppearAsync();
        await AppearAsync();

        Assert.Equal(1, RouteAccess.SubscriberCount);

        ViewModel.OnDisappearing();

        Assert.Equal(0, RouteAccess.SubscriberCount);
    }

    /// <summary>
    /// Prüft, dass das Ausschalten im Modus „Route“ zurück auf den Standardmodus führt, das Ergebnis verwirft und die Radiuswahl wieder zeigt.
    /// </summary>
    [Fact]
    public async Task SwitchedOffWhileRouteModeActive_FallsBackToDefaultModeAndClearsResult()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();
        Assert.True(ViewModel.HasMapRoute);

        RouteAccess.Switched = false;
        RouteAccess.RaiseChanged();
        await WaitUntilAsync(() => !ViewModel.IsRouteMode);

        Assert.False(ViewModel.IsRouteMode);
        Assert.True(ViewModel.IsRadiusVisible);
        Assert.True(ViewModel.ModeOptions[0].IsSelected);
        Assert.Equal(WithoutRoute, Modes());
        Assert.Empty(ViewModel.Stations);
        Assert.False(ViewModel.HasMapRoute);
        Assert.Null(ViewModel.RouteSummary);
        Assert.Null(ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass das Entfernen des Schlüssels während einer laufenden Routensuche diese abbricht und „Suchen“ freigibt.
    /// </summary>
    [Fact]
    public async Task KeyRemovedDuringRunningRouteSearch_CancelsItAndReleasesSearch()
    {
        await PrepareRouteAsync();
        var gate = new TaskCompletionSource();
        RouteSearch.Gate = gate.Task;
        RouteSearch.ProgressToReport.Add(new RouteSearchProgress(RouteSearchPhase.SearchingStations, 1, 3));
        RouteSearch.Result = RouteSuccess("A", "B");
        var search = ViewModel.SearchAsync();
        await WaitUntilAsync(() => ViewModel.HasRouteProgress);

        await RouteAccess.RemoveKeyAsync();
        await WaitUntilAsync(() => !ViewModel.IsRouteMode);
        await search;

        Assert.False(ViewModel.IsBusy);
        Assert.Null(ViewModel.RouteProgressText);
        Assert.False(ViewModel.IsRouteMode);
        Assert.Equal(WithoutRoute, Modes());
        Assert.Empty(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Lesen des Zustands die Routensuche ausblendet (Fail Secure) und nicht abstürzt.
    /// </summary>
    [Fact]
    public async Task ReadFailure_HidesRouteModeAndLogsOnlyType()
    {
        RouteAccess.ThrowOnRead = true;

        await AppearAsync();

        Assert.Equal(WithoutRoute, Modes());
        Assert.DoesNotContain("Ablage nicht lesbar", Logger.AllText, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", Logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass ohne aktivierte Routensuche eine Routensuche, die trotzdem angestoßen wird, den Dienst nicht erreicht,
    /// weil der Modus gar nicht gewählt werden kann (Umkreissuche läuft weiter ohne Routendienst).
    /// </summary>
    [Fact]
    public async Task RouteSearchOff_SearchUsesOnlyPriceServiceAndNeverRouteService()
    {
        RouteAccess.Switched = false;
        await AppearAsync();
        Prices.Result = Result(MAUI.Models.Pricing.PriceDataSource.Live, MAUI.Models.Pricing.PriceFailure.None, TwoStations());

        await SearchAsync();

        Assert.Empty(RouteSearch.Requests);
        Assert.Single(Prices.Queries);
    }
}
