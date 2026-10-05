namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Filter nach Spritsorte und Sortierung der Ergebnisliste, einschließlich der aus den Optionen übernommenen Standardsortierung.
/// </summary>
public class SearchE2ETests_FilterSort : SearchE2ETestBase
{
    private void SearchAllStations()
    {
        OpenSearch();
        SetRadius("25");
        Submit();
        WaitForStationNames("Delta Tankstelle", "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle");
    }

    /// <summary>
    /// Prüft, dass der Dieselfilter Beta (ohne Diesel) entfernt, nach Dieselpreis sortiert und keinen neuen Abruf auslöst.
    /// </summary>
    [Fact]
    public void DieselFilter_RemovesStationsWithoutDieselAndSortsByDieselPrice()
    {
        RunWithDiagnostics(() =>
        {
            SearchAllStations();
            var requests = Server.TotalRequests;

            SelectFilter("Diesel");

            WaitForStationNames("Gamma Tankstelle", "Delta Tankstelle", "Alpha Tankstelle");
            Assert.Equal(requests, Server.TotalRequests);

            SelectFilter("All");
            WaitForStationNames("Delta Tankstelle", "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle");
            Assert.Equal(requests, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft die Sortierung nach Entfernung und nach Name ohne neuen Abruf.
    /// </summary>
    [Fact]
    public void SortOptions_ReorderListWithoutNewRequest()
    {
        RunWithDiagnostics(() =>
        {
            SearchAllStations();
            var requests = Server.TotalRequests;

            SelectSort("Distance");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Delta Tankstelle");

            SelectSort("Name");
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Delta Tankstelle", "Gamma Tankstelle");

            SelectSort("Price");
            WaitForStationNames("Delta Tankstelle", "Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle");
            Assert.Equal(requests, Server.TotalRequests);
        });
    }

    /// <summary>
    /// Prüft, dass die Standardsortierung aus den Optionen beim Öffnen der Suche vorgewählt ist und die Liste danach sortiert.
    /// </summary>
    [Fact]
    public void DefaultSortFromOptions_IsPreselected()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            SelectRadio("Settings.Sort.Name");

            OpenSearch();

            WaitUntil(() => IsChipSelected("Search.Sort.Name"), "Die Standardsortierung 'Name' ist nicht vorgewählt.");
            Assert.False(IsChipSelected("Search.Sort.Price"));
            SetRadius("25");
            Submit();
            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle", "Delta Tankstelle", "Gamma Tankstelle");
        });
    }
}
