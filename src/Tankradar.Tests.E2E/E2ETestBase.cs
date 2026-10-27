using System.Diagnostics;
using System.Runtime.CompilerServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Data.Sqlite;
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

        Automation = CreateAutomation();

        LaunchApplication();
    }

    /// <summary>
    /// Das Verzeichnis, in das Diagnosedaten fehlgeschlagener Tests geschrieben werden (Standard: Verzeichnis e2e-diagnostics im Repository-Root).
    /// </summary>
    protected virtual string DiagnosticsDirectory => DefaultDiagnosticsDirectory;

    /// <summary>
    /// Gibt an, ob die Testbasis die Einrichtung beim ersten Start selbst abschließt (Standard): Fehlt in der Testdatenbank vor dem Start die Zeile
    /// <c>UserSettings</c>, wird die Einrichtung nach dem Start über „Überspringen“ beendet. Die App legt dabei die Zeile mit den Standardwerten an,
    /// sodass die Oberfläche wie bei einer eingerichteten Installation bedienbar ist. Die Tests der Einrichtung selbst geben <see langword="false"/> zurück.
    /// </summary>
    protected virtual bool CompletesSetupOnStart => true;

    /// <summary>
    /// AutomationId der Schaltfläche „Speichern“ der Einrichtung beim ersten Start.
    /// </summary>
    protected const string SetupSaveId = "Setup.Save";

    /// <summary>
    /// AutomationId der Schaltfläche „Überspringen“ der Einrichtung beim ersten Start.
    /// </summary>
    protected const string SetupSkipId = "Setup.Skip";

    /// <summary>
    /// AutomationId der Überschrift der Einrichtung.
    /// </summary>
    protected const string SetupHeadingId = "Setup.Heading";

    /// <summary>
    /// AutomationId des Hinweises der Einrichtung.
    /// </summary>
    protected const string SetupHintId = "Setup.Hint";

    /// <summary>
    /// AutomationId der Status- bzw. Fehlermeldung der Einrichtung.
    /// </summary>
    protected const string SetupStatusMessageId = "Setup.StatusMessage";

    /// <summary>
    /// Liefert die AutomationId des Schalters einer Spritsorte in der Einrichtung.
    /// </summary>
    /// <param name="fuelType">Der Spritsortenname (z. B. SuperE5).</param>
    /// <returns>Die AutomationId, z. B. <c>Setup.FuelType.SuperE5.Switch</c>.</returns>
    protected static string SetupFuelTypeSwitchId(string fuelType) => $"Setup.FuelType.{fuelType}.Switch";

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
    /// Wechselt auf den Reiter und wartet, bis die Zielseite erkannt ist: entweder an einem Element mit der angegebenen AutomationId
    /// oder (ohne Angabe) am Seitentitel der Shell-Kopfzeile, der dem Reiternamen entsprechen muss.
    /// Bleibt der Seitenwechsel aus, wird der Reiter mit begrenzten Wiederholungen erneut ausgewählt
    /// (abwechselnd per SelectionItemPattern und InvokePattern; es werden keine Mausklicks verwendet).
    /// </summary>
    /// <param name="tabTitle">Titel des Reiters (z. B. Optionen).</param>
    /// <param name="pageAutomationIds">AutomationIds von Elementen, die nur auf der Zielseite existieren; mehrere Angaben stehen für gleichwertige Zielseiten
    /// (z. B. die Wurzel eines Reiters oder eine darauf geöffnete Seite), eine genügt. Ohne Angabe wird der Seitentitel der Shell geprüft.</param>
    protected void NavigateToTab(string tabTitle, params string[] pageAutomationIds)
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
                if (pageAutomationIds.Length == 0
                    ? IsShellTitle(tabTitle)
                    : pageAutomationIds.Any(id => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(id)) is not null))
                {
                    return;
                }

                Thread.Sleep(100);
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"Nach {maxAttempts} Versuchen wurde der Seitenwechsel auf Reiter '{tabTitle}' nicht erkannt (erwartet: {(pageAutomationIds.Length == 0 ? $"Seitentitel '{tabTitle}'" : $"AutomationId '{string.Join("' oder '", pageAutomationIds)}'")}).");
    }

    /// <summary>
    /// Wartet, bis die Kopfzeile der Shell den Seitentitel anzeigt (z. B. nach der Rückkehr aus der Detailansicht); schlägt sonst fehl.
    /// </summary>
    /// <param name="title">Der erwartete Seitentitel (z. B. Karte).</param>
    protected void WaitForPageTitle(string title)
    {
        // Ein UIA-Timeout zählt als „noch nicht erfüllt“; gewartet wird bis zur Frist weiter.
        if (!TransientWait.Until(() => IsShellTitle(title), TimeSpan.FromSeconds(30), out _))
        {
            Xunit.Assert.Fail($"Der Seitentitel '{title}' wurde nicht angezeigt.");
        }
    }

    /// <summary>
    /// Prüft, ob die Kopfzeile der Shell (Windows: Text mit der AutomationId <c>title</c>) den Seitentitel anzeigt.
    /// Der Seitentitel ersetzt die frühere Überschrift im Seiteninhalt als Signal für die angezeigte Seite.
    /// </summary>
    /// <param name="title">Der erwartete Seitentitel (z. B. Karte).</param>
    /// <returns><see langword="true"/>, wenn der Titel angezeigt wird.</returns>
    protected bool IsShellTitle(string title)
    {
        var element = TransientRetry.Run(
            () => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("title")),
            $"Suche des Seitentitels '{title}'");
        return string.Equals(element?.Name, title, StringComparison.Ordinal);
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
    /// Das isolierte Datenverzeichnis der gestarteten App (Testmodus); die Datenbank der App liegt dort als <c>tankatlas.db</c>.
    /// </summary>
    protected string TestDataDirectory => _testDataDirectory;

    /// <summary>
    /// Der Pfad der Datenbankdatei der laufenden App im <see cref="TestDataDirectory"/>.
    /// </summary>
    protected string DatabasePath
    {
        get { return Path.Combine(TestDataDirectory, DatabaseFileName); }
    }

    /// <summary>
    /// Die UI-Automation-Engine, mit der die gestartete App bedient wird.
    /// </summary>
    protected UIA3Automation Automation { get; private set; }

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

        CloseApplicationAndWait();

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
        CloseApplicationAndWait();
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

    /// <summary>
    /// Schließt die App und wartet auf das Prozessende, damit der Testlauf nicht vor dem Abbau der App (Finalizer der WinRT-Objekte) weiterläuft;
    /// bleibt der Prozess bestehen, wird er samt Prozessbaum beendet. Ein Absturz beim Beenden (0xC0000005) wird im Testprotokoll vermerkt.
    /// </summary>
    private void CloseApplicationAndWait()
    {
        // Nach einem vor dem App-Start abgebrochenen Konstruktor gibt es keine App, die beendet werden müsste.
        if (Application is null)
        {
            return;
        }

        Process? process = null;
        try
        {
            process = Process.GetProcessById(Application.ProcessId);
            try
            {
                Application.Close();
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Hinweis: Application.Close() schlug fehl ({exception.GetType().Name}); der Prozess wird trotzdem abgewartet.");
            }

            if (!process.WaitForExit(TimeSpan.FromSeconds(15)))
            {
                Console.Error.WriteLine($"Hinweis: Die Tankatlas-App (PID {process.Id}) wurde nach 15 s nicht beendet und wird beendet.");
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(10));
            }
            else if (process.ExitCode == AccessViolationExitCode)
            {
                Console.Error.WriteLine($"Hinweis: Die Tankatlas-App (PID {process.Id}) endete mit 0xC0000005 (Zugriffsverletzung beim Beenden, siehe ADR 0015).");
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Best-effort: Der Prozess kann bereits beendet sein.
        }
        finally
        {
            process?.Dispose();
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

        var uiaTimeout = E2EStartupPolicy.ResolveUiaTimeout(Environment.GetEnvironmentVariable);
        var setupPending = CompletesSetupOnStart && !HasSettingsRow(DatabasePath);
        try
        {
            // Auf langsamen Runnern (GitHub-Windows) dauert der UIA-Verbindungsaufbau zum frisch gestarteten Fenster länger als die
            // FlaUI-Standard-Timeouts. Deshalb: großzügigere UIA-Timeouts, Warten auf Hauptfenster-Handle und Eingabebereitschaft,
            // Wiederholungen mit wachsendem Abstand und Gesamtzeitlimit, und höchstens ein Neustart der App samt neuer Automation-Instanz.
            // Nur UIA-Timeouts werden so behandelt; ein fehlendes Fenster oder andere Fehler bleiben sofort sichtbar.
            E2EStartupPolicy.StartWithRestart(
                () => StartAndConnect(startInfo, uiaTimeout),
                DiscardFailedStart,
                MaxStarts,
                "Start der Tankatlas-App");
            if (setupPending)
            {
                SkipSetup();
            }
        }
        catch (Exception ex)
        {
            // Startfehler: Diagnose erfassen (gesamter Bildschirm, da kein Fenster verfügbar ist) und Prozess aufräumen.
            E2EDiagnostics.Capture(DiagnosticsDirectory, GetType().Name + ".Start", null, ex, DescribeProcess());
            Dispose();
            throw;
        }
    }

    private const int MaxStarts = 2;

    private const string DatabaseFileName = "tankatlas.db";

    private const int AccessViolationExitCode = unchecked((int)0xC0000005);

    private static UIA3Automation CreateAutomation()
    {
        var timeout = E2EStartupPolicy.ResolveUiaTimeout(Environment.GetEnvironmentVariable);
        return TransientRetry.Run(
            () => new UIA3Automation { ConnectionTimeout = timeout, TransactionTimeout = timeout },
            "Aufbau der UI-Automation");
    }

    private bool StartAndConnect(ProcessStartInfo startInfo, TimeSpan uiaTimeout)
    {
        Application = Application.Launch(startInfo);
        using (var process = Process.GetProcessById(Application.ProcessId))
        {
            // Ein Überschreiten ist nicht fatal: Die Verbindung wird danach regulär mit Wiederholungen versucht.
            E2EStartupPolicy.WaitUntilReady(process, uiaTimeout);
        }

        var windowTimeout = uiaTimeout > TimeSpan.FromSeconds(30) ? uiaTimeout : TimeSpan.FromSeconds(30);
        MainWindow = TransientRetry.Run(
            () => Application.GetMainWindow(Automation, windowTimeout)
                ?? throw new InvalidOperationException($"Das Hauptfenster der Tankatlas-App wurde nicht innerhalb von {windowTimeout.TotalSeconds:0} Sekunden gefunden."),
            "Abfrage des Hauptfensters der Tankatlas-App",
            maxAttempts: 4,
            pause: TimeSpan.FromSeconds(2),
            backoffFactor: 2.0,
            maxPause: TimeSpan.FromSeconds(15),
            totalLimit: TimeSpan.FromSeconds(Math.Max(60, uiaTimeout.TotalSeconds * 2)));
        TransientRetry.Run(
            () => MainWindow.Title,
            "Erste Abfrage des Hauptfensters",
            maxAttempts: 4,
            pause: TimeSpan.FromSeconds(2),
            backoffFactor: 2.0,
            maxPause: TimeSpan.FromSeconds(15));
        return true;
    }

    private void DiscardFailedStart()
    {
        // Halb gestartete App beenden (nur den eigenen, zuvor gestarteten Prozess) und eine frische Automation-Instanz aufbauen.
        var processId = Application.ProcessId;
        try
        {
            using var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            process.WaitForExit(TimeSpan.FromSeconds(10));
        }
        catch (Exception)
        {
            // Best-effort: Der Prozess kann bereits beendet sein.
        }

        ((IDisposable)Automation).Dispose();
        Automation = CreateAutomation();
    }

    /// <summary>
    /// Prüft ohne Schreibzugriff, ob die Datenbankdatei die Zeile <c>UserSettings</c> enthält (Einrichtung abgeschlossen).
    /// Fehlt die Datei oder die Tabelle, gilt die Einrichtung als ausstehend.
    /// </summary>
    /// <param name="databasePath">Der Pfad der Datenbankdatei.</param>
    /// <returns><see langword="true"/>, wenn die Zeile vorhanden ist.</returns>
    private static bool HasSettingsRow(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;Pooling=False;Default Timeout=5");
        connection.Open();
        using var table = connection.CreateCommand();
        table.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'UserSettings'";
        if (Convert.ToInt64(table.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 0)
        {
            return false;
        }

        using var row = connection.CreateCommand();
        row.CommandText = "SELECT COUNT(*) FROM UserSettings";
        return Convert.ToInt64(row.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    /// <summary>
    /// Wartet auf die Einrichtung, beendet sie über „Überspringen“ (UI-Automation, kein Mausklick) und wartet, bis sie ausgeblendet ist.
    /// </summary>
    private void SkipSetup()
    {
        var timeout = TimeSpan.FromSeconds(30);
        if (!TransientWait.Until(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(SetupSkipId)) is not null, timeout, out _))
        {
            throw new Xunit.Sdk.XunitException("Die Einrichtung beim ersten Start wurde nicht angezeigt.");
        }

        // Ein Aufruf direkt nach dem Erscheinen kann verloren gehen, solange die Bindung des Befehls noch nicht steht; daher begrenzt wiederholen.
        // Ein doppelter Aufruf ist unkritisch: Überspringen speichert jedes Mal dieselben Standardwerte.
        const int maxAttempts = 5;
        Exception? lastTransientError = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(SetupSkipId))?.Patterns.Invoke.Pattern.Invoke();
            }
            catch (Exception ex) when (ex is global::FlaUI.Core.Exceptions.ElementNotAvailableException or System.Runtime.InteropServices.COMException)
            {
                // Element kurzzeitig nicht ansprechbar oder bereits entfernt: Prüfung unten entscheidet.
                lastTransientError = ex;
            }

            if (TransientWait.Until(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(SetupSkipId)) is null, TimeSpan.FromSeconds(6), out _))
            {
                return;
            }
        }

        throw new Xunit.Sdk.XunitException("Die Einrichtung wurde nach „Überspringen“ nicht ausgeblendet.", lastTransientError);
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
