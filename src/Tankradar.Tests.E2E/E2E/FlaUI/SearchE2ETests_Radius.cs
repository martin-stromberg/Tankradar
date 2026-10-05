namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Der Suchradius wird als Chip aus sinnvollen Stufen (2, 5, 10, 15, 25 km) gewählt und wirkt; die Eingabeprüfung vor dem Abruf
/// ist durch Unit- und Integrationstests abgedeckt, da die Oberfläche nur gültige Stufen anbietet.
/// </summary>
public class SearchE2ETests_Radius : SearchE2ETestBase
{
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
    /// Prüft, dass 10 km zusätzlich Gamma (rund 8 km) liefert und genau der Radius 10 gesendet wird.
    /// </summary>
    [Fact]
    public void Radius10_ShowsGammaToo()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SetRadius("10");

            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle");
            Assert.Equal(10, Server.LastListRadius);
        });
    }

    /// <summary>
    /// Prüft, dass genau eine Radiusstufe ausgewählt ist (Standard 5 km) und die Wahl einer anderen Stufe die bisherige abwählt.
    /// </summary>
    [Fact]
    public void RadiusChips_AreExclusiveAndDefaultIsFive()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Assert.Equal("5", ReadRadius());

            SetRadius("15");

            Assert.Equal("15", ReadRadius());
            Assert.False(IsChipSelected("Search.Radius.5"));
            Assert.Equal(0, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass ein Wechsel der Radiusstufe nach einer Suche erst mit dem nächsten Suchen einen neuen Abruf mit dem neuen Radius auslöst.
    /// </summary>
    [Fact]
    public void ChangingRadiusAfterSearch_SearchesAgainWithNewRadius()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");

            SetRadius("2");
            Assert.Equal(1, Server.ListRequests);
            Submit();

            WaitUntil(() => Server.ListRequests == 2, "Der zweite Abruf wurde nicht ausgelöst.");
            Assert.Equal(2, Server.LastListRadius);
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        });
    }
}
