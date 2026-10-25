using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den erfolgreichen Gesamtablauf der Routensuche und die Reihenfolge der Aufrufe.
/// </summary>
public class RouteSearchServiceTests_Flow : RouteSearchServiceTestBase
{
    /// <summary>
    /// Prüft Ergebnis, Namen, Positionen und Stationen eines erfolgreichen Ablaufs.
    /// </summary>
    [Fact]
    public async Task SearchAsync_Success_ReturnsRouteNamesAndStations()
    {
        var station = RouteStationFactory.At(1, 52.70, 13.405);
        Prices.Respond = (_, _) => RouteStationFactory.Live(station);

        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.Success, result.Status);
        Assert.NotNull(result.Route);
        Assert.Equal("Berlin-Mitte", result.StartName);
        Assert.Equal("Oranienburg, Brandenburg", result.DestinationName);
        Assert.Equal(52.52, result.StartPosition!.Latitude);
        Assert.Equal(52.88, result.DestinationPosition!.Latitude);
        Assert.Equal(result.Route!.LengthKm, result.LengthKm);
        Assert.Equal(station.Id, Assert.Single(result.Stations).Station.Id);
    }

    /// <summary>
    /// Prüft die Reihenfolge: erst Start, dann Ziel, dann Route, dann Tankstellen.
    /// </summary>
    [Fact]
    public async Task SearchAsync_Success_CallsServicesInOrder()
    {
        var progress = new ListProgress();

        await SearchAsync(progress: progress);

        Assert.Equal([StartText, DestinationText], Geocoding.Inputs);
        Assert.Equal(1, Routing.Calls);
        Assert.NotEmpty(Prices.Queries);
        Assert.Equal(RouteSearchPhase.ResolvingPlaces, progress.Reports[0].Phase);
        Assert.Equal(RouteSearchPhase.CalculatingRoute, progress.Reports[1].Phase);
        Assert.All(progress.Reports.Skip(2), report => Assert.Equal(RouteSearchPhase.SearchingStations, report.Phase));
    }

    /// <summary>
    /// Prüft, dass bei vorgegebener Startposition (aktueller Standort) nur das Ziel aufgelöst wird und der Start nicht über die Ortssuche läuft.
    /// </summary>
    [Fact]
    public async Task SearchAsync_StartPositionGiven_ResolvesOnlyDestination()
    {
        var request = new RouteSearchRequest(new GeoPosition(52.52, 13.405), null, DestinationText, 2, [FuelType.SuperE5]);

        var result = await SearchAsync(request);

        Assert.Equal(RouteSearchStatus.Success, result.Status);
        Assert.Equal([DestinationText], Geocoding.Inputs);
        Assert.Null(result.StartName);
        Assert.Equal(52.52, Routing.LastStart!.Latitude);
    }

    /// <summary>
    /// Prüft, dass eine Route ohne Treffer ein erfolgreiches Ergebnis mit leerer Liste ist.
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoStationsInBand_IsSuccessWithEmptyList()
    {
        var result = await SearchAsync();

        Assert.Equal(RouteSearchStatus.Success, result.Status);
        Assert.Empty(result.Stations);
    }

    /// <summary>
    /// Prüft, dass die Abfragen die Sorten und Radien der Abfragepunkte verwenden und der Radius 25 km nie überschreitet.
    /// </summary>
    [Fact]
    public async Task SearchAsync_Success_UsesQueryPointsOfSampler()
    {
        await SearchAsync(Request(5));

        Assert.Equal(1, Prices.MaxConcurrent);
        Assert.All(Prices.Queries, query =>
        {
            Assert.InRange(query.RadiusKm, 1, 25);
            Assert.Equal([FuelType.SuperE5], query.FuelTypes);
        });
    }

    /// <summary>
    /// Prüft, dass Orte und Routen nicht über die Textdarstellung der Ergebnisse oder Aufträge preisgegeben werden.
    /// </summary>
    [Fact]
    public async Task ToString_OfRequestAndResult_ContainsNoPlacesOrCoordinates()
    {
        var result = await SearchAsync();

        Assert.Equal("Success", result.ToString());
        Assert.DoesNotContain("Oranienburg", Request().ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("52", result.Route!.ToString(), StringComparison.Ordinal);
        Assert.Equal(GeocodingStatus.Found, result.GeocodingStatus);
    }
}
