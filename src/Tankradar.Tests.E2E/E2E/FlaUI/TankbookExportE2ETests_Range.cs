namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Zeitraums beim Export: Auswahlchips filtern die Zeilen, bei „Eigener Zeitraum“ erscheinen die Datumsfelder und ein Start nach dem Ende wird gemeldet.
/// </summary>
public class TankbookExportE2ETests_Range : TankbookExportE2ETestBase
{
    /// <summary>
    /// Prüft „Alle Vorgänge“, „Letztes Jahr“ und „Dieses Jahr“ anhand der exportierten Zeilen (Vorgänge im Vorvorjahr, im Vorjahr und im laufenden Jahr).
    /// </summary>
    [Fact]
    public void Export_PresetRanges_FilterRows()
    {
        RunWithDiagnostics(() =>
        {
            var year = DateTime.Now.Year;
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(year - 2, 6, 15, 8, 0, 0), 10500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(year - 1, 3, 10, 8, 0, 0), 11000, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(year - 1, 9, 20, 8, 0, 0), 11500, 40m, 74.36m);
            TankbookTestDatabase.AddEntry(DatabasePath, "Golf", new DateTime(year, 1, 1, 0, 1, 0), 12000, 40m, 74.36m);
            Tankbook.OpenEntries("Golf");

            var all = ReadCsvLines(ExportAndWaitForFile());
            Assert.Equal(5, all.Length);

            ChooseRange("lastYear");
            var lastYear = ReadCsvLines(ExportAndWaitForFile());
            Assert.Equal([$"10.03.{year - 1}", $"20.09.{year - 1}"], lastYear.Skip(1).Select(line => line.Split(';')[0]));
            Assert.Equal("500", lastYear[1].Split(';')[10]);

            ChooseRange("thisYear");
            var thisYear = ReadCsvLines(ExportAndWaitForFile());
            Assert.Equal([$"01.01.{year}"], thisYear.Skip(1).Select(line => line.Split(';')[0]));

            ChooseRange("all");
            Assert.Equal(5, ReadCsvLines(ExportAndWaitForFile()).Length);
        });
    }

    /// <summary>
    /// Prüft, dass die Datumsfelder nur bei „Eigener Zeitraum“ erscheinen und ein Startdatum nach dem Enddatum eine Meldung am Feld zeigt, ohne Datei.
    /// </summary>
    [Fact]
    public void Export_CustomRange_ShowsDateFields_AndRejectsStartAfterEnd()
    {
        RunWithDiagnostics(() =>
        {
            OpenEntriesOfNewVehicle("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");

            Assert.False(Exists("TankbookEntries.Export.From"));
            Assert.False(Exists("TankbookEntries.Export.To"));
            ChooseRange("custom");
            WaitForAutomationId("TankbookEntries.Export.From");
            WaitForAutomationId("TankbookEntries.Export.To");
            var from = ReadDate("TankbookEntries.Export.From");
            var to = ReadDate("TankbookEntries.Export.To");
            Assert.True(from < to);
            ChooseRange("thisYear");
            WaitUntil(() => !Exists("TankbookEntries.Export.From"), "Die Datumsfelder sind nach dem Wechsel des Zeitraums weiterhin sichtbar.");

            ChooseRange("custom");
            var end = PickFifteenthBefore("TankbookEntries.Export.To", new DateTime(from.Year, from.Month, 1));
            Assert.True(end < ReadDate("TankbookEntries.Export.From"));
            ClearExportFiles();
            StartExport();

            WaitUntil(() => FindByAutomationId("TankbookEntries.Export.RangeError")?.Name == "Das Startdatum liegt nach dem Enddatum.", "Die Meldung zum Zeitraum fehlt.");
            Assert.Empty(ExportFiles());
            Assert.Equal(string.Empty, ReadStatus());
        });
    }
}
