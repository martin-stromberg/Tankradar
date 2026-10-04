namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Sichert ab, dass die Diagnoseerfassung bei einem fehlgeschlagenen E2E-Test tatsächlich Dateien erzeugt.
/// </summary>
public class DiagnosticsCaptureE2ETests : E2ETestBase
{
    private readonly string _diagnosticsDirectory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E.Diagnostics", Guid.NewGuid().ToString("N"));

    /// <inheritdoc />
    protected override string DiagnosticsDirectory => _diagnosticsDirectory;

    /// <summary>
    /// Prüft, dass ein fehlschlagender Testkörper Screenshot, UI-Baum und Fehlerbeschreibung erzeugt und den Fehler weiterreicht.
    /// </summary>
    [Fact]
    public void FailingTestBodyProducesScreenshotUiTreeAndErrorFile()
    {
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => RunWithDiagnostics(() => throw new InvalidOperationException("absichtlicher Fehlschlag"), "Beispieltest"));
            Assert.Equal("absichtlicher Fehlschlag", exception.Message);

            var prefix = Path.Combine(_diagnosticsDirectory, GetType().Name + ".Beispieltest");
            var screenshot = new FileInfo(prefix + ".png");
            Assert.True(screenshot.Exists && screenshot.Length > 0, "Screenshot wurde nicht erzeugt.");
            var tree = File.ReadAllText(prefix + ".uitree.txt");
            Assert.Contains("AutomationId=", tree);
            var error = File.ReadAllText(prefix + ".error.txt");
            Assert.Contains("absichtlicher Fehlschlag", error);
        }
        finally
        {
            if (Directory.Exists(_diagnosticsDirectory))
            {
                Directory.Delete(_diagnosticsDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Prüft, dass bei einem erfolgreichen Testkörper keine Diagnosedateien entstehen.
    /// </summary>
    [Fact]
    public void PassingTestBodyProducesNoDiagnostics()
    {
        RunWithDiagnostics(() => Assert.NotNull(MainWindow));

        Assert.False(Directory.Exists(_diagnosticsDirectory));
    }
}
