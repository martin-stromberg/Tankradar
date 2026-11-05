using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Öffnen der Detailansicht aus der Ergebnisliste im <see cref="MAUI.ViewModels.MapViewModel"/>.
/// </summary>
public class MapViewModelTests_OpenStation : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass der Befehl die gewählte Tankstelle an die Navigation übergibt.
    /// </summary>
    [Fact]
    public async Task OpenStationCommand_PassesTappedStationToNavigator()
    {
        Prices.Result = Result(MAUI.Models.Pricing.PriceDataSource.Live, MAUI.Models.Pricing.PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
        var second = ViewModel.Stations[1];

        ViewModel.OpenStationCommand.Execute(second);

        var opened = Assert.Single(Navigator.Opened);
        Assert.Equal(second.Id, opened.Id);
        Assert.Equal(second.PriceLines, opened.PriceLines);
    }

    /// <summary>
    /// Prüft, dass ein leerer Parameter nichts öffnet.
    /// </summary>
    [Fact]
    public void OpenStationCommand_NullParameter_DoesNothing()
    {
        ViewModel.OpenStationCommand.Execute(null);

        Assert.Empty(Navigator.Opened);
    }

    /// <summary>
    /// Prüft, dass ein Fehler der Navigation gemeldet wird, ohne die Liste zu verändern.
    /// </summary>
    [Fact]
    public async Task OpenStationCommand_NavigationFails_ShowsMessage()
    {
        Prices.Result = Result(MAUI.Models.Pricing.PriceDataSource.Live, MAUI.Models.Pricing.PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
        Navigator.OpenException = new InvalidOperationException("kaputt");

        ViewModel.OpenStationCommand.Execute(ViewModel.Stations[0]);

        Assert.Equal(DetailTexts.NotAvailable, ViewModel.StatusMessage);
        Assert.Equal(2, ViewModel.Stations.Count);
    }
}
