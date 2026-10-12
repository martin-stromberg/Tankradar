using System.Globalization;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Monatskosten: Kalendermonate nach lokalem Zeitpunkt, Monatsgrenze 31.12. 23:59 / 01.01. 00:00 und leerer Monat dazwischen.
/// </summary>
public class TankbookStatisticsE2ETests_Months : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft die Monatsgrenze (letzte Minute des Vormonats und erste Minute des Monats) mit getrennten Summen, den leeren Monat dazwischen mit 0,00 € und die Gesamtkosten.
    /// Die Reihe läuft bis zum aktuellen Monat; geprüft wird das Ende der Liste (älteste Monate).
    /// </summary>
    [Fact]
    public void MonthBoundary_SplitsSumsAndShowsEmptyMonth()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            var current = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var oldest = current.AddMonths(-5);
            var beforeBoundary = current.AddMonths(-3);
            var afterBoundary = current.AddMonths(-2);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", oldest.AddDays(14).AddHours(8), 10_500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", afterBoundary.AddMinutes(-1), 11_000, 30m, 50m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", afterBoundary, 11_500, 30m, 60m);

            Tankbook.OpenEntries("Golf");

            var german = CultureInfo.GetCultureInfo("de-DE");
            string[] oldestMonths =
            [
                $"{afterBoundary.ToString("MMMM yyyy", german)}: 60,00 €",
                $"{beforeBoundary.ToString("MMMM yyyy", german)}: 50,00 €",
                $"{current.AddMonths(-4).ToString("MMMM yyyy", german)}: 0,00 €",
                $"{oldest.ToString("MMMM yyyy", german)}: 74,36 €",
            ];
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
