namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Vorgang korrigieren (Verbrauch ändert sich) und nach Rückfrage löschen; Schaltflächen tragen ihren sichtbaren Text als Namen, der Kontext steht im Hinweis.
/// </summary>
public class TankbookEntriesE2ETests_EditDelete : TankbookE2ETestBase
{
    /// <summary>
    /// Prüft Korrigieren des Kilometerstands, Abbrechen und Bestätigen des Löschens.
    /// </summary>
    [Fact]
    public void EditAndDeleteEntry_Flow()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 6,00 L/100 km", "Verbrauch 8,00 L/100 km (ab Startstand)");

            Assert.Equal("Bearbeiten", Tankbook.FindAllOrdered("TankbookEntry.Item.Edit")[0].Name);
            var hint = Tankbook.FindAllOrdered("TankbookEntry.Item.Edit")[0].Properties.HelpText.ValueOrDefault ?? string.Empty;
            Assert.StartsWith("Vorgang vom ", hint, StringComparison.Ordinal);
            Assert.EndsWith(", Alpha Tankstelle", hint, StringComparison.Ordinal);

            Tankbook.PressForEntry("TankbookEntry.Item.Edit", 0);
            Assert.Equal("Vorgang bearbeiten", WaitForAutomationId("TankbookEntries.Form.Title").Name);
            Assert.Equal("11000", WaitForAutomationId("TankbookEntries.Form.Odometer").Patterns.Value.Pattern.Value.Value);
            Assert.Equal("30,00", WaitForAutomationId("TankbookEntries.Form.Quantity").Patterns.Value.Pattern.Value.Value);
            Type("TankbookEntries.Form.Odometer", "11500");
            Tankbook.SaveEntryForm();
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 3,00 L/100 km", "Verbrauch 8,00 L/100 km (ab Startstand)");
            WaitForTexts("TankbookEntry.Item.Distance", "Strecke 1.000 km", "Strecke 500 km");

            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            var question = WaitForAutomationId("TankbookEntry.Item.DeleteQuestion").Name;
            Assert.StartsWith("Vorgang vom ", question, StringComparison.Ordinal);
            Assert.EndsWith("(Alpha Tankstelle) löschen? Das lässt sich nicht rückgängig machen.", question, StringComparison.Ordinal);
            Assert.Equal("Ja, löschen", WaitForAutomationId("TankbookEntry.Item.DeleteConfirm").Name);
            Assert.EndsWith("Alpha Tankstelle endgültig löschen", Tankbook.ReadHint("TankbookEntry.Item.DeleteConfirm"), StringComparison.Ordinal);
            Assert.Equal("Abbrechen", WaitForAutomationId("TankbookEntry.Item.DeleteCancel").Name);
            Assert.Equal("Vorgang behalten", Tankbook.ReadHint("TankbookEntry.Item.DeleteCancel"));

            Press("TankbookEntry.Item.DeleteCancel");
            WaitUntil(() => !Exists("TankbookEntry.Item.DeleteQuestion"), "Die Rückfrage blieb stehen.");
            Assert.Equal(2, TankbookTestDatabase.CountEntries(DatabasePath));

            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            Press("TankbookEntry.Item.DeleteConfirm");
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 8,00 L/100 km (ab Startstand)");
            Assert.Equal([10_500], TankbookTestDatabase.ReadEntries(DatabasePath).Select(entry => entry.OdometerKm));
        });
    }
}
