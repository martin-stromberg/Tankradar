using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass abgelöste oder durch Verlassen der Seite abgebrochene Suchen das Ergebnis nicht verändern.
/// </summary>
public class MapViewModelTests_Concurrency : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass das Verlassen der Seite eine laufende Standortabfrage abbricht, ohne Preisabfrage, Fehlermeldung oder Ergebnis.
    /// </summary>
    [Fact]
    public async Task OnDisappearing_CancelsRunningSearch()
    {
        await AppearAsync();
        var gate = new TaskCompletionSource();
        Location.Gate = gate.Task;
        var search = ViewModel.SearchAsync();
        Assert.True(ViewModel.IsBusy);

        ViewModel.OnDisappearing();
        await search;

        Assert.False(ViewModel.IsBusy);
        Assert.Empty(Prices.Queries);
        Assert.Null(ViewModel.StatusMessage);
        Assert.Empty(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass eine abgelöste Suche das Ergebnis der neueren Suche nicht überschreibt und nicht den Ladezustand beendet.
    /// </summary>
    [Fact]
    public async Task SupersededSearch_DoesNotOverwriteNewerResult()
    {
        await AppearAsync();
        var gate = new TaskCompletionSource();
        Location.Gate = gate.Task;
        var first = ViewModel.SearchAsync();

        Location.Gate = null;
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await ViewModel.SearchAsync();
        gate.SetResult();
        await first;

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Null(ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
        Assert.Single(Prices.Queries);
    }
}
