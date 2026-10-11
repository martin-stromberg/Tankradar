using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Ablauf der Startseite: Laden, Aktualisieren, Standort nur bei Bedarf, Tankstelle öffnen, Fehlerfälle und Datenschutz.
/// </summary>
public class FavoritesViewModelTests_Home : FavoritesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass zuerst der lokale Stand erscheint und danach der Abruf die Preise aktualisiert und Entfernungen ergänzt.
    /// </summary>
    [Fact]
    public async Task Load_ShowsLocalDataThenRefreshes()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Arbeitsweg", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync();

        var local = Assert.Single(viewModel.Groups).Stations[0];
        Assert.Equal("Alpha Tankstelle", local.Name);
        Assert.Equal(["Super E5", "Diesel"], local.PriceLines.Select(l => l.FuelLabel));
        Assert.Equal("vor 5 Min.", local.PriceLines[0].AgeText);
        Assert.False(local.HasDistance);
        Assert.Empty(Prices.RefreshRequests);

        await viewModel.RefreshAsync(force: false);

        Assert.Equal([[FavoritesFixture.StationA]], Prices.RefreshRequests.Select(r => r.ToArray()));
        Assert.Equal("1,1 km", viewModel.Groups[0].Stations[0].DistanceText);
    }

    /// <summary>
    /// Prüft, dass ohne Gruppen der Hinweis erscheint und weder Preise noch Standort abgefragt werden.
    /// </summary>
    [Fact]
    public async Task Load_WithoutGroups_ShowsHintAndDoesNotQueryPricesOrLocation()
    {
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.True(viewModel.ShowEmptyHint);
        Assert.Empty(Prices.RefreshRequests);
        Assert.Empty(Location.Calls);
        Assert.False(viewModel.Nearby.IsVisible);
    }

    /// <summary>
    /// Prüft, dass bei der Standortnutzung „Nie“ der Standortdienst nie aufgerufen wird und keine Entfernungen erscheinen.
    /// </summary>
    [Fact]
    public async Task Load_GpsNever_NeverCallsLocationService()
    {
        Settings.Stored = AppSettings.CreateDefault() with { GpsUsage = GpsUsage.Never, ShowNearby = true };
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Empty(Location.Calls);
        Assert.False(viewModel.Groups[0].Stations[0].HasDistance);
        Assert.False(viewModel.Nearby.IsVisible);
        Assert.Single(Prices.RefreshRequests);
    }

    /// <summary>
    /// Prüft, dass bei erlaubter Standortnutzung genau einmal je Abruf der Standort ermittelt und die Entfernung angezeigt wird.
    /// </summary>
    [Fact]
    public async Task Load_GpsAllowed_ShowsDistances()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA, FavoritesFixture.StationB);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal([GpsUsage.WhileInUse], Location.Calls);
        Assert.Equal(["1,1 km", "2,2 km"], viewModel.Groups[0].Stations.Select(s => s.DistanceText));
    }

    /// <summary>
    /// Prüft, dass ein nicht verfügbarer Standort ohne Fehlermeldung zu Anzeige ohne Entfernungen führt.
    /// </summary>
    [Fact]
    public async Task Load_LocationUnavailable_ShowsNoDistances()
    {
        Location.Result = LocationResult.Failure(LocationStatus.PermissionDenied);
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.False(viewModel.Groups[0].Stations[0].HasDistance);
        Assert.False(viewModel.HasStatusMessage);
        Assert.True(viewModel.Groups[0].Stations[0].HasPrices);
    }

    /// <summary>
    /// Prüft, dass die Schaltfläche „Preise aktualisieren“ den Abruf erzwingt, das Beim-Erscheinen-Abrufen aber nicht.
    /// </summary>
    [Fact]
    public async Task Refresh_Command_ForcesRequest()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await viewModel.LastTask;

        viewModel.RefreshCommand.Execute(null);
        await viewModel.LastTask;

        Assert.Equal([false, true], Prices.RefreshForces);
        Assert.True(viewModel.CanRefresh);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung die Schaltfläche nicht bedienbar ist und kein Abruf startet.
    /// </summary>
    [Fact]
    public async Task Refresh_NotRunningWhenOffline()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        Connection.IsOnline = false;
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();

        viewModel.RefreshCommand.Execute(null);
        await viewModel.LastTask;

        Assert.False(viewModel.CanRefresh);
        Assert.Empty(Prices.RefreshRequests);
    }

    /// <summary>
    /// Prüft, dass das Verlassen der Seite einen laufenden Abruf abbricht und der Standort verworfen wird.
    /// </summary>
    [Fact]
    public async Task Disappearing_CancelsAndDropsPosition()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var gate = new TaskCompletionSource();
        Prices.RefreshGate = gate.Task;
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await Prices.RefreshEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));

        viewModel.OnDisappearing();
        await viewModel.LastTask;
        Prices.RefreshGate = null;
        await viewModel.LoadAsync();

        Assert.False(viewModel.IsBusy);
        Assert.False(viewModel.Groups[0].Stations[0].HasDistance);
        Assert.Single(Location.Calls);
    }

    /// <summary>
    /// Prüft, dass Änderungen der Favoriten die Anzeige aus dem lokalen Stand neu aufbauen, ohne erneut beim Preisdienst abzurufen.
    /// </summary>
    [Fact]
    public async Task FavoritesChanged_ReloadsWithoutNewRequest()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        viewModel.OnAppearing();
        await viewModel.LastTask;

        await Fixture.Service.CreateGroupAsync("Urlaub", null);
        await viewModel.LastTask;

        Assert.Equal(["Heimat", "Urlaub"], viewModel.Groups.Select(g => g.Name));
        Assert.Single(Prices.RefreshRequests);
    }

    /// <summary>
    /// Prüft, dass das Öffnen einer Tankstelle die Detailansicht mit Kennung und Namen öffnet und doppeltes Antippen nur einmal öffnet.
    /// </summary>
    [Fact]
    public async Task OpenStation_NavigatesWithStationAndGuardsDoubleOpen()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        var gate = new TaskCompletionSource();
        StationNavigator.Gate = gate.Task;

        viewModel.Groups[0].Stations[0].OpenCommand.Execute(null);
        viewModel.Groups[0].Stations[0].OpenCommand.Execute(null);
        gate.SetResult();
        await viewModel.LastTask;

        var opened = Assert.Single(StationNavigator.Opened);
        Assert.Equal(FavoritesFixture.StationA, opened.Id);
        Assert.Equal("Alpha Tankstelle", opened.Name);
    }

    /// <summary>
    /// Prüft, dass ein Fehler beim Laden der Einstellungen als Meldung erscheint und nichts protokolliert wird, was den Fehlertext verrät.
    /// </summary>
    [Fact]
    public async Task Load_StorageFailure_ShowsMessage()
    {
        Settings.LoadException = new InvalidOperationException("Pfad C:\\geheim");
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync();

        Assert.Equal(FavoritesTexts.LoadFailed, viewModel.StatusMessage);
        Assert.DoesNotContain(Logger.Messages, message => message.Contains("geheim", StringComparison.Ordinal));
    }

    /// <summary>
    /// Prüft, dass ein fehlgeschlagener Abruf eine Meldung zeigt, die zuletzt bekannten Preise aber stehen bleiben.
    /// </summary>
    [Fact]
    public async Task Refresh_Failure_ShowsMessageAndKeepsLastKnown()
    {
        var stations = SeedPrices(FreshTime);
        Prices.RefreshResult = new StationRefreshResult(stations, PriceDataSource.OfflineFallback, PriceFailure.Unreachable);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.Equal("Der Preisdienst ist nicht erreichbar.", viewModel.StatusMessage);
        Assert.True(viewModel.Groups[0].Stations[0].HasPrices);
        Assert.True(viewModel.IsOffline);
    }

    /// <summary>
    /// Prüft, dass Koordinaten weder bei einem Fehler noch sonst in Protokolle gelangen.
    /// </summary>
    [Fact]
    public async Task Position_IsNeverLogged()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Heimat", FavoritesFixture.StationA);
        Prices.RefreshException = new InvalidOperationException("52,52 13,405");
        var viewModel = CreateViewModel();

        viewModel.OnAppearing();
        await viewModel.LastTask;

        Assert.NotEmpty(Logger.Messages);
        Assert.All(Logger.Messages, message =>
        {
            Assert.DoesNotContain("52,5", message, StringComparison.Ordinal);
            Assert.DoesNotContain("52.5", message, StringComparison.Ordinal);
            Assert.DoesNotContain("13,4", message, StringComparison.Ordinal);
            Assert.DoesNotContain("13.4", message, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// Prüft, dass die Beschreibung des Ein-/Ausklapp-Schalters Gruppenname und Zustand nennt und dem Zustand folgt.
    /// </summary>
    [Fact]
    public async Task Load_ToggleDescription_NamesGroupAndState()
    {
        SeedPrices(FreshTime);
        await CreateGroupAsync("Arbeitsweg", FavoritesFixture.StationA);
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        var group = Assert.Single(viewModel.Groups);

        Assert.Contains("Arbeitsweg", group.ToggleDescription, StringComparison.Ordinal);
        Assert.Contains("aufgeklappt", group.ToggleDescription, StringComparison.Ordinal);

        group.ToggleCommand.Execute(null);

        Assert.Contains("Arbeitsweg", group.ToggleDescription, StringComparison.Ordinal);
        Assert.Contains("eingeklappt", group.ToggleDescription, StringComparison.Ordinal);
    }
}
