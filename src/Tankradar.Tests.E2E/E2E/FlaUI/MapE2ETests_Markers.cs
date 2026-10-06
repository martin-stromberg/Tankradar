namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Markierungen der Karte mit Preis und Preisniveau, markierte Suchposition bei Standort- und Adresssuche, Filter und Öffnen der Detailansicht über eine Markierung.
/// </summary>
public class MapE2ETests_Markers : MapE2ETestBase
{
    /// <summary>
    /// Prüft Beschreibung, Preisniveau (günstigster Preis, hoher Preis) und Preistext der Markierungen für die zuerst gewählte Sorte (Super E5).
    /// </summary>
    [Fact]
    public void Markers_ShowPriceAndLevelOfFirstSelectedFuel()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            Assert.Equal("Alpha Tankstelle, Super E5 1,859 Euro pro Liter", WaitForAutomationId(AlphaMarker).Name);
            Assert.Equal("Günstigster Preis", ReadMarkerLevel(AlphaMarker));
            Assert.Equal("Beta Tankstelle, Super E5 1,879 Euro pro Liter", WaitForAutomationId(BetaMarker).Name);
            Assert.Equal("Hoher Preis", ReadMarkerLevel(BetaMarker));
            Assert.Equal(2, FindMarkers().Count);
        });
    }

    /// <summary>
    /// Prüft, dass der Spritsortenfilter die Karte mit der gefilterten Sorte neu aufbaut (Beta ohne Diesel entfällt, Alpha ist einzige und damit günstigste Station) und keine neue Anfrage auslöst.
    /// </summary>
    [Fact]
    public void Filter_RebuildsMarkersForFilteredFuel()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            SelectFilter("Diesel");

            WaitForMapCount("1 von 1 Station sichtbar");
            Assert.Equal("Alpha Tankstelle, Diesel 1,699 Euro pro Liter", WaitForAutomationId(AlphaMarker).Name);
            Assert.Equal("Günstigster Preis", ReadMarkerLevel(AlphaMarker));
            WaitUntil(() => !Exists(BetaMarker), "Die Markierung ohne Diesel-Preis wurde nicht entfernt.");
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass bei der Standortsuche der eigene Standort markiert ist.
    /// </summary>
    [Fact]
    public void LocationSearch_MarksOwnLocation()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            Assert.Equal("Mein Standort", WaitForAutomationId("Map.Origin").Name);
        });
    }

    /// <summary>
    /// Prüft, dass bei der Adresssuche die gesuchte Position markiert ist und der eigene Standort dafür nicht abgefragt wird.
    /// </summary>
    [Fact]
    public void AddressSearch_MarksSearchedPosition()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Gps.Never");
            OpenSearch();
            SelectAddressMode();
            SetAddress("Berlin");
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");

            ShowMapView();

            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.Equal("Gesuchte Position", WaitForAutomationId("Map.Origin").Name);
        });
    }

    /// <summary>
    /// Prüft, dass das Antippen einer Markierung die Detailansicht der Tankstelle öffnet und der Zurück-Pfeil zur Karte mit denselben Markierungen führt.
    /// </summary>
    [Fact]
    public void TapMarker_OpensDetailsAndBackReturnsToMap()
    {
        RunWithDiagnostics(() =>
        {
            SearchAndShowMap();

            WaitForAutomationId(BetaMarker).Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("Detail.Name"))?.Name == "Beta Tankstelle", "Die Detailansicht von Beta wurde nicht geöffnet.");
            WaitForAutomationId("NavigationViewBackButton").Patterns.Invoke.Pattern.Invoke();

            WaitForAutomationId("MapPage.Headline");
            WaitForMapCount("2 von 2 Stationen sichtbar");
            Assert.True(Exists(AlphaMarker));
            Assert.Equal(1, Server.ListRequests);
        });
    }
}
