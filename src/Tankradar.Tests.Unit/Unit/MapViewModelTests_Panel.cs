using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Trefferliste am unteren Rand des <see cref="MapViewModel"/>: Auf- und Zuklappen, Startzustand aus der Standardansicht (nicht gespeichert)
/// und „Weitere anzeigen“ nur bei ausgeklappter Liste.
/// </summary>
public class MapViewModelTests_Panel : MapViewModelTestBase
{
    private void TogglePanel()
    {
        ViewModel.ToggleBottomSheetCommand.Execute(null);
    }

    /// <summary>
    /// Prüft, dass der Befehl die Trefferliste abwechselnd ein- und ausklappt.
    /// </summary>
    [Fact]
    public async Task ToggleBottomSheet_ChangesExpandedState()
    {
        await AppearAsync();
        Assert.True(ViewModel.IsBottomSheetExpanded);

        TogglePanel();
        Assert.False(ViewModel.IsBottomSheetExpanded);

        TogglePanel();
        Assert.True(ViewModel.IsBottomSheetExpanded);
    }

    /// <summary>
    /// Prüft, dass die Standardansicht „Liste“ die Trefferliste ausgeklappt öffnet.
    /// </summary>
    [Fact]
    public async Task DefaultView_List_OpensPanelExpanded()
    {
        Settings.Stored = Settings.Stored with { ResultView = ResultView.List };

        await AppearAsync();

        Assert.True(ViewModel.IsBottomSheetExpanded);
        Assert.False(ViewModel.HasMapContent);
    }

    /// <summary>
    /// Prüft, dass die Standardansicht „Karte“ die Trefferliste eingeklappt öffnet und ohne Ergebnis weder Inhalt noch „Weitere anzeigen“ zeigt.
    /// </summary>
    [Fact]
    public async Task DefaultView_Map_OpensPanelCollapsed()
    {
        Settings.Stored = Settings.Stored with { ResultView = ResultView.Map };

        await AppearAsync();

        Assert.False(ViewModel.IsBottomSheetExpanded);
        Assert.False(ViewModel.ShowMoreVisible);
        Assert.Equal(0, ViewModel.TotalStationCount);
    }

    /// <summary>
    /// Prüft, dass „Weitere anzeigen“ nur bei ausgeklappter Liste erscheint; das Einklappen ändert die dargestellten Tankstellen nicht.
    /// </summary>
    [Fact]
    public async Task ShowMore_OnlyVisible_WhenPanelExpanded()
    {
        var stations = Enumerable.Range(1, MapViewModel.PageSize + 5)
            .Select(number => StationFactory.CreateCustom(number, "S" + number, number, Now, null, true, (FuelType.SuperE5, 1.70m)))
            .ToArray();
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, stations);
        await AppearAsync();
        await SearchAsync();
        Assert.True(ViewModel.HasMore);
        Assert.True(ViewModel.ShowMoreVisible);

        TogglePanel();

        Assert.False(ViewModel.ShowMoreVisible);
        Assert.True(ViewModel.HasMore);
        Assert.Equal(MapViewModel.PageSize, ViewModel.Stations.Count);

        TogglePanel();
        Assert.True(ViewModel.ShowMoreVisible);
    }

    /// <summary>
    /// Prüft, dass ein in der Sitzung gewählter Zustand der Trefferliste beim erneuten Erscheinen erhalten bleibt, bis sich die Standardansicht ändert.
    /// </summary>
    [Fact]
    public async Task SessionPanelState_SurvivesReappearingUntilDefaultChanges()
    {
        await AppearAsync();
        TogglePanel();

        await AppearAsync();
        Assert.False(ViewModel.IsBottomSheetExpanded);

        Settings.Stored = Settings.Stored with { ResultView = ResultView.Map };
        await AppearAsync();
        TogglePanel();
        Settings.Stored = Settings.Stored with { ResultView = ResultView.List };
        await AppearAsync();
        Assert.True(ViewModel.IsBottomSheetExpanded);
        Assert.Equal(ResultView.List, Settings.Stored.ResultView);
    }

    /// <summary>
    /// Prüft, dass Auf- und Zuklappen der Trefferliste keine neue Anfrage und keine neue Standortabfrage auslöst und die Ergebnisse erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task TogglePanel_DoesNotSearchAgain()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();

        TogglePanel();
        TogglePanel();

        Assert.Single(Prices.Queries);
        Assert.Single(Location.Calls);
        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Equal(2, ViewModel.MapMarkers.Count);
    }

    /// <summary>
    /// Prüft, dass die Kopfzeile ohne Ergebnis „Keine Ergebnisse“ zeigt.
    /// </summary>
    [Fact]
    public async Task StationCountText_ShowsNoResults_WithoutStations()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None);
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(SearchTexts.NoResults, ViewModel.StationCountText);
    }

    /// <summary>
    /// Prüft, dass die Kopfzeile bei einer Tankstelle die Einzahl verwendet.
    /// </summary>
    [Fact]
    public async Task StationCountText_ShowsSingular_ForOneStation()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, StationFactory.CreateCustom(1, "Alpha", 1, Now, null, true, (FuelType.SuperE5, 1.70m)));
        await AppearAsync();

        await SearchAsync();

        Assert.Equal("1 Tankstelle", ViewModel.StationCountText);
    }

    /// <summary>
    /// Prüft, dass die Kopfzeile bei mehreren Tankstellen die Gesamtzahl des Ergebnisses nennt (nicht nur die dargestellte Seite) und bei Änderung benachrichtigt.
    /// </summary>
    [Fact]
    public async Task StationCountText_ShowsTotalCount_ForManyStations()
    {
        var stations = Enumerable.Range(1, MapViewModel.PageSize + 5)
            .Select(number => StationFactory.CreateCustom(number, "S" + number, number, Now, null, true, (FuelType.SuperE5, 1.70m)))
            .ToArray();
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, stations);
        await AppearAsync();
        var changed = new List<string?>();
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await SearchAsync();

        Assert.Equal("30 Tankstellen", ViewModel.StationCountText);
        Assert.Contains(nameof(MapViewModel.StationCountText), changed);
    }
}
