using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Belegfotos und Tankbuch sind Teil der Sicherung; nach dem Wiederherstellen sind Vorgänge, Foto und Auswertung zurück.
/// </summary>
public class BackupE2ETests_Photo : BackupE2ETestBase
{
    private static readonly string PhotoPath = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E", "beleg-" + Guid.NewGuid().ToString("N") + ".png");

    /// <summary>
    /// Erzeugt das Testfoto und startet die App mit der Test-Fotoauswahl.
    /// </summary>
    public BackupE2ETests_Photo()
        : base(CreateEnvironment())
    {
    }

    /// <summary>
    /// Prüft, dass gelöschte Vorgänge samt Foto durch die Wiederherstellung zurückkommen (Foto byte-gleich) und die Auswertung wieder die Werte zeigt.
    /// </summary>
    [Fact]
    public void RestoreBringsBackEntriesPhotoAndStatistics()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Press("TankbookEntries.Add");
            Press("TankbookEntries.Form.PickPhoto");
            WaitForAutomationId("TankbookEntries.Form.RemovePhoto");
            Tankbook.FillEntryForm("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Tankbook.SaveEntryForm();
            Tankbook.CreateEntry("Alpha Tankstelle", "30", "1,859", string.Empty, "11000");
            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 €", "130,13 €");
            var photo = Assert.Single(TankbookTestDatabase.ListReceiptFiles(TestDataDirectory));
            var photoBytes = File.ReadAllBytes(Path.Combine(TestDataDirectory, "receipts", photo));

            OpenBackup();
            CreateBackup();

            OpenTankbook();
            Tankbook.OpenEntries("Golf");
            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            Press("TankbookEntry.Item.DeleteConfirm");
            Tankbook.PressForEntry("TankbookEntry.Item.Delete", 0);
            Press("TankbookEntry.Item.DeleteConfirm");
            WaitForAutomationId("TankbookEntries.Empty");
            Assert.Empty(TankbookTestDatabase.ListReceiptFiles(TestDataDirectory));

            OpenBackup();
            RestoreBackup(1);

            Assert.Equal(photo, Assert.Single(TankbookTestDatabase.ListReceiptFiles(TestDataDirectory)));
            Assert.Equal(photoBytes, File.ReadAllBytes(Path.Combine(TestDataDirectory, "receipts", photo)));
            Assert.Equal(2, TankbookTestDatabase.CountEntries(DatabasePath));
            OpenTankbook();
            Tankbook.OpenEntries("Golf");
            Tankbook.WaitForStatistics("Fuel", "6,00 L/100 km", "11,15 €", "130,13 €");
            Tankbook.WaitUntil(() => Tankbook.ReadTexts("TankbookEntry.Item.ShowReceipt").Count == 1, "Der Beleg des wiederhergestellten Vorgangs fehlt.");
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
}
