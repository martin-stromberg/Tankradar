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

    /// <summary>
    /// Name der Umgebungsvariable des Testlaufs, mit der der Off-Screen-Betrieb auf <c>foreground</c> (Vordergrundbetrieb) zurückgestellt werden kann.
    /// </summary>
    public const string WindowModeEnvironmentVariable = "TANKRADAR_E2E_WINDOW";

    private static readonly string DefaultDiagnosticsDirectory = E2EDiagnostics.ResolveDirectory();

    private readonly string _testDataDirectory;
    private IReadOnlyDictionary<string, string> _additionalEnvironment;
    private bool _disposed;

    /// <summary>
    /// Erstellt die Testbasis, legt ein isoliertes Testdatenverzeichnis an und startet die Windows-App darauf ausgerichtet.
    /// </summary>
    protected E2ETestBase()
        : this(null)
    {
    }

    /// <summary>
    /// Erstellt die Testbasis wie <see cref="E2ETestBase()"/> und gibt der gestarteten App zusätzliche Umgebungsvariablen mit
    /// (Testkonfiguration, z. B. die Adresse des Mock-Dienstes für Kraftstoffpreise).
    /// </summary>
    /// <param name="additionalEnvironment">Zusätzliche Umgebungsvariablen oder <see langword="null"/>.</param>
    protected E2ETestBase(IReadOnlyDictionary<string, string>? additionalEnvironment)
    {
        _additionalEnvironment = additionalEnvironment ?? new Dictionary<string, string>();
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "Tankradar.Tests.E2E", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDataDirectory);

        Automation = TransientRetry.Run(() => new UIA3Automation(), "Aufbau der UI-Automation");

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

    /// <summary>
    /// Wechselt auf den Reiter und wartet, bis die seitenspezifische Überschrift (AutomationId) sichtbar ist.
    /// Bleibt der Seitenwechsel aus, wird der Reiter mit begrenzten Wiederholungen erneut ausgewählt
    /// (abwechselnd per SelectionItemPattern und InvokePattern; es werden keine Mausklicks verwendet).
    /// </summary>
    /// <param name="tabTitle">Titel des Reiters (z. B. Optionen).</param>
    /// <param name="pageAutomationId">AutomationId eines Elements, das nur auf der Zielseite existiert.</param>
    protected void NavigateToTab(string tabTitle, string pageAutomationId)
    {
        const int maxAttempts = 5;
        var attemptTimeout = TimeSpan.FromSeconds(5);

        // Erst wenn die Shell interaktiv ist (Tab-Leiste vorhanden), wird geklickt.
        var tab = WaitForTab(tabTitle, TimeSpan.FromSeconds(30))
            ?? throw new Xunit.Sdk.XunitException($"Der Reiter '{tabTitle}' wurde nicht gefunden.");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                tab = FindTab(tabTitle) ?? tab;
                // Ausschließlich UI-Automation-Muster, nie ein Mausklick: Das App-Fenster liegt im Off-Screen-Betrieb außerhalb
                // des Bildschirms, und ein Klick würde den Mauszeiger des Anwenders bewegen.
                if (attempt % 2 == 1 && tab.Patterns.SelectionItem.IsSupported)
                {
                    tab.Patterns.SelectionItem.Pattern.Select();
                }
                else if (tab.Patterns.Invoke.IsSupported)
                {
                    tab.Patterns.Invoke.Pattern.Invoke();
                }
                else if (tab.Patterns.SelectionItem.IsSupported)
                {
                    tab.Patterns.SelectionItem.Pattern.Select();
                }
            }
            catch (Exception)
            {
                // Element kurzzeitig nicht ansprechbar: nächster Versuch.
            }

            var deadline = DateTime.UtcNow + attemptTimeout;
            while (DateTime.UtcNow < deadline)
            {
                if (MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(pageAutomationId)) is not null)
                {
                    return;
                }

                Thread.Sleep(100);
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"Nach {maxAttempts} Versuchen wurde der Seitenwechsel auf Reiter '{tabTitle}' nicht erkannt (erwartete AutomationId '{pageAutomationId}').");
    }

    private AutomationElement? FindTab(string title)
    {
        return TransientRetry.Run(
            () => MainWindow.FindFirstDescendant(cf => cf.ByName(title).Or(cf.ByAutomationId(title))),
            $"Suche des Reiters '{title}'");
    }

    private AutomationElement? WaitForTab(string title, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (FindTab(title) is { } tab)
            {
                return tab;
            }

            Thread.Sleep(100);
        }

        return null;
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

        // Bei einem vor der Initialisierung abgebrochenen Start kann die Automation fehlen; das Aufräumen darf die Startausnahme nicht verdecken.
        ((IDisposable?)Automation)?.Dispose();

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
        Cleanup();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Wird am Ende von <see cref="Dispose"/> aufgerufen, damit abgeleitete Klassen eigene Ressourcen (z. B. einen Mock-Server) freigeben können.
    /// </summary>
    protected virtual void Cleanup()
    {
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

    /// <summary>
    /// Wie <see cref="RestartApplication()"/>, ersetzt aber die zusätzlichen Umgebungsvariablen der App für den Neustart (gleiches Testdatenverzeichnis).
    /// </summary>
    /// <param name="additionalEnvironment">Die Umgebungsvariablen der neu gestarteten App.</param>
    protected void RestartApplication(IReadOnlyDictionary<string, string> additionalEnvironment)
    {
        ArgumentNullException.ThrowIfNull(additionalEnvironment);
        _additionalEnvironment = additionalEnvironment;
        RestartApplication();
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
        startInfo.Environment[TestDataPaths.TestWindowEnvironmentVariable] = ResolveWindowMode();

        foreach (var (name, value) in _additionalEnvironment)
        {
            startInfo.Environment[name] = value;
        }

        Application = Application.Launch(startInfo);
        try
        {
            // Ein UIA-Timeout beim Aufbau der Automation bzw. der ersten Fensterabfrage (beobachtet auf dem GitHub-Windows-Runner)
            // wird begrenzt wiederholt; ein fehlendes Fenster oder andere Fehler bleiben sofort sichtbar.
            MainWindow = TransientRetry.Run(
                () => Application.GetMainWindow(Automation, TimeSpan.FromSeconds(30))
                    ?? throw new InvalidOperationException("Das Hauptfenster der Tankatlas-App wurde nicht innerhalb von 30 Sekunden gefunden."),
                "Abfrage des Hauptfensters der Tankatlas-App");
            TransientRetry.Run(() => MainWindow.Title, "Erste Abfrage des Hauptfensters");
        }
        catch (Exception ex)
        {
            // Startfehler: Diagnose erfassen (gesamter Bildschirm, da kein Fenster verfügbar ist) und Prozess aufräumen.
            E2EDiagnostics.Capture(DiagnosticsDirectory, GetType().Name + ".Start", null, ex, DescribeProcess());
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Ermittelt die Fensterbetriebsart der App: Standard ist der Off-Screen-Betrieb; der Rückfall auf den Vordergrundbetrieb
    /// erfolgt durch <c>TANKRADAR_E2E_WINDOW=foreground</c> in der Umgebung des Testlaufs.
    /// </summary>
    /// <returns>Der Wert für <c>TANKATLAS_TEST_WINDOW</c>.</returns>
    private static string ResolveWindowMode()
    {
        var configured = Environment.GetEnvironmentVariable(WindowModeEnvironmentVariable);
        return string.Equals(configured?.Trim(), TestDataPaths.TestWindowForeground, StringComparison.OrdinalIgnoreCase)
            ? TestDataPaths.TestWindowForeground
            : TestDataPaths.TestWindowOffscreen;
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
