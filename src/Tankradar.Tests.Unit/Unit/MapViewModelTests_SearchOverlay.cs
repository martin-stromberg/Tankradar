using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Suchdialog über der Karte im <see cref="MapViewModel"/>: Öffnen und Schließen ohne Verlust der Eingaben und Schließen beim Start einer Suche.
/// </summary>
public class MapViewModelTests_SearchOverlay : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass der Dialog geschlossen startet, sich öffnen und ohne Suche schließen lässt und Suchart, Adresse und Radius dabei erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task OpenSearchOverlay_SetsFlag_AndCloseKeepsInputs()
    {
        await AppearAsync();
        Assert.False(ViewModel.IsSearchOverlayOpen);

        ViewModel.OpenSearchOverlayCommand.Execute(null);
        Assert.True(ViewModel.IsSearchOverlayOpen);
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        ViewModel.AddressText = "Berlin";
        ViewModel.RadiusOptions.Single(option => option.RadiusKm == 10).SelectCommand.Execute(null);

        ViewModel.CloseSearchOverlayCommand.Execute(null);

        Assert.False(ViewModel.IsSearchOverlayOpen);
        Assert.True(ViewModel.IsAddressMode);
        Assert.Equal("Berlin", ViewModel.AddressText);
        Assert.Equal(10, ViewModel.RadiusKm);
        Assert.Empty(Prices.Queries);
        Assert.Empty(Geocoding.Inputs);
    }

    /// <summary>
    /// Prüft, dass eine ungültige Eingabe den Dialog mit Meldung offen lässt.
    /// </summary>
    [Fact]
    public async Task Search_InvalidInput_KeepsOverlayOpen()
    {
        await OpenOverlayInAddressModeAsync();

        await SearchAsync();

        Assert.True(ViewModel.IsSearchOverlayOpen);
        Assert.Equal(SearchTexts.AddressEmpty, ViewModel.StatusMessage);
        Assert.Empty(Prices.Queries);
    }

    /// <summary>
    /// Prüft, dass der Start einer gültigen Suche den Dialog schließt.
    /// </summary>
    [Fact]
    public async Task Search_ClosesOverlay_OnStart()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await OpenOverlayInAddressModeAsync();
        ViewModel.AddressText = "Berlin";

        await SearchAsync();

        Assert.False(ViewModel.IsSearchOverlayOpen);
        Assert.Equal(2, ViewModel.Stations.Count);
    }

    private async Task OpenOverlayInAddressModeAsync()
    {
        await AppearAsync();
        ViewModel.OpenSearchOverlayCommand.Execute(null);
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
    }
}
