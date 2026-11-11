using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Startsuche beim erstmaligen Anzeigen der Karte im <see cref="MAUI.ViewModels.MapViewModel"/>: Umkreissuche am aktuellen Standort
/// mit Standardradius, Erhalt des Ergebnisses bei erneutem Erscheinen, Wiederholung nach Abbruch, Abbruch des Vorlaufs (Einstellungen laden)
/// und stilles Überspringen bei „Nie“.
/// </summary>
public class MapViewModelTests_AutoLoad : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass das erste Erscheinen eine Umkreissuche am aktuellen Standort mit dem Standardradius auslöst und das Ergebnis anzeigt.
    /// </summary>
    [Fact]
    public async Task FirstAppearing_SearchesAtCurrentLocationWithDefaultRadius()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());

        await AppearAndAutoSearchAsync();

        Assert.Equal([GpsUsage.WhileInUse], Location.Calls);
        var query = Assert.Single(Prices.Queries);
        Assert.Equal(5, query.RadiusKm);
        Assert.Equal(StationFactory.CenterLatitude, query.Latitude);
        Assert.Equal(StationFactory.CenterLongitude, query.Longitude);
        Assert.Equal(MapOriginKind.CurrentLocation, ViewModel.Origin?.Kind);
        Assert.Equal(MapFitMode.FitToResult, ViewModel.MapFitMode);
        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Equal(2, ViewModel.MapMarkers.Count);
    }

    /// <summary>
    /// Prüft, dass ein erneutes Erscheinen das Ergebnis samt Filter und Sortierung unverändert lässt und nichts erneut abfragt.
    /// </summary>
    [Fact]
    public async Task Reappearing_KeepsResultWithoutNewQueries()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAndAutoSearchAsync();
        ViewModel.FuelFilterOptions.Single(option => option.FuelType == FuelType.Diesel).SelectCommand.Execute(null);
        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Name).SelectCommand.Execute(null);
        var origin = ViewModel.Origin;
        var markerCount = ViewModel.MapMarkers.Count;
        var stationNames = ViewModel.Stations.Select(station => station.Name).ToList();

        ViewModel.OnDisappearing();
        await AppearAndAutoSearchAsync();

        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.Same(origin, ViewModel.Origin);
        Assert.Equal(markerCount, ViewModel.MapMarkers.Count);
        Assert.Equal(stationNames, ViewModel.Stations.Select(station => station.Name));
        Assert.True(ViewModel.FuelFilterOptions.Single(option => option.FuelType == FuelType.Diesel).IsSelected);
        Assert.True(ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Name).IsSelected);
    }

    /// <summary>
    /// Prüft, dass ein leeres Ergebnis als ausgelöst gilt und ein erneutes Erscheinen die Startsuche nicht wiederholt.
    /// </summary>
    [Fact]
    public async Task Reappearing_AfterEmptyResult_DoesNotRepeat()
    {
        await AppearAndAutoSearchAsync();
        Assert.True(ViewModel.ShowEmptyState);

        ViewModel.OnDisappearing();
        await AppearAndAutoSearchAsync();

        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.True(ViewModel.ShowEmptyState);
    }

    /// <summary>
    /// Prüft, dass eine fehlgeschlagene Startsuche (Dienstfehler oder Standortmeldung) als ausgelöst gilt und nicht wiederholt wird.
    /// </summary>
    /// <param name="failAtLocation">Gibt an, ob der Standort statt des Preisdienstes fehlschlägt.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reappearing_AfterFailureMessage_DoesNotRepeat(bool failAtLocation)
    {
        if (failAtLocation)
        {
            Location.Result = LocationResult.Failure(LocationStatus.PermissionDenied);
        }
        else
        {
            Prices.SearchException = new InvalidOperationException();
        }

        await AppearAndAutoSearchAsync();
        Assert.True(ViewModel.HasStatusMessage);
        var locationCalls = Location.Calls.Count;
        var priceQueries = Prices.Queries.Count;

        ViewModel.OnDisappearing();
        await AppearAndAutoSearchAsync();

        Assert.Equal(locationCalls, Location.Calls.Count);
        Assert.Equal(priceQueries, Prices.Queries.Count);
        Assert.True(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Verlassen der Seite während der laufenden Startsuche diese abbricht und das nächste Erscheinen sie erneut startet.
    /// </summary>
    [Fact]
    public async Task DisappearingDuringAutoSearch_ReappearingRetries()
    {
        var gate = new TaskCompletionSource();
        Location.Gate = gate.Task;
        ViewModel.OnAppearing();
        var autoSearch = ViewModel.LastSearchTask;
        Assert.Single(Location.Calls);
        Assert.True(ViewModel.IsBusy);

        ViewModel.OnDisappearing();
        await autoSearch;
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.HasStatusMessage);
        Assert.Empty(ViewModel.Stations);

        Location.Gate = null;
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAndAutoSearchAsync();

        Assert.Equal(2, Location.Calls.Count);
        Assert.Single(Prices.Queries);
        Assert.Equal(2, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass bei der Standortnutzung „Nie“ die Startsuche still unterbleibt: keine Abfrage, keine Meldung, kein Leerzustand.
    /// </summary>
    [Fact]
    public async Task GpsNever_SkipsAutoSearchSilently()
    {
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };

        await AppearAndAutoSearchAsync();

        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.HasStatusMessage);
        Assert.False(ViewModel.ShowEmptyState);
        Assert.Empty(ViewModel.Stations);
        Assert.Null(ViewModel.Origin);
    }

    /// <summary>
    /// Prüft, dass nach einem Wechsel der Standortnutzung von „Nie“ auf „Bei Nutzung“ das nächste Erscheinen die Startsuche nachholt.
    /// </summary>
    [Fact]
    public async Task GpsNever_EnabledLater_AutoSearchRunsOnReappear()
    {
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };
        await AppearAndAutoSearchAsync();
        Assert.Empty(Location.Calls);

        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.WhileInUse };
        ViewModel.OnDisappearing();
        await AppearAndAutoSearchAsync();

        Assert.Equal([GpsUsage.WhileInUse], Location.Calls);
        Assert.Single(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein Wechsel der Standortnutzung auf „Nie“ die Startsuche auch dann still unterbleiben lässt, wenn das Einstellungsladen
    /// beim erneuten Erscheinen noch läuft: Die Prüfung wartet den frischen Lauf ab statt den veralteten Zwischenstand zu lesen.
    /// </summary>
    [Fact]
    public async Task GpsDisabledBeforeReappearWhileSettingsLoadPending_SkipsAutoSearchSilently()
    {
        // Erstes Erscheinen: Die Einstellungen („Bei Nutzung“) sind geladen; die Startsuche hängt an der Ortung und wird per Verlassen abgebrochen.
        var locationGate = new TaskCompletionSource();
        Location.Gate = locationGate.Task;
        ViewModel.OnAppearing();
        var firstSearch = ViewModel.LastSearchTask;
        Assert.Single(Location.Calls);
        ViewModel.OnDisappearing();
        await firstSearch;
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.HasStatusMessage);

        // Der Anwender stellt die Ortung auf „Nie“; beim nächsten Erscheinen läuft das Einstellungsladen noch (asynchron).
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };
        var loadGate = new TaskCompletionSource();
        Settings.LoadGate = loadGate.Task;
        Location.Gate = null;
        Location.Calls.Clear();

        ViewModel.OnAppearing();
        var retry = ViewModel.LastSearchTask;
        loadGate.SetResult();
        await retry;

        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.HasStatusMessage);
        Assert.Empty(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass ein Wechsel der Standortnutzung von „Nie“ auf „Bei Nutzung“ beim erneuten Erscheinen mit noch laufendem
    /// Einstellungsladen sofort nachgeholt wird: Der Vorlauf liest nicht den veralteten Zwischenstand „Nie“ und beendet sich still.
    /// </summary>
    [Fact]
    public async Task GpsEnabledBeforeReappearWhileSettingsLoadPending_AutoSearchRuns()
    {
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };
        await AppearAndAutoSearchAsync();
        Assert.Empty(Location.Calls);

        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.WhileInUse };
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var loadGate = new TaskCompletionSource();
        Settings.LoadGate = loadGate.Task;
        ViewModel.OnDisappearing();
        ViewModel.OnAppearing();
        var retry = ViewModel.LastSearchTask;
        loadGate.SetResult();
        await retry;

        Assert.Equal([GpsUsage.WhileInUse], Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.Equal(2, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass ein Erscheinen im Adressmodus keine Startsuche auslöst.
    /// </summary>
    [Fact]
    public async Task AppearingInAddressMode_DoesNotAutoSearch()
    {
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);

        await AppearAndAutoSearchAsync();

        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.Empty(Geocoding.Inputs);
    }

    /// <summary>
    /// Prüft, dass ein Verlassen der Seite während des Einstellungsladens den Vorlauf der Startsuche abbricht (keine Suche, keine Meldung)
    /// und das nächste Erscheinen die Startsuche nachholt, weil der Abbruch nicht als „erfolgt“ gilt.
    /// </summary>
    [Fact]
    public async Task DisappearingDuringSettingsLoad_AbortsPreludeAndRetriesOnReappear()
    {
        var gate = new TaskCompletionSource();
        Settings.LoadGate = gate.Task;
        ViewModel.OnAppearing();
        var autoSearch = ViewModel.LastSearchTask;

        ViewModel.OnDisappearing();
        gate.SetResult();
        await autoSearch;

        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.HasStatusMessage);
        Assert.Empty(ViewModel.Stations);

        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAndAutoSearchAsync();

        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.Equal(2, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass ein erneutes Erscheinen, während der durch das Verlassen abgebrochene Vorlauf noch am Laden der Einstellungen hängt,
    /// die Startsuche mit einem neuen Lauf nachholt: der abgebrochene Lauf führt keine Suche mehr aus, das Laden wird nicht doppelt ausgelöst.
    /// </summary>
    [Fact]
    public async Task ReappearingDuringSettingsLoad_RestartsAbortedAutoSearch()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var gate = new TaskCompletionSource();
        Settings.LoadGate = gate.Task;
        ViewModel.OnAppearing();
        var autoSearch = ViewModel.LastSearchTask;

        ViewModel.OnDisappearing();
        ViewModel.OnAppearing();
        var retry = ViewModel.LastSearchTask;

        Assert.NotSame(autoSearch, retry);
        Assert.Equal(1, Settings.LoadCount);
        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);

        gate.SetResult();
        await autoSearch;
        await retry;

        Assert.Single(Location.Calls);
        var query = Assert.Single(Prices.Queries);
        Assert.Equal(5, query.RadiusKm);
        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein Wechsel der Suchart während des Einstellungsladens die Startsuche verwirft: keine Suche im neuen Modus,
    /// keine Meldung ohne Nutzeraktion; zurück im Standardmodus holt das nächste Erscheinen die Startsuche nach.
    /// </summary>
    [Fact]
    public async Task ModeSwitchDuringSettingsLoad_SkipsAutoSearchAndRetriesOnReappear()
    {
        var gate = new TaskCompletionSource();
        Settings.LoadGate = gate.Task;
        ViewModel.OnAppearing();
        var autoSearch = ViewModel.LastSearchTask;

        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        gate.SetResult();
        await autoSearch;

        Assert.True(ViewModel.IsAddressMode);
        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
        Assert.Empty(Geocoding.Inputs);
        Assert.False(ViewModel.HasStatusMessage);

        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.CurrentLocation).SelectCommand.Execute(null);
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAndAutoSearchAsync();

        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.Equal(2, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass eine per Suchbefehl gestartete Suche während des Einstellungsladens nicht von der Startsuche verdrängt wird.
    /// </summary>
    [Fact]
    public async Task ManualSearchDuringSettingsLoad_IsNotReplacedByAutoSearch()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var gate = new TaskCompletionSource();
        Settings.LoadGate = gate.Task;
        ViewModel.OnAppearing();
        var autoSearch = ViewModel.LastSearchTask;

        ViewModel.SearchCommand.Execute(null);
        var manualSearch = ViewModel.LastSearchTask;
        Assert.NotSame(autoSearch, manualSearch);

        gate.SetResult();
        await autoSearch;
        await manualSearch;

        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.Equal(2, ViewModel.Stations.Count);
    }
}
