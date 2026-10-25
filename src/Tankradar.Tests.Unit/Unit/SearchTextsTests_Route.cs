using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Texte der Routensuche: Beschriftungen, Formate, Quellenangabe und Meldungen für jeden Fehlergrund und jeden Ausgang.
/// </summary>
public class SearchTextsTests_Route : BaseTest
{
    /// <summary>
    /// Prüft die Beschriftung der Suchart „Route“ und dass jede Suchart und jede Startart eine nicht leere Beschriftung hat.
    /// </summary>
    [Fact]
    public void GetModeLabel_Route_IsRoute()
    {
        Assert.Equal("Route", SearchTexts.GetModeLabel(SearchMode.Route));
        Assert.All(Enum.GetValues<SearchMode>(), mode => Assert.NotEmpty(SearchTexts.GetModeLabel(mode)));
        Assert.NotEmpty(SearchTexts.RouteStartCurrentLocation);
        Assert.NotEmpty(SearchTexts.RouteStartAddress);
        Assert.Equal(2, Enum.GetValues<RouteStartKind>().Length);
    }

    /// <summary>
    /// Prüft die Quellenangabe für OpenRouteService und OpenStreetMap.
    /// </summary>
    [Fact]
    public void RoutingAttribution_NamesBothSources()
    {
        Assert.Equal("Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende", SearchTexts.RoutingAttribution);
    }

    /// <summary>
    /// Prüft die Formate der Entfernung und des Abstands zur Route in deutscher Schreibweise.
    /// </summary>
    [Fact]
    public void Formats_UseGermanNotation()
    {
        Assert.Equal("12,3 km ab Start", SearchTexts.FormatRouteDistance(12.34));
        Assert.Equal("0,8 km abseits der Route", SearchTexts.FormatRouteOffset(0.8));
        Assert.Equal(
            "Von Berlin nach Hamburg, 289 km, 12 Tankstellen im Abstand von 2 km zur Route",
            SearchTexts.FormatRouteSummary("Berlin", "Hamburg", 288.6, 12, 2));
        Assert.Contains("1 Tankstelle ", SearchTexts.FormatRouteSummary("A", "B", 10, 1, 5), StringComparison.Ordinal);
        Assert.Equal("Route von A nach B eingezeichnet, 3 Tankstellen", SearchTexts.FormatRouteMapHint("A", "B", 3));
    }

    /// <summary>
    /// Prüft die Einzahl im Kartenhinweis („1 Tankstelle“) und die richtige Mehrzahl bei null Treffern.
    /// </summary>
    [Fact]
    public void FormatRouteMapHint_UsesSingularForOneStation()
    {
        Assert.Equal("Route von A nach B eingezeichnet, 1 Tankstelle", SearchTexts.FormatRouteMapHint("A", "B", 1));
        Assert.Equal("Route von A nach B eingezeichnet, 0 Tankstellen", SearchTexts.FormatRouteMapHint("A", "B", 0));
    }

