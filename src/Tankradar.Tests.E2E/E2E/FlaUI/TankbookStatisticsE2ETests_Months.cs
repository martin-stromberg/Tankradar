namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Monatskosten: Kalendermonate nach lokalem Zeitpunkt, Monatsgrenze 31.12. 23:59 / 01.01. 00:00 und leerer Monat dazwischen.
/// </summary>
public class TankbookStatisticsE2ETests_Months : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft Dezember 2025 (23:59) und Januar 2026 (00:00) getrennt, den leeren November mit 0,00 € und die Gesamtkosten.
    /// Die Reihe läuft bis zum aktuellen Monat; geprüft wird das Ende der Liste (älteste Monate).
    /// </summary>
    [Fact]
    public void MonthBoundary_SplitsSumsAndShowsEmptyMonth()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(2025, 10, 15, 8, 0, 0), 10_500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(2025, 12, 31, 23, 59, 0), 11_000, 30m, 50m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(2026, 1, 1, 0, 0, 0), 11_500, 30m, 60m);

            Tankbook.OpenEntries("Golf");

            string[] oldestMonths = ["Januar 2026: 60,00 €", "Dezember 2025: 50,00 €", "November 2025: 0,00 €", "Oktober 2025: 74,36 €"];
            Tankbook.WaitUntil(
                () =>
                {
                    var rows = ReadTexts("TankbookEntries.Statistics.Fuel.Month");
                    return rows.Count >= oldestMonths.Length && rows.Skip(rows.Count - oldestMonths.Length).SequenceEqual(oldestMonths);
                },
                $"Die Monatskosten zeigen nicht [{string.Join(" | ", oldestMonths)}] am Ende, gelesen: [{string.Join(" | ", ReadTexts("TankbookEntries.Statistics.Fuel.Month"))}].");
            Tankbook.WaitForStatistic("Fuel", "TotalCost", "184,36 €");
            Assert.All(
                ReadTexts("TankbookEntries.Statistics.Fuel.Month").SkipLast(oldestMonths.Length),
                text => Assert.EndsWith(": 0,00 €", text, StringComparison.Ordinal));
        });
    }
}
