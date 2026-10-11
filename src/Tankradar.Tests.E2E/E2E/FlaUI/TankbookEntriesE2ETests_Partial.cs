namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Teilbetankung: keine eigene Verbrauchsangabe, die Menge zählt beim nächsten vollen Vorgang mit.
/// </summary>
public class TankbookEntriesE2ETests_Partial : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft eine Teilbetankung zwischen zwei vollen Vorgängen: (20 L + 10 L) auf 500 km ergeben 6,00 L/100 km.
    /// </summary>
    [Fact]
    public void PartialBetweenFullEntries_IsAddedToNextFullEntry()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Petrol", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");

            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "10", "1,859", string.Empty, "10800", isPartial: true);
            Tankbook.CreateEntry("Alpha Tankstelle", "20", "1,859", string.Empty, "11000");

            WaitForTexts(
                "TankbookEntry.Item.Consumption",
                "Verbrauch 6,00 L/100 km",
                "Teilbetankung: zählt beim nächsten vollen Vorgang mit",
                "Noch kein Verbrauch: erster voller Vorgang");
            WaitForTexts("TankbookEntry.Item.Partial", "Teilbetankung");
            WaitForTexts("TankbookEntry.Item.Distance", "Strecke 500 km");
            Assert.Equal([false, true, false], TankbookTestDatabase.ReadEntries(DatabasePath).Select(entry => entry.IsPartial));
        });
    }
}