    /// <summary>
    /// Prüft, dass kurze Routen mit einer Nachkommastelle angegeben werden (nicht „0 km“) und längere in ganzen Kilometern.
    /// </summary>
    /// <param name="lengthKm">Die Länge in Kilometern.</param>
    /// <param name="expected">Der erwartete Text.</param>
    [Theory]
    [InlineData(0.4, "0,4 km")]
    [InlineData(8.04, "8,0 km")]
    [InlineData(9.94, "9,9 km")]
    [InlineData(9.96, "10 km")]
    [InlineData(40.0, "40 km")]
    [InlineData(288.6, "289 km")]
    public void FormatRouteLength_ShowsDecimalBelowTenKilometers(double lengthKm, string expected)
    {
        Assert.Equal(expected, SearchTexts.FormatRouteLength(lengthKm));
        Assert.Contains(expected, SearchTexts.FormatRouteSummary("A", "B", lengthKm, 2, 2), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft den Hinweis zur Abfragegrenze: Er nennt die Routenlänge, den Abstand und die bei diesem Abstand durchsuchbare Länge.
    /// </summary>
    [Fact]
    public void FormatRouteLimit_NamesRouteLengthDistanceAndSearchableLength()
    {
        var text = SearchTexts.FormatRouteLimit(new RouteLimit(5, 200.2, 114));

        Assert.Contains("200 km", text, StringComparison.Ordinal);
        Assert.Contains("5 km Abstand", text, StringComparison.Ordinal);
        Assert.Contains("bis 114 km", text, StringComparison.Ordinal);
        Assert.Contains("kürzere Strecke", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass die durchsuchbare Länge abgerundet wird (nie größer als tatsächlich möglich).
    /// </summary>
    [Fact]
    public void FormatRouteLimit_RoundsSearchableLengthDown()
    {
        var text = SearchTexts.FormatRouteLimit(new RouteLimit(5, 200.2, 85.9));

        Assert.Contains("bis 85 km", text, StringComparison.Ordinal);
        Assert.DoesNotContain("86", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass bei 1 km Abstand kein kleinerer Abstand vorgeschlagen wird, bei größerem Abstand aber schon.
    /// </summary>
    [Fact]
    public void FormatRouteLimit_AtOneKm_OmitsSmallerDistanceHint()
    {
        var atOne = SearchTexts.FormatRouteLimit(new RouteLimit(1, 400, 400.5));
        var atTwo = SearchTexts.FormatRouteLimit(new RouteLimit(2, 400, 260));

        Assert.DoesNotContain("kleineren Abstand", atOne, StringComparison.Ordinal);
        Assert.EndsWith("Wähle eine kürzere Strecke.", atOne, StringComparison.Ordinal);
        Assert.Contains("kleineren Abstand", atTwo, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass die Meldung der Abfragegrenze nur mit Angaben erscheint und sonst (Routingdienst lehnt ab) der allgemeine Hinweis gilt.
    /// </summary>
    [Fact]
    public void GetRouteMessage_RouteTooLong_UsesLimitOnlyWhenPresent()
    {
        Assert.Equal(SearchTexts.FormatRouteLimit(new RouteLimit(2, 300, 131)), SearchTexts.GetRouteMessage(RouteSearchResult.FailLimit(new RouteLimit(2, 300, 131))));
        Assert.Equal(SearchTexts.RouteTooLongForService, SearchTexts.GetRouteMessage(RouteSearchResult.Fail(RouteSearchStatus.RouteTooLong)));
        Assert.Equal(SearchTexts.RouteTooLongForService, SearchTexts.GetRoutingMessage(RoutingFailure.RouteTooLong));
    }

    /// <summary>
    /// Prüft den Fortschrittstext für jeden Abschnitt.
    /// </summary>
    [Fact]
    public void FormatRouteProgress_CoversAllPhases()
    {
        Assert.Equal(SearchTexts.RouteProgressPlaces, SearchTexts.FormatRouteProgress(new RouteSearchProgress(RouteSearchPhase.ResolvingPlaces, 0, 0)));
        Assert.Equal(SearchTexts.RouteProgressRoute, SearchTexts.FormatRouteProgress(new RouteSearchProgress(RouteSearchPhase.CalculatingRoute, 0, 0)));
        Assert.Equal("Tankstellen werden gesucht (3 von 9) …", SearchTexts.FormatRouteProgress(new RouteSearchProgress(RouteSearchPhase.SearchingStations, 3, 9)));
        Assert.All(Enum.GetValues<RouteSearchPhase>(), phase => Assert.NotEmpty(SearchTexts.FormatRouteProgress(new RouteSearchProgress(phase, 1, 2))));
    }

    /// <summary>
    /// Prüft, dass jeder Fehlergrund des Routingdienstes außer „kein Fehler“ eine Meldung hat und keine Rohtexte enthält.
    /// </summary>
    [Fact]
    public void GetRoutingMessage_CoversAllFailures()
    {
        Assert.Equal(string.Empty, SearchTexts.GetRoutingMessage(RoutingFailure.None));
        foreach (var failure in Enum.GetValues<RoutingFailure>().Where(failure => failure != RoutingFailure.None))
        {
            var message = SearchTexts.GetRoutingMessage(failure);
            Assert.NotEmpty(message);
            Assert.DoesNotContain(failure.ToString(), message, StringComparison.Ordinal);
        }

        Assert.Equal(
            "Der Routendienst ist gerade ausgelastet oder das Tageskontingent ist aufgebraucht. Bitte später erneut versuchen.",
            SearchTexts.GetRoutingMessage(RoutingFailure.QuotaExceeded));
        Assert.Equal("Routensuche nicht verfügbar. Die übrige Suche funktioniert weiterhin.", SearchTexts.GetRoutingMessage(RoutingFailure.ApiKeyMissing));
    }

    /// <summary>
    /// Prüft, dass jeder Ausgang der Routensuche außer „Erfolg“ eine Meldung hat.
    /// </summary>
    [Fact]
    public void GetRouteMessage_CoversAllStatuses()
    {
        var success = RouteSearchResult.Success(
            new RouteProfile([new GeoPosition(52, 13), new GeoPosition(52.1, 13)]),
            new GeoPosition(52, 13),
            new GeoPosition(52.1, 13),
            null,
            null,
            []);
        Assert.Equal(string.Empty, SearchTexts.GetRouteMessage(success));

        foreach (var status in Enum.GetValues<RouteSearchStatus>().Where(status => status is not (RouteSearchStatus.Success or RouteSearchStatus.GeocodingFailed or RouteSearchStatus.RoutingFailed or RouteSearchStatus.PriceFailed)))
        {
            Assert.NotEmpty(SearchTexts.GetRouteMessage(RouteSearchResult.Fail(status)));
        }

        Assert.Equal(SearchTexts.AddressNotFound, SearchTexts.GetRouteMessage(RouteSearchResult.FailGeocoding(GeocodingStatus.NotFound)));
        Assert.Equal(SearchTexts.GeocodingUnavailable, SearchTexts.GetRouteMessage(RouteSearchResult.FailGeocoding(GeocodingStatus.Unavailable)));
        Assert.Equal(SearchTexts.RoutingQuotaExceeded, SearchTexts.GetRouteMessage(RouteSearchResult.FailRouting(RoutingFailure.QuotaExceeded)));
        Assert.Equal(SearchTexts.GetFailureMessage(PriceFailure.Rejected, false), SearchTexts.GetRouteMessage(RouteSearchResult.FailPrice(PriceFailure.Rejected)));
        Assert.Equal(SearchTexts.RouteStartNotFound, SearchTexts.GetRouteMessage(RouteSearchResult.Fail(RouteSearchStatus.StartNotFound)));
        Assert.Equal(SearchTexts.RouteDestinationNotFound, SearchTexts.GetRouteMessage(RouteSearchResult.Fail(RouteSearchStatus.DestinationNotFound)));
    }

    /// <summary>
    /// Prüft die Meldungen zu ungültigen Eingaben von Start und Ziel.
    /// </summary>
    [Fact]
    public void GetRouteInputMessage_PrefixesStartOrDestination()
    {
        Assert.Equal(string.Empty, SearchTexts.GetRouteInputMessage(true, AddressInputError.None));
        Assert.Equal("Start: " + SearchTexts.AddressEmpty, SearchTexts.GetRouteInputMessage(true, AddressInputError.Empty));
        Assert.Equal("Ziel: " + SearchTexts.AddressTooShort, SearchTexts.GetRouteInputMessage(false, AddressInputError.TooShort));
    }

    /// <summary>
    /// Prüft die entdoppelte Beschriftung der Schaltfläche zum Nachladen: „weitere“ kommt nur einmal vor.
    /// </summary>
    [Fact]
    public void FormatShowMore_ContainsWordWeitereOnlyOnce()
    {
        var text = SearchTexts.FormatShowMore(281);

        Assert.Equal("Weitere anzeigen (281 übrig)", text);
        Assert.Single(text.Split(' ', StringSplitOptions.RemoveEmptyEntries), word => word.Equals("weitere", StringComparison.OrdinalIgnoreCase));
    }
}
