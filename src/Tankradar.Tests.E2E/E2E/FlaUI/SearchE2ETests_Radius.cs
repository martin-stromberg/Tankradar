namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Der Suchradius von 1 bis 25 km wirkt; ungültige Werte werden vor jedem Abruf abgewiesen.
/// </summary>
public class SearchE2ETests_Radius : SearchE2ETestBase
{
    private const string RadiusInvalid = "Bitte einen Radius von 1 bis 25 km eingeben.";

    /// <summary>
    /// Prüft, dass 25 km Gamma und Delta, aber nicht Epsilon liefert und genau der Radius 25 gesendet wird.
    /// </summary>
    [Fact]
    public void Radius25_ShowsFarStationsButNotEpsilon()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SetRadius("25");

            Submit();

            WaitForStationNames("Delta Tankstelle", "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle");
            Assert.Equal(25, Server.LastListRadius);
            Assert.False(Exists("Search.StatusMessage"));
        });
    }

    /// <summary>
    /// Prüft, dass ein Radius von 1 km nur die nächste Tankstelle liefert.
    /// </summary>
    [Fact]
    public void Radius1_ShowsOnlyNearestStation()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SetRadius("1");

            Submit();

            WaitForStationNames("Alpha Tankstelle");
            Assert.Equal(1, Server.LastListRadius);
        });
    }

    /// <summary>
    /// Prüft, dass ungültige Radien eine Meldung zeigen und keine Anfrage an den Preisdienst auslösen.
    /// </summary>
    /// <param name="radius">Die Eingabe.</param>
    [Theory]
    [InlineData("26")]
    [InlineData("0")]
    public void InvalidRadius_ShowsMessageAndSendsNoRequest(string radius)
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SetRadius(radius);

            Submit();

            WaitForStatusMessage(RadiusInvalid);
            Assert.Equal(0, Server.TotalRequests);
            Assert.False(Exists("Search.Station.Name"));
        });
    }

    /// <summary>
    /// Prüft, dass nach einer ungültigen Eingabe die Korrektur auf einen gültigen Radius die Suche ermöglicht und die Meldung verschwindet.
    /// </summary>
    [Fact]
    public void InvalidRadiusThenValid_SearchesAndClearsMessage()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SetRadius("26");
            Submit();
            WaitForStatusMessage(RadiusInvalid);

            SetRadius("5");
            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            WaitUntil(() => !Exists("Search.StatusMessage"), "Die Meldung zum Radius wurde nicht entfernt.");
            Assert.Equal(1, Server.ListRequests);
        });
    }
}
