using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E;

/// <summary>
/// Basisklasse für FlaUI-E2E-Tests; startet die Windows-Anwendung mit einem isolierten Testdatenverzeichnis und automatisiert sie über UI Automation.
/// </summary>
public abstract class E2ETestBase : IDisposable
{
#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif

    private const string AppRelativePath = @"..\..\..\..\Tankradar.MAUI\bin\" + BuildConfiguration + @"\net10.0-windows10.0.19041.0\win-x64\Tankradar.MAUI.exe";

    private readonly string _testDataDirectory;
    private bool _disposed;

    /// <summary>
    /// Erstellt die Testbasis, legt ein isoliertes Testdatenverzeichnis an und startet die Windows-App darauf ausgerichtet.
    /// </summary>
    protected E2ETestBase()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDataDirectory);

        Automation = new UIA3Automation();

        var startInfo = new ProcessStartInfo(ResolveAppPath())
        {
            UseShellExecute = false,
        };
        startInfo.Environment[TestDataPaths.TestDataPathEnvironmentVariable] = _testDataDirectory;

        Application = Application.Launch(startInfo);
        MainWindow = Application.GetMainWindow(Automation, TimeSpan.FromSeconds(30))
            ?? throw new InvalidOperationException("Das Hauptfenster der Tankradar-App wurde nicht innerhalb von 30 Sekunden gefunden.");
    }

    /// <summary>
    /// Die UI-Automation-Engine, mit der die gestartete App bedient wird.
    /// </summary>
    protected UIA3Automation Automation { get; }

    /// <summary>
    /// Der gestartete App-Prozess.
    /// </summary>
    protected Application Application { get; }

    /// <summary>
    /// Das Hauptfenster der gestarteten App.
    /// </summary>
    protected Window MainWindow { get; }

    /// <summary>
    /// Beendet die App, gibt die UI-Automation-Engine frei und löscht das Testdatenverzeichnis.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            Application.Close();
        }
        catch (Exception)
        {
            // Best-effort: Der Prozess kann bereits beendet sein.
        }

        Automation.Dispose();

        try
        {
            if (Directory.Exists(_testDataDirectory))
            {
                Directory.Delete(_testDataDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort: Verzeichnis kann noch kurz durch den beendeten Prozess gesperrt sein.
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static string ResolveAppPath()
    {
        var candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, AppRelativePath));
        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException(
                "Die Tankradar.MAUI.exe wurde nicht gefunden. Bitte zunächst 'dotnet build -f net10.0-windows10.0.19041.0' im Hauptprojekt (src/Tankradar.MAUI) ausführen.",
                candidate);
        }

        return candidate;
    }
}
