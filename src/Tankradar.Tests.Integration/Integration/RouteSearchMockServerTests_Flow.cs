using System.Diagnostics;
using Tankradar.TestSupport;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft die Routensuche mit echten Diensten gegen die drei Mock-Server: Reihenfolge und Anzahl der Aufrufe, Abfragebudget (höchstens 15), Mindestabstände,
/// Cache und Auswahl der Tankstellen nach Abstand zur Route.
/// </summary>
public class RouteSearchMockServerTests_Flow : RouteSearchMockServerTestBase
{
    /// <summary>
    /// Prüft die Auswahl bei 2 km: Stationen auf und nahe der Route in Reihenfolge der Streckenposition, Stationen weiter abseits oder hinter dem Ziel entfallen.
    /// </summary>
    [Fact]
    public async Task Search_TwoKilometers_ShowsStationsAlongRouteInOrder()
    {
        var viewModel = await CreateRouteViewModelAsync();

        await viewModel.SearchAsync();

        Assert.Null(viewModel.StatusMessage);
        Assert.Equal(["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Epsilon Tankstelle"], Names(viewModel));
        Assert.EndsWith("ab Start", viewModel.Stations[1].DistanceText, StringComparison.Ordinal);
        Assert.True(viewModel.HasMapRoute);
        Assert.Contains("Berlin", viewModel.RouteSummary, StringComparison.Ordinal);
        Assert.Contains("Oranienburg", viewModel.RouteSummary, StringComparison.Ordinal);
        Assert.Contains("5 Tankstellen", viewModel.RouteSummary, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die Reihenfolge und Anzahl der externen Aufrufe: zwei Ortsauflösungen, eine Route, eine Umkreisabfrage innerhalb der Grenze von 25 km.
    /// </summary>
    [Fact]
    public async Task Search_CallsEachServiceAsPlanned()
    {
        var viewModel = await CreateRouteViewModelAsync();

        await viewModel.SearchAsync();

        Assert.Equal(2, Nominatim.Requests);
        Assert.Equal(1, Routing.Requests);
        Assert.Equal(1, Server.ListRequests);
        Assert.InRange(Server.LastListRadius!.Value, 1, 25);
        Assert.Equal(52.70, Server.LastListLatitude!.Value, 2);
        Assert.Equal(MockOpenRouteServiceServer.AcceptedKey, Routing.LastAuthorization);
        Assert.True(Server.ListRequests <= 15);
    }

    /// <summary>
    /// Prüft, dass der einstellbare Abstand die Treffermenge ändert und die Zahl der Abfragen die Obergrenze nicht überschreitet.
    /// </summary>
    /// <param name="distanceKm">Der Abstand zur Route.</param>
    /// <param name="expected">Die erwarteten Namen.</param>
    [Theory]
    [InlineData(1, new[] { "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Epsilon Tankstelle" })]
    [InlineData(2, new[] { "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Epsilon Tankstelle" })]
    [InlineData(5, new[] { "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Route Mittel", "Epsilon Tankstelle" })]
    [InlineData(10, new[] { "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Route Mittel", "Epsilon Tankstelle", "Route Dahinter" })]
    public async Task Search_DistanceSteps_ChangeStationSet(int distanceKm, string[] expected)
    {
        var viewModel = await CreateRouteViewModelAsync(distanceKm: distanceKm);

        await viewModel.SearchAsync();

        Assert.Equal(expected, Names(viewModel));
        Assert.InRange(Server.ListRequests, 1, 15);
    }

    /// <summary>
    /// Prüft den Takt der Tankerkönig-Abfragen bei langer Route mit echter Drosselung: drei Abfragen im Abstand von mindestens einer Sekunde,
    /// und eine Wiederholung der Suche wird aus dem Such-Cache bedient (keine weiteren Abfragen).
    /// </summary>
    [Fact]
    public async Task Search_LongRoute_SpacesQueriesAndServesRepeatFromCache()
    {
        Routing.SetRoute([(52.52, 13.405), (52.80, 13.405), (53.10, 13.405), (53.40, 13.405)]);
        var viewModel = await CreateRouteViewModelAsync(CreateService(minRequestInterval: TimeSpan.FromSeconds(1)));

        await viewModel.SearchAsync();

        var times = Server.ListRequestTimes;
        Assert.Equal(3, times.Count);
        for (var i = 1; i < times.Count; i++)
        {
            Assert.True((times[i] - times[i - 1]).TotalSeconds >= 0.9, $"Abstand der Abfragen {i - 1} und {i} zu kurz: {(times[i] - times[i - 1]).TotalSeconds:0.00} s");
        }

        var requestsAfterFirst = Server.ListRequests;
        await viewModel.SearchAsync();

        Assert.Equal(requestsAfterFirst, Server.ListRequests);
        Assert.Equal(2, Routing.Requests);
    }

    /// <summary>
    /// Prüft, dass die zwei Ortsauflösungen mit dem Mindestabstand der Nutzungsrichtlinie von Nominatim erfolgen.
    /// Gemessen wird clientseitig beim Absenden, nicht beim Empfang am Mock-Server: Der Verbindungsaufbau der ersten Anfrage verzögert deren
    /// Empfang und verkürzt sonst den gemessenen Abstand. 10 ms Toleranz decken die Timer-Auflösung ab.
    /// </summary>
    [Fact]
    public async Task Search_GeocodingStartAndDestination_AreSpacedByNominatimInterval()
    {
        var timing = new RequestTimingHandler(new HttpClientHandler { AllowAutoRedirect = false });
        var viewModel = await CreateRouteViewModelAsync(geocoding: CreateGeocoding(handler: timing));

        await viewModel.SearchAsync();

        var times = timing.SendTimes;
        Assert.Equal(2, times.Count);
        Assert.Equal(2, Nominatim.RequestTimes.Count);
        var gap = Stopwatch.GetElapsedTime(times[0], times[1]);
        Assert.True(gap >= TimeSpan.FromMilliseconds(990), $"Die Anfragen an Nominatim liegen nur {gap.TotalMilliseconds} ms auseinander.");
        Assert.False(string.IsNullOrWhiteSpace(Nominatim.LastUserAgent));
    }

    /// <summary>
    /// Prüft den Start am aktuellen Standort: Es wird nur das Ziel über Nominatim aufgelöst.
    /// </summary>
    [Fact]
    public async Task Search_CurrentLocationStart_ResolvesOnlyDestination()
    {
        var viewModel = await CreateRouteViewModelAsync(startAddress: false);

        await viewModel.SearchAsync();

        Assert.Equal(1, Nominatim.Requests);
        Assert.Equal(1, Routing.Requests);
        Assert.Contains("\"coordinates\":[[13.412346,52.512346],[13.405,52.88]]", Routing.LastRequestBody, StringComparison.Ordinal);
        Assert.NotEmpty(viewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass Filter und Sortierung nach der Suche ohne weitere externe Aufrufe wirken.
    /// </summary>
    [Fact]
    public async Task FilterAndSort_AfterSearch_MakeNoFurtherCalls()
    {
        var viewModel = await CreateRouteViewModelAsync();
        await viewModel.SearchAsync();
        var nominatim = Nominatim.Requests;
        var routing = Routing.Requests;
        var list = Server.ListRequests;

        viewModel.SortOptions.Single(option => option.Value == MAUI.Models.ResultSortOrder.Name).SelectCommand.Execute(null);
        viewModel.FuelFilterOptions.Single(option => option.FuelType == MAUI.Models.FuelType.Diesel).SelectCommand.Execute(null);

        Assert.Equal(nominatim, Nominatim.Requests);
        Assert.Equal(routing, Routing.Requests);
        Assert.Equal(list, Server.ListRequests);
        Assert.Equal(Names(viewModel).OrderBy(name => name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("de-DE"), true)), Names(viewModel));
    }
}
