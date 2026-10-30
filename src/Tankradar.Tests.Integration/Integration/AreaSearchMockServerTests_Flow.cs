using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft das Nachladen des sichtbaren Kartenausschnitts über ViewModel, Dienst, Client, Preis-Cache und Mock-Server: gesendeter Mittelpunkt und Radius,
/// Ersetzen des Ergebnisses und Verhalten ohne Verbindung.
/// </summary>
public class AreaSearchMockServerTests_Flow : SearchMockServerTestBase
{
    private static MapViewport DeltaArea()
    {
        // Rund um die Teststation Delta (Eberswalde), etwa 22 km östlich der Suchposition; bei Zoomstufe 13 ergibt der Ausschnitt einen Radius von 5 km.
        return MapViewport.Create(52.52, 13.73, 13, 400, 700);
    }

    private async Task<MapViewModel> SearchAtLocationAsync()
    {
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        await viewModel.LastRouteAccessTask;
        await viewModel.SearchAsync();
        return viewModel;
    }

    /// <summary>
    /// Prüft, dass das Nachladen genau eine Anfrage mit der Mitte des Ausschnitts und dem aufgerundeten Radius der halben Diagonale sendet.
    /// </summary>
    [Fact]
    public async Task ReloadArea_SendsCenterAndRadiusOfVisibleArea()
    {
        var viewModel = await SearchAtLocationAsync();
        var requests = Server.ListRequests;
        var area = DeltaArea();
        var expected = MapAreaQuery.From(area.GetBounds(), AllFuels)!;

        viewModel.OnViewportChanged(area);
        Assert.Equal(requests, Server.ListRequests);
        viewModel.ReloadVisibleAreaCommand.Execute(null);
        await viewModel.LastSearchTask;

        Assert.Equal(requests + 1, Server.ListRequests);
        Assert.Equal(5, Server.LastListRadius);
        Assert.Equal(expected.Latitude, Server.LastListLatitude!.Value, 4);
        Assert.Equal(expected.Longitude, Server.LastListLongitude!.Value, 4);
        Assert.Equal(52.52, Server.LastListLatitude.Value, 1);
        Assert.Equal(13.73, Server.LastListLongitude.Value, 1);
    }

    /// <summary>
    /// Prüft, dass das Ergebnis des Ausschnitts Liste und Markierungen ersetzt (nur Delta statt Alpha und Beta), die Suchposition behält und den Ausschnitt nicht einpasst.
    /// </summary>
    [Fact]
    public async Task ReloadArea_ReplacesResult_KeepsOriginAndViewport()
    {
        var viewModel = await SearchAtLocationAsync();
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle"], viewModel.Stations.Select(station => station.Name));
        var origin = viewModel.Origin;

        viewModel.OnViewportChanged(DeltaArea());
        viewModel.ReloadVisibleAreaCommand.Execute(null);
        await viewModel.LastSearchTask;

        Assert.Equal(["Delta Tankstelle"], viewModel.Stations.Select(station => station.Name));
        Assert.Equal(["Delta Tankstelle"], viewModel.MapMarkers.Select(marker => marker.Station.Name));
        Assert.Equal(1, viewModel.TotalStationCount);
        Assert.Same(origin, viewModel.Origin);
        Assert.Equal(MapFitMode.KeepViewport, viewModel.MapFitMode);
        Assert.Null(viewModel.StatusMessage);
        Assert.False(viewModel.ShowAreaReloadButton);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung keine Anfrage an den Preisdienst geht, ohne bekannte Preise des Ausschnitts die Liste leer ist und der Offline-Zustand mit Meldung erscheint.
    /// </summary>
    [Fact]
    public async Task ReloadArea_Offline_SendsNoRequestAndShowsMessage()
    {
        var viewModel = await SearchAtLocationAsync();
        var requests = Server.TotalRequests;
        Connection.Change(false);

        viewModel.OnViewportChanged(DeltaArea());
        viewModel.ReloadVisibleAreaCommand.Execute(null);
        await viewModel.LastSearchTask;

        Assert.Equal(requests, Server.TotalRequests);
        Assert.True(viewModel.IsOffline);
        Assert.Empty(viewModel.Stations);
        Assert.True(viewModel.HasStatusMessage);
        Assert.False(viewModel.IsBusy);
    }
}
