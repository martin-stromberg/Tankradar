using System.Globalization;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis der E2E-Tests des Exports: Bedienung des Bereichs „Export“ auf der Seite „Tank- und Ladevorgänge“ ausschließlich über UI-Automation-Muster
/// (Auswahlchips, Schaltfläche, Tageswahl im Kalender über das SelectionItemPattern) und Zugriff auf die Dateien, die der Testmodus statt des Teilen-Dialogs
/// in das Verzeichnis <c>test-shared</c> kopiert. Die App läuft im Testmodus ohne Preisdienst; es werden keine Produktivendpunkte aufgerufen.
/// </summary>
public abstract class TankbookExportE2ETestBase : BackupE2ETestBase
{
    private const string Prefix = "TankbookEntries.Export.";

    /// <summary>
    /// Der Anfang der Statusmeldung nach einem erfolgreichen Export.
    /// </summary>
    protected const string ReadyPrefix = "Export bereit: Tankbuch_";

    /// <summary>
    /// Die Kopfzeile der CSV-Datei.
    /// </summary>
    protected static string CsvHeader
    {
        get
        {
            return string.Join(
                ';',
                ["Datum", "Uhrzeit", "Tankstelle", "Art", "Menge", "Einheit", "Preis pro Einheit " + Bracket("EUR"), "Gesamtbetrag " + Bracket("EUR"), "Kilometerstand", "Teilbetankung", "Strecke " + Bracket("km"), "Verbrauch", "Verbrauchseinheit", "Hinweis zum Verbrauch", "Notiz"]);
        }
    }

    /// <summary>
    /// Die Dateien des Exports im Verzeichnis, in das der Testmodus „Teilen“ kopiert.
    /// </summary>
    /// <returns>Die Dateinamen, sortiert.</returns>
    protected string[] ExportFiles()
    {
        return Directory.Exists(SharedDirectory)
            ? Directory.GetFiles(SharedDirectory, "Tankbuch_*").Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToArray()
            : [];
    }

    /// <summary>
    /// Wählt das Format (<c>csv</c> oder <c>pdf</c>) und wartet, bis die Auswahl angezeigt wird.
    /// </summary>
    /// <param name="key">Der Schlüssel des Formats.</param>
    protected void ChooseFormat(string key)
    {
        Choose($"{Prefix}Format.{key}");
    }

    /// <summary>
    /// Wählt den Zeitraum (<c>all</c>, <c>thisYear</c>, <c>lastYear</c>, <c>last12Months</c> oder <c>custom</c>) und wartet, bis die Auswahl angezeigt wird.
    /// </summary>
    /// <param name="key">Der Schlüssel des Zeitraums.</param>
    protected void ChooseRange(string key)
    {
        Choose($"{Prefix}Range.{key}");
    }

    /// <summary>
    /// Gibt an, ob die Option ausgewählt ist (Hinweis „Ausgewählt“ für die Sprachausgabe).
    /// </summary>
    /// <param name="automationId">Die AutomationId der Option.</param>
    /// <returns><see langword="true"/>, wenn die Option ausgewählt ist.</returns>
    protected bool IsChosen(string automationId)
    {
        return FindByAutomationId(automationId)?.Properties.HelpText.ValueOrDefault == "Ausgewählt";
    }

    /// <summary>
    /// Löst <b>Exportieren</b> aus.
    /// </summary>
    protected void StartExport()
    {
        Press($"{Prefix}Start");
    }

    /// <summary>
    /// Löscht die bisherigen Exportdateien im Verzeichnis des Testmodus, damit eine neue Datei eindeutig erkennbar ist.
    /// </summary>
    protected void ClearExportFiles()
    {
        foreach (var file in ExportFiles())
        {
            File.Delete(Path.Combine(SharedDirectory, file));
        }
    }

    /// <summary>
    /// Löst den Export aus und wartet, bis genau eine neue Datei im Verzeichnis des Testmodus liegt und die Statusmeldung den Dateinamen nennt.
    /// </summary>
    /// <returns>Der vollständige Pfad der Datei.</returns>
    protected string ExportAndWaitForFile()
    {
        ClearExportFiles();
        StartExport();
        WaitUntil(() => ExportFiles().Length == 1, "Der Export hat keine Datei im Verzeichnis des Testmodus abgelegt.");
        WaitUntil(
            () => ReadStatus().StartsWith(ReadyPrefix, StringComparison.Ordinal),
            $"Die Statusmeldung meldet keinen fertigen Export, zuletzt: „{ReadStatus()}“.");
        return Path.Combine(SharedDirectory, ExportFiles().Single());
    }

    /// <summary>
    /// Liest die Statusmeldung des Bereichs; leer, wenn keine angezeigt wird.
    /// </summary>
    /// <returns>Der Text.</returns>
    protected string ReadStatus()
    {
        return FindByAutomationId($"{Prefix}Status")?.Name ?? string.Empty;
    }

    /// <summary>
    /// Wartet, bis die Statusmeldung genau den Text zeigt.
    /// </summary>
    /// <param name="expected">Der erwartete Text.</param>
    protected void WaitForExportStatus(string expected)
    {
        WaitUntil(() => ReadStatus() == expected, $"Die Statusmeldung des Exports ist nicht „{expected}“, zuletzt: „{ReadStatus()}“.");
    }

