namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Ablauf der Suche nach Adresse, Ort oder PLZ gegen die Mock-Dienste (Preisdienst und Nominatim): Suchart wählen, Adresse eingeben,
/// absenden, Ergebnisliste rund um die Adresse, Quellenangabe, Unabhängigkeit vom Standort. Produktive Endpunkte werden nie berührt.
/// </summary>
public class SearchE2ETests_Address : SearchE2ETestBase
{
    /// <summary>
    /// Prüft den Grundablauf: Adresse eingeben, absenden, Tankstellen rund um den aufgelösten Ort mit Entfernung zur Adresse, Ortshinweis und Quellenangabe.
    /// </summary>
    [Fact]
    public void AddressSearch_ShowsStationsAroundAddress()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SelectAddressMode();
            Assert.Equal("Geodaten © OpenStreetMap-Mitwirkende", WaitForAutomationId("Search.Attribution").Name);
            SetAddress("Oranienburg");
            SetRadius("1");

            Submit();

            WaitForStationNames("Epsilon Tankstelle");
            Assert.Equal(["0,0 km"], ReadTexts("Search.Station.Distance"));
            WaitForResolvedPlace("Suche rund um: Oranienburg, Landkreis Oberhavel, Brandenburg, Deutschland");
            Assert.Equal("Oranienburg", Geocoding.LastQuery);
            Assert.Contains("Tankatlas", Geocoding.LastUserAgent, StringComparison.Ordinal);
            Assert.Equal(52.88, Server.LastListLatitude!.Value, 4);
            Assert.Equal(1, Server.LastListRadius);
            Assert.False(Exists("Search.StatusMessage"));
        });
    }

    /// <summary>
    /// Prüft, dass die Ergebnisliste der Adresssuche Sortierung und Spritsortenfilter wie bei der Standortsuche bietet, ohne erneute Anfragen.
    /// </summary>
    [Fact]
    public void AddressSearch_SupportsSortAndFilter()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            SelectAddressMode();
            SetAddress("Berlin");
            SetRadius("25");
            Submit();
            WaitForStationNames("Delta Tankstelle", "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle");

            SelectSort("Name");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Delta Tankstelle", "Gamma Tankstelle");
            SelectFilter("Diesel");
            WaitForStationNames("Alpha Tankstelle", "Delta Tankstelle", "Gamma Tankstelle");

            Assert.Equal(1, Geocoding.Requests);
            Assert.Equal(1, Server.ListRequests);
        });
    }

    /// <summary>
    /// Prüft, dass die Adresssuche bei der Standortnutzung „Nie“ funktioniert, die Standortsuche dagegen weiterhin den Hinweis zeigt.
    /// </summary>
    [Fact]
    public void AddressSearch_WorksWithGpsNever()
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
            Assert.False(Exists("Search.StatusMessage"));

            SelectLocationMode();
            Submit();
            WaitForStatusMessage("Die Standortnutzung ist ausgeschaltet. Du kannst sie unter „Optionen“ ändern.");
            Assert.False(Exists("Search.Station.Name"));
        });
    }

    /// <summary>
    /// Prüft, dass Adressfeld und Quellenangabe nur in der Suchart „Adresse“ sichtbar sind und die Löschen-Schaltfläche das Feld leert.
    /// </summary>
    [Fact]
    public void AddressMode_ShowsFieldAttributionAndClearButtonOnlyWhenNeeded()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();
            Assert.False(Exists("Search.Address.Input"));
            Assert.False(Exists("Search.Attribution"));

            SelectAddressMode();
            Assert.True(Exists("Search.Attribution"));
            Assert.False(Exists("Search.Address.Clear"));

            SetAddress("Frankfurt");
            WaitForAutomationId("Search.Address.Clear").Patterns.Invoke.Pattern.Invoke();
            WaitUntil(() => ReadAddress().Length == 0 && !Exists("Search.Address.Clear"), "Die Eingabe wurde nicht gelöscht.");

            SelectLocationMode();
            Assert.False(Exists("Search.Attribution"));
        });
    }
}
