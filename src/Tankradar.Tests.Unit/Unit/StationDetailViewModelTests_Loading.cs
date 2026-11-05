using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Laden der Detailansicht: Anfangsanzeige aus der Liste, Abruf der Details, Auswahl der Sorten und Fehlerfälle.
/// </summary>
public class StationDetailViewModelTests_Loading : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Navigationsparameter die Anfangsanzeige aus der Liste setzen, bevor etwas geladen wurde.
    /// </summary>
    [Fact]
    public void ApplyQueryAttributes_ShowsListDataImmediately()
    {
        ViewModel.ApplyQueryAttributes(new Dictionary<string, object> { [ShellStationNavigator.StationParameter] = CreateOrigin() });

        Assert.True(ViewModel.HasDetail);
        Assert.Equal("Alpha Tankstelle", ViewModel.Detail!.Name);
        Assert.Equal("1,8 km", ViewModel.Detail.DistanceText);
        Assert.Equal("1,850 €", Assert.Single(ViewModel.Detail.PriceLines).PriceText);
        Assert.Empty(Prices.DetailRequests);
    }

    /// <summary>
    /// Prüft, dass ohne Tankstelle in den Parametern nichts angezeigt und beim Erscheinen nichts abgefragt wird.
    /// </summary>
    [Fact]
    public async Task Appearing_WithoutStation_DoesNothing()
    {
        ViewModel.ApplyQueryAttributes(new Dictionary<string, object>());

        await AppearAsync();

        Assert.False(ViewModel.HasDetail);
        Assert.Empty(Prices.DetailRequests);
    }

    /// <summary>
    /// Prüft, dass die Details abgerufen werden und die Anzeige Preise der aktivierten Sorten, Entfernung aus der Liste, Öffnungszeiten und Hinweis zeigt.
    /// </summary>
    [Fact]
    public async Task Appearing_LoadsDetailsAndCombinesWithListDistance()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-2), Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.Equal([StationId], Prices.DetailRequests);
        var detail = ViewModel.Detail!;
        Assert.Equal("1,8 km", detail.DistanceText);
        Assert.Equal(3, detail.PriceLines.Count);
        Assert.Equal("vor 2 Min.", detail.PriceLines[0].AgeText);
        Assert.Equal("Mo-So: 00:00 – 24:00 Uhr", Assert.Single(detail.OpeningHours).DisplayText);
        Assert.True(detail.IsAutomatedStation);
        Assert.False(ViewModel.IsOffline);
        Assert.False(ViewModel.HasStatusMessage);
        Assert.False(ViewModel.HasSourceNote);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass nur die in den Einstellungen aktivierten Sorten angezeigt werden.
    /// </summary>
    [Fact]
    public async Task Appearing_ShowsOnlySettingsFuels()
    {
        Settings.Stored = AppSettings.CreateDefault() with { FuelTypes = [new FuelTypeSelection(FuelType.Diesel, true), new FuelTypeSelection(FuelType.SuperE5, false), new FuelTypeSelection(FuelType.SuperE10, false)] };
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.Equal(FuelType.Diesel, Assert.Single(ViewModel.Detail!.PriceLines).FuelType);
    }

    /// <summary>
    /// Prüft, dass ohne Preise für die aktivierten Sorten der Hinweis dazu erscheint.
    /// </summary>
    [Fact]
    public async Task Appearing_NoPricesForEnabledFuels_ShowsNoPricesNote()
    {
        Settings.Stored = AppSettings.CreateDefault() with { FuelTypes = [new FuelTypeSelection(FuelType.Diesel, false), new FuelTypeSelection(FuelType.SuperE5, false), new FuelTypeSelection(FuelType.SuperE10, false)] };
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());

        await AppearAsync();

        Assert.True(ViewModel.ShowNoPrices);
    }

    /// <summary>
    /// Prüft, dass die Einstellungen nur einmal geladen werden, auch bei einer erneuten Aktualisierung.
    /// </summary>
    [Fact]
    public async Task Refresh_ReusesSettings()
    {
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();

        ViewModel.RefreshCommand.Execute(null);
        await ViewModel.LastLoadTask;

        Assert.Equal(1, Settings.LoadCount);
        Assert.Equal(2, Prices.DetailRequests.Count);
    }

    /// <summary>
    /// Prüft, dass das Verlassen der Seite einen laufenden Abruf abbricht und das Laden beendet.
    /// </summary>
    [Fact]
    public async Task Disappearing_CancelsRunningLoad()
    {
        var gate = new TaskCompletionSource();
        Prices.DetailGate = gate.Task;
        Prices.DetailResult = new StationDetailResult(CreateStation(Now, Now), PriceDataSource.Live, PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        ViewModel.OnAppearing();
        Assert.True(ViewModel.IsBusy);

        ViewModel.OnDisappearing();
        gate.SetResult();
        await ViewModel.LastLoadTask;

        Assert.False(ViewModel.IsBusy);
        Assert.Equal("1,850 €", ViewModel.Detail!.PriceLines[0].PriceText);
    }
}
