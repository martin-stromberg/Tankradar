using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;

namespace Tankradar.Tests.E2E;

/// <summary>
/// Erzeugt bei fehlgeschlagenen E2E-Tests Diagnosematerial (Screenshot, UI-Automation-Baum, Fehlerbeschreibung) in einem definierten Verzeichnis,
/// das die CI-Workflows als Artefakt hochladen.
/// </summary>
public static class E2EDiagnostics
{
    /// <summary>
    /// Name der Umgebungsvariable, mit der das Diagnoseverzeichnis überschrieben werden kann.
    /// </summary>
    public const string DirectoryEnvironmentVariable = "TANKRADAR_E2E_DIAGNOSTICS_DIR";

    /// <summary>
    /// Name des Standard-Diagnoseverzeichnisses im Repository-Root (per .gitignore ausgeschlossen, von den CI-Workflows hochgeladen).
    /// </summary>
    public const string DefaultDirectoryName = "e2e-diagnostics";

    private const int MaxTreeDepth = 40;

    /// <summary>
    /// Ermittelt das Diagnoseverzeichnis: Umgebungsvariable, sonst <c>e2e-diagnostics</c> im Repository-Root.
    /// </summary>
    /// <returns>Der absolute Pfad des Diagnoseverzeichnisses.</returns>
    public static string ResolveDirectory()
    {
        var configured = Environment.GetEnvironmentVariable(DirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Tankradar.sln")))
            {
                return Path.Combine(current.FullName, DefaultDirectoryName);
            }

            current = current.Parent;
        }

        return Path.Combine(Path.GetTempPath(), DefaultDirectoryName);
    }

    /// <summary>
    /// Erfasst Diagnosedaten zu einem fehlgeschlagenen Test; Fehler bei der Erfassung werden nie nach außen gegeben.
    /// </summary>
    /// <param name="directory">Zielverzeichnis der Diagnosedaten.</param>
    /// <param name="testName">Name des Tests; wird für die Dateinamen verwendet.</param>
    /// <param name="window">Das App-Fenster, oder <c>null</c>, wenn es nicht verfügbar ist (dann wird der gesamte Bildschirm erfasst).</param>
    /// <param name="failure">Der aufgetretene Fehler.</param>
    /// <param name="processInfo">Optionale Angaben zum App-Prozess (Status, Exit-Code).</param>
    /// <returns>Die Pfade der erzeugten Dateien.</returns>
    public static IReadOnlyList<string> Capture(string directory, string testName, Window? window, Exception failure, string? processInfo = null)
    {
        var created = new List<string>();
        var baseName = SanitizeFileName(testName);

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception)
        {
            return created;
        }

        var errorPath = Path.Combine(directory, baseName + ".error.txt");
        TryWrite(created, errorPath, () => File.WriteAllText(errorPath, BuildErrorText(testName, failure, processInfo), Encoding.UTF8));

        var screenshotPath = Path.Combine(directory, baseName + ".png");
        TryWrite(created, screenshotPath, () =>
        {
            using var image = window is not null ? FlaUI.Core.Capturing.Capture.Element(window) : FlaUI.Core.Capturing.Capture.Screen();
            image.ToFile(screenshotPath);
        });

        if (window is not null)
        {
            var treePath = Path.Combine(directory, baseName + ".uitree.txt");
            TryWrite(created, treePath, () => File.WriteAllText(treePath, DumpTree(window), Encoding.UTF8));
        }

        return created;
    }

    /// <summary>
    /// Erzeugt eine textuelle Darstellung des UI-Automation-Baums ab dem übergebenen Element.
    /// </summary>
    /// <param name="root">Wurzelelement.</param>
    /// <returns>Der eingerückte Baum mit Typ, Name, AutomationId und Klasse je Element.</returns>
    public static string DumpTree(AutomationElement root)
    {
        var builder = new StringBuilder();
        AppendElement(builder, root, 0);
        return builder.ToString();
    }

    private static void AppendElement(StringBuilder builder, AutomationElement element, int depth)
    {
        builder.Append(' ', depth * 2);
        try
        {
            var bounds = element.Properties.BoundingRectangle.ValueOrDefault;
            builder.Append(element.Properties.ControlType.ValueOrDefault)
                .Append(" Name='").Append(element.Properties.Name.ValueOrDefault)
                .Append("' AutomationId='").Append(element.Properties.AutomationId.ValueOrDefault)
                .Append("' Class='").Append(element.Properties.ClassName.ValueOrDefault)
                .Append("' Bounds=").Append(bounds)
                .AppendLine();
        }
        catch (Exception ex)
        {
            builder.Append("<Element nicht lesbar: ").Append(ex.GetType().Name).AppendLine(">");
            return;
        }

        if (depth >= MaxTreeDepth)
        {
            return;
        }

        AutomationElement[] children;
        try
        {
            children = element.FindAllChildren();
        }
        catch (Exception)
        {
            return;
        }

        foreach (var child in children)
        {
            AppendElement(builder, child, depth + 1);
        }
    }

    private static string BuildErrorText(string testName, Exception failure, string? processInfo)
    {
        var builder = new StringBuilder();
        builder.Append("Test: ").AppendLine(testName);
        builder.Append("Zeitpunkt (UTC): ").AppendLine(DateTime.UtcNow.ToString("O"));
        if (!string.IsNullOrEmpty(processInfo))
        {
            builder.Append("App-Prozess: ").AppendLine(processInfo);
        }

        builder.AppendLine().AppendLine(failure.ToString());
        return builder.ToString();
    }

    private static void TryWrite(List<string> created, string path, Action write)
    {
        try
        {
            write();
            if (File.Exists(path))
            {
                created.Add(path);
            }
        }
        catch (Exception)
        {
            // Best-effort: Die Diagnoseerfassung darf den eigentlichen Testfehler nie überdecken.
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "unbekannt" : cleaned;
    }
}
