namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Tests des Bereichs „In der Nähe“ der Startseite: Sichtbarkeit, Sortierung, Filter und Öffnen der Detailansicht.
/// </summary>
public class HomeE2ETests_Nearby : HomeE2ETestBase
{
    /// <summary>
    /// Prüft, dass der Bereich standardmäßig fehlt und nach dem Einschalten Alpha und Beta nach Entfernung zeigt.
    /// </summary>
    [Fact]
    public void NearbySwitch_ShowsSectionWithStationsByDistance()
    {
        RunWithDiagnostics(() =>
        {
            OpenHome();
            Assert.False(Exists("Nearby.Heading"));

            EnableNearby();
            OpenHome();

            WaitForAutomationId("Nearby.Heading");
            WaitForTexts("Nearby.Station.Name", "Alpha Tankstelle", "Beta Tankstelle");
            Assert.All(SafeTexts("Nearby.Station.Distance"), text => Assert.Matches(DistancePattern, text));
            Assert.False(Exists("Favorites.Station.Name"));
            Assert.Equal(2, FindAllOrdered("Nearby.Station.Open").Count);
        });
    }

    /// <summary>
    /// Prüft, dass der Filter Diesel nur Alpha zeigt und „Alle“ beide Tankstellen zurückbringt.
    /// </summary>
    [Fact]
    public void FuelFilter_NarrowsAndRestoresEntries()
    {
        RunWithDiagnostics(() =>
        {
            EnableNearby();
            OpenHome();
            WaitForTexts("Nearby.Station.Name", "Alpha Tankstelle", "Beta Tankstelle");

            SelectChip("Nearby.Filter.Diesel");
            WaitForTexts("Nearby.Station.Name", "Alpha Tankstelle");

            SelectChip("Nearby.Filter.All");
            WaitForTexts("Nearby.Station.Name", "Alpha Tankstelle", "Beta Tankstelle");
        });
    }

    /// <summary>
    /// Prüft, dass das Antippen einer Tankstelle im Bereich die Detailansicht öffnet.
    /// </summary>
    [Fact]
    public void OpenNearbyStation_ShowsDetail()
    {
        RunWithDiagnostics(() =>
        {
            EnableNearby();
            OpenHome();
            WaitForTexts("Nearby.Station.Name", "Alpha Tankstelle", "Beta Tankstelle");

            FindAllOrdered("Nearby.Station.Open")[1].Patterns.Invoke.Pattern.Invoke();

            WaitUntil(() => FindByAutomationId("Detail.Name")?.Name == "Beta Tankstelle", "Die Detailansicht zeigt nicht 'Beta Tankstelle'.");
        });
    }
}
