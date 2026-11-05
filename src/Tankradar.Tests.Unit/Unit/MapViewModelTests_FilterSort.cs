using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass Filter- und Sortierwechsel das gehaltene Ergebnis neu aufbereiten, ohne erneut Standort oder Preisdienst abzufragen.
/// </summary>
public class MapViewModelTests_FilterSort : MapViewModelTestBase
{
    private async Task SearchWithTwoStationsAsync()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();
        await SearchAsync();
    }

    private FuelFilterOptionViewModel FilterOption(string key)
    {
        return ViewModel.FuelFilterOptions.Single(option => option.AutomationKey == key);
    }

    private ChoiceOptionViewModel<ResultSortOrder> SortOption(ResultSortOrder order)
    {
        return ViewModel.SortOptions.Single(option => option.Value == order);
    }

    /// <summary>
    /// Prüft, dass die Filteroptionen „Alle“ und die gewählten Sorten in Einstellungsreihenfolge enthalten und „Alle“ vorgewählt ist.
    /// </summary>
    [Fact]
    public async Task FilterOptions_AreAllPlusSelectedFuelsInSettingsOrder()
    {
        Settings.Stored = Settings.Stored with { FuelTypes = SearchTestData.Only(FuelType.Diesel, FuelType.SuperE10) };

        await AppearAsync();

        Assert.Equal(["All", "Diesel", "SuperE10"], ViewModel.FuelFilterOptions.Select(option => option.AutomationKey));
        Assert.Equal(["Alle", "Diesel", "Super E10"], ViewModel.FuelFilterOptions.Select(option => option.Label));
        Assert.True(FilterOption("All").IsSelected);
    }

    /// <summary>
    /// Prüft, dass ein Filterwechsel die Liste neu aufbereitet, ohne Standort oder Preisdienst erneut aufzurufen.
    /// </summary>
    [Fact]
    public async Task FilterChange_ReBuildsListWithoutNewCalls()
    {
        await SearchWithTwoStationsAsync();
        Assert.Equal(2, ViewModel.Stations.Count);

        FilterOption("Diesel").IsSelected = true;

        Assert.Equal(["Nahe"], ViewModel.Stations.Select(item => item.Name));
        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.False(FilterOption("All").IsSelected);

        FilterOption("All").IsSelected = true;

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Single(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass ein Sortierwechsel die Reihenfolge ändert, ohne Standort oder Preisdienst erneut aufzurufen.
    /// </summary>
    [Fact]
    public async Task SortChange_ReordersWithoutNewCalls()
    {
        await SearchWithTwoStationsAsync();
        Assert.Equal(["Ferne", "Nahe"], ViewModel.Stations.Select(item => item.Name));

        SortOption(ResultSortOrder.Distance).IsSelected = true;
        Assert.Equal(["Nahe", "Ferne"], ViewModel.Stations.Select(item => item.Name));

        SortOption(ResultSortOrder.Name).IsSelected = true;
        Assert.Equal(["Ferne", "Nahe"], ViewModel.Stations.Select(item => item.Name));

        Assert.Single(Location.Calls);
        Assert.Single(Prices.Queries);
        Assert.False(SortOption(ResultSortOrder.Distance).IsSelected);
    }

    /// <summary>
    /// Prüft, dass ein gewählter Filter beim erneuten Erscheinen der Seite erhalten bleibt, solange die Sorte noch gewählt ist.
    /// </summary>
    [Fact]
    public async Task Filter_SurvivesReappearing()
    {
        await SearchWithTwoStationsAsync();
        FilterOption("Diesel").IsSelected = true;

        await AppearAsync();

        Assert.True(FilterOption("Diesel").IsSelected);
        Assert.Equal(["Nahe"], ViewModel.Stations.Select(item => item.Name));
    }

    /// <summary>
    /// Prüft, dass ein Filter auf eine in den Einstellungen abgewählte Sorte beim erneuten Erscheinen auf „Alle“ zurückfällt.
    /// </summary>
    [Fact]
    public async Task Filter_FallsBackToAllWhenFuelIsDeselected()
    {
        await SearchWithTwoStationsAsync();
        FilterOption("Diesel").IsSelected = true;
        Settings.Stored = Settings.Stored with { FuelTypes = SearchTestData.Only(FuelType.SuperE5, FuelType.SuperE10) };

        await AppearAsync();

        Assert.True(FilterOption("All").IsSelected);
        Assert.Equal(2, ViewModel.Stations.Count);
    }
}
