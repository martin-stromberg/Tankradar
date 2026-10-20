using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.MAUI.ViewModels;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// Gemeinsamer Aufbau der Tests der Routensuche gegen die drei Mock-Server (Nominatim, OpenRouteService, Tankerkönig) mit echten Diensten.
/// Die Route verläuft standardmäßig geradlinig von Berlin-Mitte nach Oranienburg (rund 40 km entlang des Längengrads 13,405).
/// </summary>
public abstract class RouteSearchMockServerTestBase : SearchMockServerTestBase
{
    /// <summary>
    /// Legt die zusätzlichen Routen-Teststationen im Tankerkönig-Mock an.
    /// </summary>
    protected RouteSearchMockServerTestBase()
    {
        Server.AddRouteStations();
    }

    /// <summary>
    /// Erstellt das ViewModel im Routenmodus mit Adressstart „Berlin“ und Ziel „Oranienburg“, dem übergebenen Abstand und der Sortierung nach Entfernung (Streckenposition).
    /// </summary>
    /// <param name="service">Der Preisdienst oder <see langword="null"/> für den Dienst gegen den Mock-Server.</param>
    /// <param name="routeSearch">Der Routensuchdienst oder <see langword="null"/> für den Dienst gegen die Mock-Server.</param>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    /// <param name="startAddress"><see langword="true"/> für den Adressstart, <see langword="false"/> für den (Test-)Standort.</param>
    /// <returns>Das ViewModel, bereit für die Suche.</returns>
    protected async Task<MapViewModel> CreateRouteViewModelAsync(FuelPriceService? service = null, IRouteSearchService? routeSearch = null, int distanceKm = 2, bool startAddress = true)
    {
        var viewModel = CreateViewModel(service ?? CreateService(), routeSearch: routeSearch);
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        viewModel.SortOptions.Single(option => option.Value == MAUI.Models.ResultSortOrder.Distance).SelectCommand.Execute(null);
        viewModel.ModeOptions.Single(option => option.Value == SearchMode.Route).SelectCommand.Execute(null);
        if (startAddress)
        {
            viewModel.RouteStartOptions.Single(option => option.Value == RouteStartKind.Address).SelectCommand.Execute(null);
            viewModel.StartInput = MockNominatimServer.QueryBerlin;
        }

        viewModel.DestinationInput = MockNominatimServer.QueryOranienburg;
        viewModel.RouteDistanceOptions.Single(option => option.RadiusKm == distanceKm).SelectCommand.Execute(null);
        return viewModel;
    }

    /// <summary>
    /// Die Namen der Tankstellen der Liste in der angezeigten Reihenfolge.
    /// </summary>
    /// <param name="viewModel">Das ViewModel.</param>
    /// <returns>Die Namen.</returns>
    protected static string[] Names(MapViewModel viewModel)
    {
        return viewModel.Stations.Select(station => station.Name).ToArray();
    }
}
