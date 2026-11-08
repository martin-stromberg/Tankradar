using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Befehl „Navigation starten“: Bedienbarkeit je nach Ziel (Position oder Adresse) und der Aufruf des Karten-Öffners.
/// </summary>
public class StationDetailViewModelTests_Navigation : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass ohne Angaben zur Tankstelle keine Navigation angeboten wird.
    /// </summary>
    [Fact]
    public void CanNavigate_NoDetail_False()
    {
        Assert.False(ViewModel.CanNavigate);
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle mit Position die Navigation anbietet und die Koordinaten an den Karten-Öffner gehen.
    /// </summary>
    [Fact]
    public async Task Navigate_OriginWithPosition_OpensCoordinates()
    {
        ViewModel.Show(CreateOrigin() with { Latitude = 52.52, Longitude = 13.405 });

        Assert.True(ViewModel.CanNavigate);
        ViewModel.NavigateCommand.Execute(null);
        await ViewModel.LastNavigationTask;

        var target = Assert.Single(MapOpener.PositionTargets);
        Assert.Equal(52.52, target.Position.Latitude);
        Assert.Equal(13.405, target.Position.Longitude);
        Assert.Equal("Alpha Tankstelle", target.Name);
        Assert.Empty(MapOpener.AddressTargets);
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle ohne Position, aber mit Adresse die Adresse an den Karten-Öffner gibt.
    /// </summary>
    [Fact]
    public async Task Navigate_OriginWithAddressOnly_OpensAddress()
    {
        ViewModel.Show(CreateOrigin());

        Assert.True(ViewModel.CanNavigate);
        ViewModel.NavigateCommand.Execute(null);
        await ViewModel.LastNavigationTask;

        var target = Assert.Single(MapOpener.AddressTargets);
        Assert.Equal("Hauptstraße 1, 10115 Berlin", target.Address);
        Assert.Equal("Alpha Tankstelle", target.Name);
        Assert.Empty(MapOpener.PositionTargets);
    }

    /// <summary>
    /// Prüft, dass ohne Position und ohne Adresse weder die Schaltfläche angeboten noch der Öffner gerufen wird.
    /// </summary>
    [Fact]
    public async Task Navigate_NoPositionNoAddress_NotOfferedAndNoCall()
    {
        ViewModel.Show(CreateOrigin() with { AddressText = "" });

        Assert.False(ViewModel.CanNavigate);
        ViewModel.NavigateCommand.Execute(null);
        await ViewModel.LastNavigationTask;

        Assert.Empty(MapOpener.PositionTargets);
        Assert.Empty(MapOpener.AddressTargets);
    }

    /// <summary>
    /// Prüft, dass die geladenen Details der Tankstelle deren Position als Navigationsziel übernehmen.
    /// </summary>
    [Fact]
    public async Task Navigate_LoadedStationWithPosition_OpensCoordinates()
    {
        var station = CreateStation(Now.AddMinutes(-10), Now.AddMinutes(-10));
        Prices.DetailResult = new StationDetailResult(
            new StationInfo
            {
                Id = StationId,
                Name = station.Name,
                Street = station.Street,
                HouseNumber = station.HouseNumber,
                PostCode = station.PostCode,
                Place = station.Place,
                Latitude = 52.52,
                Longitude = 13.405,
            },
            PriceDataSource.Live,
            PriceFailure.None);
        ViewModel.Show(CreateOrigin());
        await AppearAsync();

        Assert.True(ViewModel.CanNavigate);
        ViewModel.NavigateCommand.Execute(null);
        await ViewModel.LastNavigationTask;

        var target = Assert.Single(MapOpener.PositionTargets);
        Assert.Equal(52.52, target.Position.Latitude);
        Assert.Equal(13.405, target.Position.Longitude);
    }

    /// <summary>
    /// Prüft, dass ein Fehlschlag des Karten-Öffners als Statusmeldung erscheint.
    /// </summary>
    [Fact]
    public async Task Navigate_OpenerFails_ShowsMessage()
    {
        MapOpener.Failure = new InvalidOperationException("kein Karten-App");
        ViewModel.Show(CreateOrigin() with { Latitude = 52.52, Longitude = 13.405 });

        ViewModel.NavigateCommand.Execute(null);
        await ViewModel.LastNavigationTask;

        Assert.Equal(DetailTexts.NavigationFailed, ViewModel.StatusMessage);
        Assert.Empty(MapOpener.PositionTargets);
    }
}
