using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Belegfotos: Die Test-Fotoauswahl (<c>TANKATLAS_TEST_RECEIPT_PHOTO</c>) liefert eine zur Laufzeit erzeugte PNG-Datei; das Foto liegt lokal unter
/// <c>receipts/</c>, „Beleg anzeigen“ blendet es ein, und das Löschen des Vorgangs entfernt die Datei.
/// </summary>
public class TankbookEntriesE2ETests_Receipt : TankbookE2ETestBase
{
    private static readonly string PhotoPath = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E", "beleg-" + Guid.NewGuid().ToString("N") + ".png");

    /// <summary>
    /// Erzeugt die Testdatei und startet die App mit der Test-Fotoauswahl.
    /// </summary>
    public TankbookEntriesE2ETests_Receipt()
        : base(CreateEnvironment())
    {
    }

    /// <summary>
    /// Prüft Auswahl, Vorschau, Ablage, Anzeige und Löschen des Belegfotos.
    /// </summary>
    [Fact]
    public void ReceiptPhoto_IsStoredShownAndDeletedWithEntry()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Press("TankbookEntries.Add");
            Assert.False(Exists("TankbookEntries.Form.CapturePhoto"));
            WaitForAutomationId("TankbookEntries.Form.PickPhoto");
            WaitForName("TankbookEntries.Form.PickPhoto", "Foto auswählen");

            Press("TankbookEntries.Form.PickPhoto");
            WaitForAutomationId("TankbookEntries.Form.RemovePhoto");
            WaitForAutomationId("TankbookEntries.Form.PhotoPreview");
            Tankbook.FillEntryForm("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.SaveEntryForm();

            var file = Assert.Single(TankbookTestDatabase.ListReceiptFiles(TestDataDirectory));
            Assert.Matches("^[0-9a-f]{32}\\.png$", file);
            Assert.Equal(file, Assert.Single(TankbookTestDatabase.ReadEntries(DatabasePath)).ReceiptPhotoFileName);
            Assert.Equal(File.ReadAllBytes(PhotoPath), File.ReadAllBytes(Path.Combine(TestDataDirectory, "receipts", file)));

            WaitForName("TankbookEntry.Item.ShowReceipt", "Beleg anzeigen");
            Press("TankbookEntry.Item.ShowReceipt");
            WaitForAutomationId("TankbookEntry.Item.Receipt");
            WaitForName("TankbookEntry.Item.ShowReceipt", "Beleg ausblenden");
            Assert.False(Exists("TankbookEntry.Item.ReceiptMissing"));

            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            Press("TankbookEntry.Item.DeleteConfirm");
            WaitForAutomationId("TankbookEntries.Empty");
            Assert.Empty(TankbookTestDatabase.ListReceiptFiles(TestDataDirectory));
        });
    }

    /// <inheritdoc />
    protected override void Cleanup()
    {
        File.Delete(PhotoPath);
        base.Cleanup();
    }

    private static Dictionary<string, string> CreateEnvironment()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PhotoPath)!);
        TankbookTestDatabase.WriteTestPng(PhotoPath);
        return new Dictionary<string, string> { [TestDataPaths.TestReceiptPhotoEnvironmentVariable] = PhotoPath };
    }

    /// <summary>
    /// Wartet, bis das Element den Namen trägt. Suche und Lesen des Namens laufen innerhalb von <see cref="TransientWait"/>: Direkt nach dem Neuaufbau des Elements
    /// meldet die UI-Automation die Eigenschaft kurz nicht (PropertyNotSupportedException bei der Abfrage mit Standardwert) oder das Element ist zwischen Suche und Lesen nicht mehr
    /// verfügbar (ElementNotAvailableException); beides gilt wie ein UIA-Timeout als „noch nicht erfüllt“, gewartet wird bis zur Frist.
    /// </summary>
    /// <param name="automationId">Die AutomationId des Elements.</param>
    /// <param name="expected">Der erwartete Name.</param>
    private void WaitForName(string automationId, string expected)
    {
        var last = string.Empty;
        WaitUntil(
            () =>
            {
                last = FindByAutomationId(automationId)?.Properties.Name.ValueOrDefault ?? string.Empty;
                return last == expected;
            },
            $"Das Element '{automationId}' trägt nicht den Namen '{expected}', zuletzt: '{last}'.");
    }
}
