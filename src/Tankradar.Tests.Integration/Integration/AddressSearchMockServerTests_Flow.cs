using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Suche nach Adresse, Ort oder PLZ im Zusammenspiel von ViewModel, Ortssuchdienst, Preisdienst und beiden Mock-Servern:
/// Umkreis rund um die aufgelöste Position, Entfernung zur gesuchten Adresse, Fehlerfälle ohne Preisabfrage und Unabhängigkeit vom Standortdienst.
/// </summary>
public class AddressSearchMockServerTests_Flow : SearchMockServerTestBase
{
    private async Task<MapViewModel> CreateAddressViewModelAsync(Uri? geocodingUrl = null)
    {
        var viewModel = CreateViewModel(CreateService(), geocodingUrl is null ? null : CreateGeocoding(geocodingUrl));
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        viewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        return viewModel;
    }

    /// <summary>
    /// Prüft, dass rund um Berlin-Mitte mit 5 km Alpha und Beta erscheinen und die Preisanfrage die aufgelöste Position sendet (nicht den Teststandort).
    /// </summary>
    [Fact]
    public async Task Berlin_Radius5_ShowsNearStationsAroundResolvedPosition()
    {
        var viewModel = await CreateAddressViewModelAsync();
        viewModel.AddressText = "Berlin";
        viewModel.RadiusKm = 5;

        await viewModel.SearchAsync();

        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle"], viewModel.Stations.Select(station => station.Name));
        Assert.Equal(52.52, Server.LastListLatitude!.Value, 4);
        Assert.Equal(13.405, Server.LastListLongitude!.Value, 4);
        Assert.Equal(5, Server.LastListRadius);
        Assert.Equal("Suche rund um: Berlin, Deutschland", viewModel.ResolvedPlace);
        Assert.False(viewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass die Entfernung auf die gesuchte Adresse bezogen ist: In Oranienburg ist Epsilon die einzige Tankstelle im 1-km-Umkreis und liegt bei 0 km.
    /// </summary>
    [Fact]
    public async Task Oranienburg_Radius1_ShowsOnlyEpsilonWithDistanceToAddress()
    {
        var viewModel = await CreateAddressViewModelAsync();
        viewModel.AddressText = "Oranienburg";
        viewModel.RadiusKm = 1;

        await viewModel.SearchAsync();

        var station = Assert.Single(viewModel.Stations);
        Assert.Equal("Epsilon Tankstelle", station.Name);
        Assert.Equal("0,0 km", station.DistanceText);
        Assert.Equal(52.88, Server.LastListLatitude!.Value, 4);
    }

    /// <summary>
    /// Prüft, dass die Ergebnisliste wie bei der Standortsuche nach Entfernung, Name und Preis sortiert werden kann.
    /// </summary>
    [Fact]
    public async Task Berlin_Radius25_CanBeSortedByNameAndFilteredByFuel()
    {
        var viewModel = await CreateAddressViewModelAsync();
        viewModel.AddressText = "10115";
        viewModel.RadiusKm = 25;
        await viewModel.SearchAsync();

        viewModel.SortOptions.Single(option => option.Value == MAUI.Models.ResultSortOrder.Name).SelectCommand.Execute(null);
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle", "Delta Tankstelle", "Gamma Tankstelle"], viewModel.Stations.Select(station => station.Name));

        viewModel.FuelFilterOptions.Single(option => option.FuelType == MAUI.Models.FuelType.Diesel).SelectCommand.Execute(null);
        Assert.DoesNotContain("Beta Tankstelle", viewModel.Stations.Select(station => station.Name));
        Assert.Equal(1, Server.ListRequests);
        Assert.Equal(1, Nominatim.Requests);
    }

    /// <summary>
    /// Prüft, dass ein unbekannter Ort gemeldet wird und keine Preisanfrage erfolgt.
    /// </summary>
    [Fact]
    public async Task UnknownPlace_ShowsNotFoundAndSendsNoPriceRequest()
    {
        var viewModel = await CreateAddressViewModelAsync();
        viewModel.AddressText = MockNominatimServer.QueryUnknown;

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.AddressNotFound, viewModel.StatusMessage);
        Assert.Equal(0, Server.TotalRequests);
        Assert.Equal(1, Nominatim.Requests);
        Assert.Empty(viewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben weder den Ortssuchdienst noch den Preisdienst erreichen.
    /// </summary>
    [Fact]
    public async Task InvalidInput_ReachesNeitherService()
    {
        var viewModel = await CreateAddressViewModelAsync();
        viewModel.AddressText = "x";

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.AddressTooShort, viewModel.StatusMessage);
        Assert.Equal(0, Nominatim.Requests);
        Assert.Equal(0, Server.TotalRequests);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung keine Anfrage an den Ortssuchdienst geht.
    /// </summary>
    [Fact]
    public async Task Offline_SendsNoRequests()
    {
        var viewModel = await CreateAddressViewModelAsync();
        Connection.Change(false);
        viewModel.AddressText = "Berlin";

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.AddressOffline, viewModel.StatusMessage);
        Assert.Equal(0, Nominatim.Requests);
        Assert.Equal(0, Server.TotalRequests);
    }

    /// <summary>
    /// Prüft, dass ein nicht erreichbarer Ortssuchdienst verständlich gemeldet wird und der Preisdienst unberührt bleibt.
    /// </summary>
    [Fact]
    public async Task UnreachableGeocoding_ShowsServiceMessage()
    {
        using var stopped = new MockNominatimServer();
        var url = stopped.BaseUrl;
        stopped.Dispose();
        var viewModel = await CreateAddressViewModelAsync(url);
        viewModel.AddressText = "Berlin";

        await viewModel.SearchAsync();

        Assert.Equal(SearchTexts.GeocodingUnavailable, viewModel.StatusMessage);
        Assert.Equal(0, Server.TotalRequests);
    }

    /// <summary>
    /// Prüft, dass die Adresssuche den Standortdienst nicht benötigt: Die Preisanfrage trägt die Position der Adresse, nicht den Teststandort.
    /// </summary>
    [Fact]
    public async Task AddressSearch_DoesNotUseTestLocation()
    {
        var viewModel = await CreateAddressViewModelAsync();
        viewModel.AddressText = "Eberswalde";
        viewModel.RadiusKm = 1;

        await viewModel.SearchAsync();

        Assert.Equal(["Delta Tankstelle"], viewModel.Stations.Select(station => station.Name));
        Assert.NotEqual(SearchLongitude, Server.LastListLongitude!.Value, 3);
        Assert.Equal(13.73, Server.LastListLongitude!.Value, 4);
    }
}