    /// <summary>
    /// Liest eine Exportdatei vollständig (mit Wiederholung, falls sie noch kurz gesperrt ist).
    /// </summary>
    /// <param name="path">Der Pfad.</param>
    /// <returns>Die Bytes.</returns>
    protected static byte[] ReadFile(string path)
    {
        byte[]? bytes = null;
        WaitUntil(
            () =>
            {
                try
                {
                    bytes = File.ReadAllBytes(path);
                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
            },
            $"Die Datei '{path}' ließ sich nicht lesen.");
        return bytes!;
    }

    /// <summary>
    /// Liest eine CSV-Datei: prüft das BOM und liefert die Zeilen ohne das abschließende Leerelement.
    /// </summary>
    /// <param name="path">Der Pfad.</param>
    /// <returns>Die Zeilen, die erste ist die Kopfzeile.</returns>
    protected static string[] ReadCsvLines(string path)
    {
        var bytes = ReadFile(path);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        var text = new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
        Assert.EndsWith("\r\n", text, StringComparison.Ordinal);
        return text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Legt ein Fahrzeug an und öffnet seine Vorgangsseite.
    /// </summary>
    /// <param name="name">Der Name.</param>
    /// <param name="fuelKindKey">Der Schlüssel der Kraftstoffart.</param>
    protected void OpenEntriesOfNewVehicle(string name, string fuelKindKey = "Diesel")
    {
        OpenTankbook();
        CreateVehicle(name, fuelKindKey, "10000", string.Empty);
        Tankbook.OpenEntries(name);
    }

    /// <summary>
    /// Verlässt die Vorgangsseite und öffnet sie erneut (lädt die Vorgänge aus der Datenbank neu).
    /// </summary>
    /// <param name="name">Der Name des Fahrzeugs.</param>
    protected void ReopenEntries(string name)
    {
        Tankbook.LeaveEntries();
        Tankbook.OpenEntries(name);
        WaitForAutomationId($"{Prefix}Start");
    }

    /// <summary>
    /// Wählt im Kalender des Datumsfelds den 15. eines Monats vor dem angegebenen Monat (nur UI-Automation-Muster: Aufruf des Felds, Vor- und Zurückschalten
    /// des Monats und Auswahl des Tages über das SelectionItemPattern). Der 15. kommt im Monatsraster genau einmal vor.
    /// </summary>
    /// <param name="pickerId">Die AutomationId des Datumsfelds.</param>
    /// <param name="before">Der Monat (erster Tag), vor dem der gewählte Monat liegen soll.</param>
    /// <returns>Das gewählte Datum.</returns>
    protected DateTime PickFifteenthBefore(string pickerId, DateTime before)
    {
        var german = CultureInfo.GetCultureInfo("de-DE");
        var firstOfMonth = new DateTime(before.Year, before.Month, 1);
        WaitForAutomationId(pickerId).Patterns.Invoke.Pattern.Invoke();
        var view = WaitForAutomationId("CalendarView");

        // Der Kalender aktualisiert Überschrift und Tage nach dem Monatswechsel verzögert, und ein Aufruf kurz nach dem Öffnen wird mitunter nicht umgesetzt:
        // Deshalb wird der Wechsel wiederholt, bis die Überschrift einen Monat vor dem Zielmonat zeigt.
        for (var attempt = 0; attempt < 12 && ReadCalendarMonth(view, german) >= firstOfMonth; attempt++)
        {
            var shown = view.FindFirstDescendant(cf => cf.ByAutomationId("HeaderButton"))?.Name;
            WaitForAutomationId("PreviousButton").Patterns.Invoke.Pattern.Invoke();
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(4);
            while (DateTime.UtcNow < deadline && view.FindFirstDescendant(cf => cf.ByAutomationId("HeaderButton"))?.Name == shown)
            {
                Thread.Sleep(200);
            }
        }

        Assert.True(ReadCalendarMonth(view, german) < firstOfMonth, "Der Kalender zeigt keinen früheren Monat an.");
        WaitUntil(
            () => view.FindAllDescendants(cf => cf.ByName("15")).Any(day => day.Patterns.SelectionItem.IsSupported),
            "Der Kalender zeigt den 15. nicht an.");
        view.FindAllDescendants(cf => cf.ByName("15")).First(day => day.Patterns.SelectionItem.IsSupported).Patterns.SelectionItem.Pattern.Select();
        WaitUntil(() => FindByAutomationId("CalendarView") is null, "Der Kalender wurde nach der Auswahl nicht geschlossen.");
        var picked = ReadDate(pickerId);
        Assert.Equal(15, picked.Day);
        return picked;
    }

    /// <summary>
    /// Liest den Wert eines Datumsfelds (<c>TT.MM.JJJJ</c>) als Datum.
    /// </summary>
    /// <param name="pickerId">Die AutomationId des Datumsfelds.</param>
    /// <returns>Das Datum.</returns>
    protected DateTime ReadDate(string pickerId)
    {
        DateTime? value = null;
        WaitUntil(
            () => DateTime.TryParseExact(FindByAutomationId(pickerId)?.Patterns.Value.PatternOrDefault?.Value.ValueOrDefault, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) && (value = parsed) is not null,
            $"Das Datumsfeld '{pickerId}' zeigt kein Datum.");
        return value!.Value;
    }

    private static string Bracket(string text)
    {
        return string.Concat("(", text, ")");
    }

    private static DateTime ReadCalendarMonth(AutomationElement view, CultureInfo culture)
    {
        DateTime? month = null;
        WaitUntil(
            () =>
            {
                var header = view.FindFirstDescendant(cf => cf.ByAutomationId("HeaderButton"))?.Name;
                var text = header?.Replace("‎", string.Empty, StringComparison.Ordinal);
                return DateTime.TryParseExact(text, "MMMM yyyy", culture, DateTimeStyles.None, out var parsed) && (month = parsed) is not null;
            },
            "Der Kalender zeigt keinen Monat an.");
        return month!.Value;
    }

    private void Choose(string automationId)
    {
        Press(automationId);
        WaitUntil(() => IsChosen(automationId), $"Die Option '{automationId}' wurde nicht ausgewählt.");
    }
}
