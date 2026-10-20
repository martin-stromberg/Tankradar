using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass <see cref="ShellTankbookNavigator"/> nur die Seiten des Fahrzeugs zum Schließen auswählt, dessen Seite geschlossen werden soll.
/// </summary>
public class ShellTankbookNavigatorTests_SelectPages : TankbookEntriesViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Seite des eigenen Fahrzeugs ausgewählt wird.
    /// </summary>
    [Fact]
    public async Task SelectPages_OwnVehicle_SelectsPage()
    {
        var vehicle = await CreateVehicleAsync();
        var pages = new[] { ("A", (object?)await OpenAsync(vehicle.Id)) };

        Assert.Equal(["A"], ShellTankbookNavigator.SelectPages(pages, page => page.Item2, vehicle.Id).Select(page => page.Item1));
    }

    /// <summary>
    /// Prüft, dass Seiten eines anderen Fahrzeugs, ohne ViewModel oder mit fremdem ViewModel nicht ausgewählt werden.
    /// </summary>
    [Fact]
    public async Task SelectPages_OtherVehicleOrOtherContext_SelectsNothing()
    {
        var vehicle = await CreateVehicleAsync();
        var pages = new[] { ("A", (object?)await OpenAsync(vehicle.Id)), ("B", null), ("C", "kein ViewModel") };

        Assert.Empty(ShellTankbookNavigator.SelectPages(pages, page => page.Item2, vehicle.Id + 1));
    }
}
