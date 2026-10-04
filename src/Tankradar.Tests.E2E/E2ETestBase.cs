using System.Diagnostics;
using System.Runtime.CompilerServices;
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

    private static readonly string DefaultDiagnosticsDirectory = E2EDiagnostics.ResolveDirectory();

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

        LaunchApplication();
    }

    /// <summary>
    /// Das Verzeichnis, in das Diagnosedaten fehlgeschlagener Tests geschrieben werden (Standard: Verzeichnis e2e-diagnostics im Repository-Root).
    /// </summary>
    protected virtual string DiagnosticsDirectory => DefaultDiagnosticsDirectory;

    /// <summary>
    /// Führt den Testkörper aus und erfasst bei einem Fehlschlag Screenshot, UI-Automation-Baum und Fehlerbeschreibung (pro Test benannt), bevor der Fehler weitergereicht wird.
    /// </summary>
    /// <param name="test">Der Testkörper.</param>
    /// <param name="testName">Name des Tests (wird automatisch vom aufrufenden Testmethodennamen übernommen).</param>
    protected void RunWithDiagnostics(Action test, [CallerMemberName] string testName = "")
    {
        try
        {
            test();
        }
        catch (Exception ex)
        {
            E2EDiagnostics.Capture(DiagnosticsDirectory, GetType().Name + "." + testName, MainWindow, ex, DescribeProcess());
            throw;
        }
    }

    private string DescribeProcess()
    {
        try
        {
            using var process = Process.GetProcessById(Application.ProcessId);
            return process.HasExited ? $"beendet, ExitCode={process.ExitCode}" : $"läuft (PID {process.Id})";
        }
        catch (Exception)
        {
            return "nicht mehr vorhanden";
        }
    }

    /// <summary>
    /// Die UI-Automation-Engine, mit der die gestartete App bedient wird.
    /// </summary>
    protected UIA3Automation Automation { get; }

    /// <summary>
    /// Der gestartete App-Prozess.
    /// </summary>
    protected Application Application { get; private set; } = null!;

    /// <summary>
    /// Das Hauptfenster der gestarteten App.
    /// </summary>
    protected Window MainWindow { get; private set; } = null!;

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

    /// <summary>
    /// Beendet die App, wartet auf das Prozessende und startet sie mit demselben Testdatenverzeichnis neu; <see cref="Application"/> und <see cref="MainWindow"/> zeigen danach auf die neue Instanz.
    /// </summary>
    protected void RestartApplication()
    {
        var processId = Application.ProcessId;
        try
        {
            Application.Close();
        }
        catch (Exception)
        {
            // Best-effort: Der Prozess kann bereits beendet sein.
        }

        WaitForProcessExit(processId);
        LaunchApplication();
    }

    private static void WaitForProcessExit(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (!process.WaitForExit(TimeSpan.FromSeconds(15)))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(10));
            }
        }
        catch (ArgumentException)
        {
            // Der Prozess ist bereits beendet.
        }
    }

    private void LaunchApplication()
    {
        var startInfo = new ProcessStartInfo(ResolveAppPath())
        {
            UseShellExecute = false,
        };
        startInfo.Environment[TestDataPaths.TestDataPathEnvironmentVariable] = _testDataDirectory;

        Application = Application.Launch(startInfo);
        try
        {
            MainWindow = Application.GetMainWindow(Automation, TimeSpan.FromSeconds(30))
                ?? throw new InvalidOperationException("Das Hauptfenster der Tankatlas-App wurde nicht innerhalb von 30 Sekunden gefunden.");
        }
        catch (Exception ex)
        {
            // Startfehler: Diagnose erfassen (gesamter Bildschirm, da kein Fenster verfügbar ist) und Prozess aufräumen.
            E2EDiagnostics.Capture(DiagnosticsDirectory, GetType().Name + ".Start", null, ex, DescribeProcess());
            Dispose();
            throw;
        }
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
