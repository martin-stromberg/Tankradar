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
            // Erst auf eine gerenderte Seite warten, damit der Screenshot Inhalt zeigen kann.
            NavigateToTab("Optionen", "SettingsPage.Headline");
            var exception = Assert.Throws<InvalidOperationException>(
                () => RunWithDiagnostics(() => throw new InvalidOperationException("absichtlicher Fehlschlag"), "Beispieltest"));
            Assert.Equal("absichtlicher Fehlschlag", exception.Message);

            var prefix = Path.Combine(_diagnosticsDirectory, GetType().Name + ".Beispieltest");
            var screenshot = new FileInfo(prefix + ".png");
            Assert.True(screenshot.Exists && screenshot.Length > 0, "Screenshot wurde nicht erzeugt.");
            AssertScreenshotShowsWindowContent(screenshot.FullName);
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

    private static void AssertScreenshotShowsWindowContent(string path)
    {
        // Der Screenshot wird direkt vom App-Fenster aufgenommen (auch im Off-Screen-Betrieb): Er darf nicht leer/einfarbig sein.
        using var image = new System.Drawing.Bitmap(path);
        Assert.True(image.Width >= 300 && image.Height >= 300, $"Screenshot ist zu klein: {image.Width}x{image.Height}.");
        var colors = new HashSet<int>();
        for (var x = 0; x < image.Width; x += Math.Max(1, image.Width / 40))
        {
            for (var y = 0; y < image.Height; y += Math.Max(1, image.Height / 40))
            {
                colors.Add(image.GetPixel(x, y).ToArgb());
            }
        }

        Assert.True(colors.Count > 2, "Der Screenshot zeigt keinen Fensterinhalt (einfarbig).");
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
