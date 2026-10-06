using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass das <see cref="MAUI.ViewModels.MapViewModel"/> die Einstellungen übernimmt: Standardsortierung, Ansicht und Ladefehler.
/// </summary>
public class MapViewModelTests_Settings : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Standardsortierung aus den Einstellungen vorgewählt ist.
    /// </summary>
    /// <param name="order">Die gespeicherte Standardsortierung.</param>
    [Theory]
    [InlineData(ResultSortOrder.Price)]
    [InlineData(ResultSortOrder.Distance)]
    [InlineData(ResultSortOrder.Name)]
    public async Task DefaultSort_IsPreselectedFromSettings(ResultSortOrder order)
    {
        Settings.Stored = Settings.Stored with { ResultSortOrder = order };

        await AppearAsync();

        Assert.Equal(order, ViewModel.SortOptions.Single(option => option.IsSelected).Value);
    }

    /// <summary>
    /// Prüft, dass eine in der Sitzung gewählte Sortierung beim erneuten Erscheinen erhalten bleibt, solange sich die Standardeinstellung nicht ändert.
    /// </summary>
    [Fact]
    public async Task SessionSort_SurvivesReappearingUntilDefaultChanges()
    {
        await AppearAsync();
        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Name).IsSelected = true;

        await AppearAsync();
        Assert.Equal(ResultSortOrder.Name, ViewModel.SortOptions.Single(option => option.IsSelected).Value);

        Settings.Stored = Settings.Stored with { ResultSortOrder = ResultSortOrder.Distance };
        await AppearAsync();
        Assert.Equal(ResultSortOrder.Distance, ViewModel.SortOptions.Single(option => option.IsSelected).Value);
    }

    /// <summary>
    /// Prüft, dass bei Standardansicht „Karte“ die Tankstellen der Liste weiterhin aufbereitet werden (Wechsel auf die Liste ohne neue Suche).
    /// </summary>
    [Fact]
    public async Task ResultViewMap_StillPreparesListStations()
    {
        Settings.Stored = Settings.Stored with { ResultView = ResultView.Map };
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(2, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass ein Ladefehler der Einstellungen die Suche verhindert (fail secure: Standortnutzung unbekannt) und eine Meldung zeigt.
    /// </summary>
    [Fact]
    public async Task SettingsLoadFailure_BlocksSearchWithMessage()
    {
        Settings.LoadException = new InvalidOperationException("Datenbank kaputt");
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(SettingsTexts.LoadFailed, ViewModel.StatusMessage);
        Assert.Empty(Location.Calls);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass nach einem Ladefehler ein späterer Ladeerfolg die Meldung entfernt.
    /// </summary>
    [Fact]
    public async Task SettingsLoadSuccessAfterFailure_ClearsMessage()
    {
        Settings.LoadException = new InvalidOperationException("Datenbank kaputt");
        await AppearAsync();
        Settings.LoadException = null;

        await AppearAsync();

        Assert.Null(ViewModel.StatusMessage);
    }
}
