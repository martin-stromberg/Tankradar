namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Die Preiszeilen folgen der Sortenreihenfolge der Optionen; abgewählte Sorten fehlen.
/// </summary>
public class SearchE2ETests_FuelOrder : SearchE2ETestBase
{
    /// <summary>
    /// Prüft die Standardreihenfolge der Preiszeilen (Super E5, Super E10, Diesel).
    /// </summary>
    [Fact]
    public void DefaultOrder_ShowsE5ThenE10ThenDiesel()
    {
        RunWithDiagnostics(() =>
        {
            OpenSearch();

            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal(["Super E5", "Super E10", "Diesel"], FuelLabelsOf("Alpha Tankstelle"));
        });
    }

    /// <summary>
    /// Prüft, dass eine geänderte Reihenfolge und eine abgewählte Sorte nach erneuter Suche in der Liste erscheinen.
    /// </summary>
    [Fact]
    public void ChangedOrderAndDeselectedFuel_AreReflectedAfterNewSearch()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            MoveFuelType("Diesel", "MoveUp");
            MoveFuelType("Diesel", "MoveUp");
            ToggleFuelType("SuperE10");
            WaitUntil(() => !IsFuelTypeSelected("SuperE10"), "Super E10 wurde nicht abgewählt.");
            Assert.Equal(["Diesel", "SuperE5", "SuperE10"], ReadFuelTypeOrder());

            OpenSearch();
            Submit();

            WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
            Assert.Equal(["Diesel", "Super E5"], FuelLabelsOf("Alpha Tankstelle"));
            Assert.Equal(["Super E5"], FuelLabelsOf("Beta Tankstelle"));
            Assert.False(Exists("Search.Price.SuperE10"));
        });
    }

    private IReadOnlyList<string> FuelLabelsOf(string station)
    {
        return TextsByStation("Search.Fuel.SuperE5", "Search.Fuel.SuperE10", "Search.Fuel.Diesel")
            .Where(entry => entry.Station == station)
            .Select(entry => entry.Text)
            .ToList();
    }
}
