using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Umkreissuche über Dienst, Client, Preis-Cache und Mock-Server: Radiusfilterung, Sortierung, gesendeter Radius und Radiusprüfung vor dem Abruf.
/// </summary>
public class SearchMockServerTests_Flow : SearchMockServerTestBase
{
    /// <summary>
    /// Prüft, dass der Standardradius 5 km nur Alpha und Beta liefert und den Radius 5 sendet.
    /// </summary>
    [Fact]
    public async Task Search_Radius5_ReturnsOnlyNearStationsAndSendsRadius()
    {
        var result = await CreateService().SearchNearbyAsync(Query(5));

        Assert.Equal(PriceDataSource.Live, result.Source);
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle"], result.Stations.Select(s => s.Name));
        Assert.Equal(5, Server.LastListRadius);
        Assert.Equal(SearchLatitude, Server.LastListLatitude!.Value, 4);
        Assert.Equal(SearchLongitude, Server.LastListLongitude!.Value, 4);
    }

    /// <summary>
    /// Prüft, dass 25 km Gamma und Delta, aber nicht Epsilon liefert, nach Entfernung sortiert mit berechneten Entfernungen.
    /// </summary>
    [Fact]
    public async Task Search_Radius25_ReturnsFourStationsSortedByDistance()
    {
        var result = await CreateService().SearchNearbyAsync(Query(25));

        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Delta Tankstelle"], result.Stations.Select(s => s.Name));
        Assert.Equal(25, Server.LastListRadius);
        Assert.InRange(result.Stations[2].DistanceKm!.Value, 7, 9);
        Assert.InRange(result.Stations[3].DistanceKm!.Value, 21, 23);
        Assert.Equal(result.Stations.OrderBy(s => s.DistanceKm).Select(s => s.Id), result.Stations.Select(s => s.Id));
    }

    /// <summary>
    /// Prüft, dass sehr kleine Radien nur die nächste Station liefern.
    /// </summary>
    [Fact]
    public async Task Search_Radius1_ReturnsOnlyAlpha()
    {
        var result = await CreateService().SearchNearbyAsync(Query(1));

        Assert.Equal(["Alpha Tankstelle"], result.Stations.Select(s => s.Name));
    }

    /// <summary>
    /// Prüft, dass ein Radius außerhalb 1 bis 25 den Dienst mit ArgumentException abbrechen lässt, ohne dass der Mock eine Anfrage erhält.
    /// </summary>
    /// <param name="radius">Der Radius.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    [InlineData(500)]
    public async Task Search_InvalidRadius_SendsNoRequest(int radius)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().SearchNearbyAsync(Query(radius)));

        Assert.Equal(0, Server.TotalRequests);
        Assert.Null(Server.LastListRadius);
    }

    /// <summary>
    /// Prüft, dass das ViewModel bei ungültigem Radius weder den Standort ermittelt noch eine Anfrage sendet, bei gültigem aber genau den Radius sendet.
    /// </summary>
    [Fact]
    public async Task ViewModel_InvalidRadius_SendsNoRequest_ValidRadius_SendsIt()
    {
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;

        viewModel.RadiusKm = 26;
        await viewModel.SearchAsync();
        Assert.Equal(0, Server.TotalRequests);
        Assert.Equal(SearchTexts.RadiusInvalid, viewModel.StatusMessage);

        viewModel.RadiusKm = 25;
        await viewModel.SearchAsync();
        Assert.Equal(1, Server.ListRequests);
        Assert.Equal(25, Server.LastListRadius);
        Assert.Equal(4, viewModel.Stations.Count);
    }

    /// <summary>
    /// Prüft, dass der Mock Radien über 25 wie die Quelle mit Fehler beantwortet (Gegenprobe zur Sperre in der App).
    /// </summary>
    [Fact]
    public async Task Mock_RadiusAbove25_AnswersWithError()
    {
        using var http = new HttpClient();
        const string KeyParameter = "api" + "key";

        var body = await http.GetStringAsync(new Uri(Server.BaseUrl, $"list.php?lat=52.5&lng=13.4&rad=26&{KeyParameter}={MockTankerkoenigServer.AcceptedKey}"));

        Assert.Contains("\"ok\":false", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Aufbereitung: Mit Filter Diesel steht Gamma (günstigster Diesel) vorn, Beta (ohne Diesel) fehlt; Gamma führt kein E10.
    /// </summary>
    [Fact]
    public async Task Search_DieselFilter_SortsByDieselPriceAndDropsStationsWithoutDiesel()
    {
        var result = await CreateService().SearchNearbyAsync(Query(25));

        var items = StationResultBuilder.Build(
            result.Stations,
            AppSettings.CreateDefault().FuelTypes,
            FuelType.Diesel,
            ResultSortOrder.Price,
            Clock.GetUtcNow().UtcDateTime);

        Assert.Equal(["Gamma Tankstelle", "Delta Tankstelle", "Alpha Tankstelle"], items.Select(item => item.Name));
        Assert.DoesNotContain(items[0].PriceLines, line => line.FuelType == FuelType.SuperE10);
        Assert.Equal("vor 0 Min.", items[0].PriceLines[0].AgeText);
    }

    /// <summary>
    /// Prüft die Hinweise aus dem Detailabruf der neuen Stationen: Delta ist durchgehend geöffnet, Gamma nicht.
    /// </summary>
    [Fact]
    public async Task Detail_NewStations_DeriveAutomatedHint()
    {
        var service = CreateService();

        var gamma = (await service.GetStationDetailAsync(MockTankerkoenigServer.StationGamma)).Station!;
        var delta = (await service.GetStationDetailAsync(MockTankerkoenigServer.StationDelta)).Station!;

        Assert.False(StationHints.IsAutomatedStation(gamma.WholeDay, gamma.OpeningTimes));
        Assert.True(StationHints.IsAutomatedStation(delta.WholeDay, delta.OpeningTimes));
    }
}
