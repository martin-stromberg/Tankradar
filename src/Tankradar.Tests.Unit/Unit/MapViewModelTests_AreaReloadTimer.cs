using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Zeitgeber der Nachladesperre im <see cref="MapViewModel"/>: Er hält beim Verlassen der Seite an, läuft beim erneuten Erscheinen mit der aktuellen Restzeit
/// weiter und reicht Ticks von einem fremden Thread über den beim Start erfassten <see cref="SynchronizationContext"/> weiter.
/// </summary>
public class MapViewModelTests_AreaReloadTimer : MapViewModelTestBase
{
    private async Task ReloadAndMoveAsync()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync();
        ViewModel.ReloadVisibleAreaCommand.Execute(null);
        await ViewModel.LastSearchTask;
        ViewModel.OnViewportChanged(viewport.PanByPixels(200, 0));
    }

    /// <summary>
    /// Prüft, dass der Zeitgeber auf der unsichtbaren Seite nicht weiter tickt: Der Hinweis bleibt nach dem Verlassen unverändert.
    /// </summary>
    [Fact]
    public async Task OnDisappearing_StopsTimer()
    {
        await ReloadAndMoveAsync();
        ViewModel.OnDisappearing();

        Clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(MapTexts.ReloadAreaWait(60), ViewModel.AreaReloadHint);
    }

    /// <summary>
    /// Prüft, dass das erneute Erscheinen während der Sperre die aktuelle Restzeit zeigt, der Zeitgeber weiterläuft und den Befehl nach Ablauf freigibt.
    /// </summary>
    [Fact]
    public async Task OnAppearing_DuringBlock_RestartsTimer()
    {
        await ReloadAndMoveAsync();
        ViewModel.OnDisappearing();
        Clock.Advance(TimeSpan.FromSeconds(30));

        await AppearAsync();

        Assert.Equal(MapTexts.ReloadAreaWait(30), ViewModel.AreaReloadHint);
        Assert.False(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));

        Clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(MapTexts.ReloadAreaWait(20), ViewModel.AreaReloadHint);

        Clock.Advance(TimeSpan.FromSeconds(20));
        Assert.Null(ViewModel.AreaReloadHint);
        Assert.True(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass das erneute Erscheinen nach Ablauf der Sperre den veralteten Hinweis entfernt und den Befehl freigibt.
    /// </summary>
    [Fact]
    public async Task OnAppearing_AfterBlock_ClearsHint()
    {
        await ReloadAndMoveAsync();
        ViewModel.OnDisappearing();
        Clock.Advance(MapViewModel.AreaReloadMinInterval);

        await AppearAsync();

        Assert.Null(ViewModel.AreaReloadHint);
        Assert.True(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass ein Tick von einem Hintergrund-Thread den Hinweis nicht direkt ändert, sondern über den beim Start des Zeitgebers erfassten Kontext weitergereicht wird.
    /// </summary>
    [Fact]
    public async Task TimerTick_FromBackgroundThread_IsPostedToCapturedContext()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync();
        var context = new QueueSynchronizationContext();
        Task? reload = null;
        context.Run(() =>
        {
            ViewModel.ReloadVisibleAreaCommand.Execute(null);
            reload = ViewModel.LastSearchTask;
        });
        while (reload is { IsCompleted: false })
        {
            await Task.Delay(10);
            context.RunAll();
        }

        await reload!;
        ViewModel.OnViewportChanged(viewport.PanByPixels(200, 0));
        Assert.Equal(MapTexts.ReloadAreaWait(60), ViewModel.AreaReloadHint);

        await Task.Run(() => Clock.Advance(TimeSpan.FromSeconds(30)));

        Assert.Equal(MapTexts.ReloadAreaWait(60), ViewModel.AreaReloadHint);
        Assert.True(context.PendingCount > 0);

        context.RunAll();

        Assert.Equal(MapTexts.ReloadAreaWait(30), ViewModel.AreaReloadHint);
    }
}
