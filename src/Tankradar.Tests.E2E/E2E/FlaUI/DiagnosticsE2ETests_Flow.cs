using System.IO.Compression;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test des Opt-in-Diagnosepakets: Die Karte zeigt Zweck und Umfang, die Vorschau geht dem Teilen voraus
/// und das Paket landet im Testmodus ohne Systemdialog im Testverzeichnis.
/// </summary>
public class DiagnosticsE2ETests_Flow : SettingsE2ETestBase
{
    private string SharedDirectory
    {
        get { return Path.Combine(TestDataDirectory, "test-shared"); }
    }

    /// <summary>
    /// Prüft den vollständigen Ablauf: Karte sichtbar, Teilen zunächst gesperrt, Vorschau zeigt den Paketinhalt,
    /// Teilen legt ein gültiges, redigiertes ZIP im Testverzeichnis ab.
    /// </summary>
    [Fact]
    public void Diagnostics_PreviewThenShare_CreatesRedactedPackage()
    {
        RunWithDiagnostics(() =>
        {
            OpenSettings();
            WaitForAutomationId("Settings.Diagnostics.Hint");
            Assert.Equal("Diagnose", WaitForAutomationId("Settings.Diagnostics.Heading").Name);
            Assert.False(WaitForAutomationId("Settings.Diagnostics.Share").IsEnabled, "Die Teilen-Schaltfläche ist vor der Vorschau nicht gesperrt.");
            Assert.Null(FindByAutomationId("Settings.Diagnostics.PreviewText"));

            PressSetupButton("Settings.Diagnostics.Preview");

            var preview = WaitForAutomationId("Settings.Diagnostics.PreviewText");
            Assert.Contains("Tankatlas-Diagnosepaket", preview.Name);
            Assert.Contains("paket-info.json", preview.Name);
            WaitUntil(() => WaitForAutomationId("Settings.Diagnostics.Share").IsEnabled, "Die Teilen-Schaltfläche wurde nach der Vorschau nicht freigegeben.");

            PressSetupButton("Settings.Diagnostics.Share");

            WaitUntil(
                () => Directory.Exists(SharedDirectory) && Directory.GetFiles(SharedDirectory, "tankatlas-diagnose-*.zip").Length == 1,
                "Das Diagnosepaket liegt nicht im Testverzeichnis.");
            var packagePath = Directory.GetFiles(SharedDirectory, "tankatlas-diagnose-*.zip").Single();
            var entryNames = ReadEntryNames(packagePath);
            Assert.Equal(["liesmich.txt", "paket-info.json", "protokoll.txt"], entryNames.Order(StringComparer.Ordinal).ToArray());
            WaitUntil(
                () => FindByAutomationId("Settings.Diagnostics.Status")?.Name.Contains("erstellt", StringComparison.Ordinal) == true,
                $"Die Statusmeldung fehlt, zuletzt: „{FindByAutomationId("Settings.Diagnostics.Status")?.Name}“.");
        });
    }

    private static string[] ReadEntryNames(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        return archive.Entries.Select(entry => entry.Name).ToArray();
    }
}
