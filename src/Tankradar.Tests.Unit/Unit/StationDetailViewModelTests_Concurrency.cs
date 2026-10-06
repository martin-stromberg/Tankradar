using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass ein neuer Abruf einen laufenden ablöst, ohne dass der abgelöste Lauf fehlschlägt oder seine Daten anzeigt.
/// </summary>
public class StationDetailViewModelTests_Concurrency : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass eine Aktualisierung während eines hängenden Abrufs diesen ablöst und der abgelöste Lauf ohne Ausnahme endet.
    /// </summary>
    [Fact]
    public async Task Refresh_WhileLoading_SupersedesFirstLoadWithoutFault()
    {
        var gate = new TaskCompletionSource();
        Prices.DetailGate = gate.Task;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        ViewModel.OnAppearing();
        var first = ViewModel.LastLoadTask;

        Prices.DetailGate = null;
        ViewModel.RefreshCommand.Execute(null);
        var second = ViewModel.LastLoadTask;
        await second;
        gate.SetResult();
        await first;

        Assert.False(first.IsFaulted);
        Assert.False(second.IsFaulted);
        Assert.False(ViewModel.IsBusy);
        Assert.Equal(3, ViewModel.Detail!.PriceLines.Count);
    }

    /// <summary>
    /// Prüft, dass ein Dienst, der den Abbruch ignoriert und nach dem Verlassen der Seite normal zurückkehrt, keine Ausnahme und keine Anzeige bewirkt.
    /// </summary>
    [Fact]
    public async Task LateResult_AfterDisappearing_IsDiscardedWithoutFault()
    {
        var gate = new TaskCompletionSource();
        Prices.DetailGate = gate.Task;
        Prices.IgnoreCancellation = true;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        ViewModel.OnAppearing();
        var load = ViewModel.LastLoadTask;

        ViewModel.OnDisappearing();
        gate.SetResult();
        await load;

        Assert.False(load.IsFaulted);
        Assert.False(ViewModel.IsBusy);
        Assert.Single(ViewModel.Detail!.PriceLines);
    }
}
