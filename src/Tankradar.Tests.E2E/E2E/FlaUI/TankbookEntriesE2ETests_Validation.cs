namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test der Prüfungen beim Erfassen: Pflichtfelder, fehlende Werte, Abweichung der Beträge und Kilometerstand unter dem Vorgänger.
/// </summary>
public class TankbookEntriesE2ETests_Validation : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft die Feldfehler nacheinander; nichts wird gespeichert, bis die Eingabe gültig ist.
    /// </summary>
    [Fact]
    public void InvalidInput_ShowsFieldErrorsAndSavesNothing()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            TankbookTestDatabase.AddEntries(DatabasePath, "Golf", 1, firstOdometerKm: 10_500);
            Tankbook.OpenEntries("Golf");
            WaitForTexts("TankbookEntry.Item.Station", "Alpha Tankstelle");

            Press("TankbookEntries.Add");
            Press("TankbookEntries.Form.Save");
            Assert.Equal("Bitte gib eine Tankstelle oder einen Ladeort ein oder wähle eine bekannte Tankstelle.", WaitForAutomationId("TankbookEntries.Form.StationError").Name);
            Assert.Equal("Bitte gib mindestens zwei der drei Werte Menge, Preis pro Liter und Gesamtbetrag an.", WaitForAutomationId("TankbookEntries.Form.TotalAmountError").Name);
            Assert.Equal("Bitte gib den Kilometerstand ein.", WaitForAutomationId("TankbookEntries.Form.OdometerError").Name);

            Tankbook.FillEntryForm("Beta Tankstelle", "40", "1,859", "75,00", "10600");
            Press("TankbookEntries.Form.Save");
            WaitUntil(
                () => FindByAutomationId("TankbookEntries.Form.TotalAmountError")?.Name == "Menge × Preis ergibt 74,36 €, nicht 75,00 €. Bitte prüfe die Werte.",
                "Die Abweichung der Beträge wird nicht gemeldet.");

            Tankbook.FillEntryForm(string.Empty, "40", "1,859", string.Empty, "10400");
            Press("TankbookEntries.Form.Save");
            WaitUntil(
                () => FindByAutomationId("TankbookEntries.Form.OdometerError")?.Name == "Der Kilometerstand darf nicht unter 10.500 km (vorheriger Vorgang) liegen.",
                "Der Kilometerstand unter dem Vorgänger wird nicht gemeldet.");
            Assert.False(Exists("TankbookEntries.Form.TotalAmountError"));
            Assert.Equal(1, TankbookTestDatabase.CountEntries(DatabasePath));

            Type("TankbookEntries.Form.Odometer", "10600");
            Tankbook.SaveEntryForm();
            WaitForTexts("TankbookEntry.Item.Station", "Beta Tankstelle", "Alpha Tankstelle");
            Assert.Equal(2, TankbookTestDatabase.CountEntries(DatabasePath));
        });
    }
}
