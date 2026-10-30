using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Sperre von <see cref="MapViewModel.AreaReloadMinInterval"/> nach einer Nachladeabfrage im <see cref="MapViewModel"/>: Der Befehl ist während der Sperre
/// inaktiv, der Hinweis nennt die laufend aktualisierte Restzeit, und nach Ablauf wird der Befehl über den Zeitgeber der Zeitquelle wieder freigegeben.
/// </summary>
public class MapViewModelTests_AreaReloadBlock : MapViewModelTestBase
{
    private void Reload()
    {
        ViewModel.ReloadVisibleAreaCommand.Execute(null);
    }

    private async Task ReloadAndMoveAsync()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        var viewport = await AppearAndViewportAsync();
        Reload();
        await ViewModel.LastSearchTask;
        ViewModel.OnViewportChanged(viewport.PanByPixels(200, 0));
    }

    /// <summary>
    /// Prüft, dass der Befehl direkt nach einer Nachladeabfrage inaktiv ist, der Hinweis die volle Wartezeit nennt und ein Auslösen nichts abfragt.
    /// </summary>
    [Fact]
    public async Task ReloadVisibleArea_DisablesCommand_DuringBlock()
    {
        await ReloadAndMoveAsync();

        Assert.True(ViewModel.ShowAreaReloadButton);
        Assert.False(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
        Assert.Equal(60, ViewModel.AreaReloadRemainingSeconds);
        Assert.Equal(MapTexts.ReloadAreaWait(60), ViewModel.AreaReloadHint);

        Reload();
        Assert.Equal(2, Prices.Queries.Count);
    }

    /// <summary>
    /// Prüft, dass der Zeitgeber die Restzeit im Hinweis aktualisiert, ohne dass der Ausschnitt bewegt wird, und der Befehl bis zum Ablauf inaktiv bleibt.
    /// </summary>
    [Fact]
    public async Task Timer_UpdatesRemainingTime_WhileBlocked()
    {
        await ReloadAndMoveAsync();

        Clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(30, ViewModel.AreaReloadRemainingSeconds);
        Assert.Equal("Der Bereich kann in 30 s erneut geladen werden.", ViewModel.AreaReloadHint);
        Assert.False(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
    }

    /// <summary>
    /// Prüft, dass der Befehl nach Ablauf der Sperre über den Zeitgeber wieder aktiv gemeldet wird, der Hinweis verschwindet und eine weitere Abfrage möglich ist.
    /// </summary>
    [Fact]
    public async Task Timer_EnablesCommand_AfterBlock()
    {
        await ReloadAndMoveAsync();
        var changed = 0;
        ViewModel.ReloadVisibleAreaCommand.CanExecuteChanged += (_, _) => changed++;

        Clock.Advance(MapViewModel.AreaReloadMinInterval);

        Assert.True(changed > 0);
        Assert.True(ViewModel.ReloadVisibleAreaCommand.CanExecute(null));
        Assert.Null(ViewModel.AreaReloadHint);
        Assert.Equal(0, ViewModel.AreaReloadRemainingSeconds);

        Reload();
        await ViewModel.LastSearchTask;
        Assert.Equal(3, Prices.Queries.Count);
    }
}
