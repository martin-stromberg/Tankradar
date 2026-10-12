using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Tank- und Ladevorgänge mit Verbrauch und Belegfoto überstehen einen Neustart der App (alles lokal gespeichert).
/// </summary>
public class TankbookEntriesE2ETests_Persistence : TankbookE2ETestBase
{
    private static readonly string PhotoPath = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E", "beleg-" + Guid.NewGuid().ToString("N") + ".png");

    /// <summary>
    /// Erzeugt die Testdatei und startet die App mit der Test-Fotoauswahl.
    /// </summary>
    public TankbookEntriesE2ETests_Persistence()
        : base(CreateEnvironment())
    {
    }

    /// <summary>
    /// Prüft Werte, Verbrauch und Belegfoto nach einem Neustart.
    /// </summary>
    [Fact]
    public void Entries_SurviveApplicationRestart()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "40", "1,859", string.Empty, "10500");
            Press("TankbookEntries.Add");
            Press("TankbookEntries.Form.PickPhoto");
            WaitForAutomationId("TankbookEntries.Form.RemovePhoto");
            Tankbook.FillEntryForm("Beta Tankstelle", "30", "1,859", string.Empty, "11000");
            Tankbook.SaveEntryForm();
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 6,00 L/100 km", "Noch kein Verbrauch: erster voller Vorgang");

            RestartApplication();
            OpenTankbook();
            Tankbook.OpenEntries("Golf");

            WaitForTexts("TankbookEntry.Item.Station", "Beta Tankstelle", "Alpha Tankstelle");
            WaitForTexts("TankbookEntry.Item.Consumption", "Verbrauch 6,00 L/100 km", "Noch kein Verbrauch: erster voller Vorgang");
            WaitForTexts("TankbookEntry.Item.Total", "55,77 €", "74,36 €");
            Press("TankbookEntry.Item.ShowReceipt");
            WaitForAutomationId("TankbookEntry.Item.Receipt");
            Assert.False(Exists("TankbookEntry.Item.ReceiptMissing"));
            Assert.Single(TankbookTestDatabase.ListReceiptFiles(TestDataDirectory));
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
