using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Chip-Auswahl des Radius und das schrittweise Anzeigen langer Ergebnislisten.
/// </summary>
public class MapViewModelTests_Chips : MapViewModelTestBase
{
    private StationInfo[] ManyStations(int count)
    {
        return Enumerable.Range(1, count)
            .Select(number => StationFactory.CreateCustom(number, $"Station {number:D3}", number, Now, null, true, (FuelType.SuperE5, 1.50m + (number / 1000m))))
            .ToArray();
    }

    /// <summary>
    /// Prüft, dass die Radiusstufen 1, 2, 5, 10, 15 und 25 km angeboten werden und 5 km vorgewählt ist.
    /// </summary>
    [Fact]
    public void RadiusOptions_AreStepsWithinLimitsAndDefaultIsSelected()
    {
        Assert.Equal(SearchRadius.Steps, ViewModel.RadiusOptions.Select(option => option.RadiusKm));
        Assert.Equal(["1 km", "2 km", "5 km", "10 km", "15 km", "25 km"], ViewModel.RadiusOptions.Select(option => option.Label));
        Assert.Equal([5], ViewModel.RadiusOptions.Where(option => option.IsSelected).Select(option => option.RadiusKm));
        Assert.Equal(ChoiceTexts.Selected, ViewModel.RadiusOptions.Single(option => option.IsSelected).SelectionHint);
    }

    /// <summary>
    /// Prüft, dass die Wahl einer Stufe den Radius setzt und genau dieser an den Preisdienst gesendet wird.
    /// </summary>
    [Fact]
    public async Task SelectingRadiusChip_SetsRadiusAndIsSent()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();

        ViewModel.RadiusOptions.Single(option => option.RadiusKm == 15).SelectCommand.Execute(null);
        await SearchAsync();

        Assert.Equal(15, ViewModel.RadiusKm);
        Assert.Equal([15], ViewModel.RadiusOptions.Where(option => option.IsSelected).Select(option => option.RadiusKm));
        Assert.Equal(15, Assert.Single(Prices.Queries).RadiusKm);
    }

    /// <summary>
    /// Prüft, dass ein von den Stufen abweichender Radius keine Stufe markiert und ein ungültiger weiterhin vor dem Abruf abgewiesen wird.
    /// </summary>
    [Fact]
    public async Task RadiusOutsideSteps_DeselectsChipsAndInvalidIsRejected()
    {
        await AppearAsync();

        ViewModel.RadiusKm = 7;
        Assert.DoesNotContain(ViewModel.RadiusOptions, option => option.IsSelected);

        ViewModel.RadiusKm = 26;
        await SearchAsync();

        Assert.Equal(SearchTexts.RadiusInvalid, ViewModel.StatusMessage);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass nur die erste Seite angezeigt wird, „Weitere anzeigen“ die nächste Seite ergänzt und die Beschriftung die Restzahl nennt.
    /// </summary>
    [Fact]
    public async Task LongResult_IsShownInPages()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, ManyStations(60));
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(MapViewModel.PageSize, ViewModel.Stations.Count);
        Assert.Equal(60, ViewModel.TotalStationCount);
        Assert.True(ViewModel.HasMore);
        Assert.Equal("Weitere anzeigen (35 weitere)", ViewModel.ShowMoreText);

        ViewModel.ShowMoreCommand.Execute(null);
        Assert.Equal(50, ViewModel.Stations.Count);

        ViewModel.ShowMoreCommand.Execute(null);
        Assert.Equal(60, ViewModel.Stations.Count);
        Assert.False(ViewModel.HasMore);
    }

    /// <summary>
    /// Prüft, dass ein Filter- oder Sortierwechsel die Anzeige auf die erste Seite zurücksetzt.
    /// </summary>
    [Fact]
    public async Task SortChange_ResetsPaging()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, ManyStations(60));
        await AppearAsync();
        await SearchAsync();
        ViewModel.ShowMoreCommand.Execute(null);
        Assert.Equal(50, ViewModel.Stations.Count);

        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Distance).IsSelected = true;

        Assert.Equal(MapViewModel.PageSize, ViewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass ein Filterwechsel die Anzeige auf die erste Seite (25 Einträge) zurücksetzt.
    /// </summary>
    [Fact]
    public async Task FilterChange_ResetsPaging()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, ManyStations(60));
        await AppearAsync();
        await SearchAsync();
        ViewModel.ShowMoreCommand.Execute(null);
        Assert.Equal(50, ViewModel.Stations.Count);

        ViewModel.FuelFilterOptions.Single(option => option.AutomationKey == "SuperE5").IsSelected = true;

        Assert.Equal(MapViewModel.PageSize, ViewModel.Stations.Count);
        Assert.True(ViewModel.HasMore);
        Assert.Equal("Weitere anzeigen (35 weitere)", ViewModel.ShowMoreText);
    }

    /// <summary>
    /// Prüft, dass HasMore und ShowMoreText auch dann gemeldet werden, wenn sich die dargestellte Liste nicht ändert.
    /// </summary>
    [Fact]
    public async Task SortChange_NotifiesShowMoreProperties()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, ManyStations(60));
        await AppearAsync();
        await SearchAsync();
        var changed = new List<string?>();
        ViewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Distance).IsSelected = true;

        Assert.Contains(nameof(MapViewModel.HasMore), changed);
        Assert.Contains(nameof(MapViewModel.ShowMoreText), changed);
        Assert.Contains(nameof(MapViewModel.TotalStationCount), changed);
    }

    /// <summary>
    /// Prüft, dass kurze Ergebnisse vollständig angezeigt werden und keine Schaltfläche erscheint.
    /// </summary>
    [Fact]
    public async Task ShortResult_HasNoShowMore()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();

        await SearchAsync();

        Assert.False(ViewModel.HasMore);
        Assert.Equal(2, ViewModel.Stations.Count);
    }
}
