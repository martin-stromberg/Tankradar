using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Belegt per Quelltextprüfung, dass die E2E-Hilfen im Off-Screen-Betrieb weder Fokus noch Vordergrund noch Tastatur oder Maus verwenden:
/// Das Testfenster darf nie Eingaben des Anwenders abfangen. Bedient wird ausschließlich über UI-Automation-Muster.
/// </summary>
public class E2EInputPolicyTests_NoFocus : BaseTest
{
    private static readonly Regex Forbidden = new(
        @"\.Focus\s*\(|\.SetForeground\s*\(|SetForegroundWindow|SetFocus|Keyboard\.|Mouse\.|SendKeys|SendInput|keybd_event|mouse_event|\.Click\s*\(|\.DoubleClick\s*\(|\.RightClick\s*\(|TypeSimultaneously|ShowWindow|SetActiveWindow|BringWindowToTop|SwitchToThisWindow|AttachThreadInput|InputSimulator|\.Hover\s*\(|\.Drag\w*\s*\(|using\s+static\s+[\w.]*(Keyboard|Mouse)",
        RegexOptions.Compiled);

    private static string SourceRoot([CallerFilePath] string path = "")
    {
        // <src>/Tankradar.Tests.Unit/Unit/<Datei> -> <src>
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
    }

    /// <summary>
    /// Prüft, dass im E2E-Projekt kein Aufruf von Fokus, Vordergrund, Tastatur oder Maus vorkommt.
    /// </summary>
    [Fact]
    public void E2ESources_NeverUseFocusKeyboardOrMouse()
    {
        var directory = Path.Combine(SourceRoot(), "Tankradar.Tests.E2E");
        Assert.True(Directory.Exists(directory), $"Das E2E-Verzeichnis fehlt: {directory}");

        var files = Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();
        Assert.NotEmpty(files);

        var violations = new List<string>();
        foreach (var file in files)
        {
            var lineNumber = 0;
            foreach (var line in File.ReadLines(file))
            {
                lineNumber++;
                var code = line.TrimStart();
                if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith("*", StringComparison.Ordinal) || code.StartsWith("/*", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Forbidden.IsMatch(line))
                {
                    violations.Add($"{Path.GetRelativePath(directory, file)}:{lineNumber}: {code}");
                }
            }
        }

        Assert.Empty(violations);
    }

    /// <summary>
    /// Prüft, dass das Off-Screen-Fenster der App nicht aktivierbar ist und nie aktiviert wird.
    /// </summary>
    [Fact]
    public void OffscreenWindow_IsNotActivatable()
    {
        var file = Path.Combine(SourceRoot(), "Tankradar.MAUI", "Platforms", "Windows", "OffscreenWindow.cs");
        Assert.True(File.Exists(file), $"Die Datei fehlt: {file}");

        var text = File.ReadAllText(file);
        Assert.Contains("WsExNoActivate", text, StringComparison.Ordinal);
        Assert.Contains("SwpNoActivate", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SetForegroundWindow", text, StringComparison.Ordinal);
        Assert.DoesNotContain(".Activate(", text, StringComparison.Ordinal);
    }
}
