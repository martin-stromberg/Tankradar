using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Bei sehr vielen Treffern (300 zusätzliche Stationen im Mock) wird nur eine Seite dargestellt; Filter- und Sortierwechsel bleiben zügig.
/// </summary>
/// <remarks>
/// Der UI-Baum ist mit vielen Listeneinträgen groß, und eine Suche über den ganzen Fensterbaum kann auf einem langsamen Runner (GitHub Actions) in einen
/// UIA-Timeout laufen. Der Test zählt die Einträge deshalb nicht bei jeder Abfrage: Die Schaltfläche „Weitere anzeigen (N übrig)“ nennt (für die Sprachausgabe
/// ohnehin) die Zahl der noch nicht angezeigten Treffer; aus ihr folgt, wie viele angezeigt sind (Gesamtzahl minus N). Das ist eine Eigenschaftsabfrage an einem
/// bereits gefundenen Element. Die Einträge werden nur einmal gezählt (50 nach „Weitere anzeigen“). Nach dem Sortierwechsel liest der Test keine Listeneinträge mehr: Auf dem CI-Runner
/// antwortete die App nach dem Neuaufbau der Liste mehrfach minutenlang nicht auf Baumsuchen (Läufe 40167639322 und 40172732055, auch FindFirst). Dass der
/// Wechsel die Ansicht neu aufbaut, belegt die Rückkehr der Schaltfläche auf die Zahl der ersten Seite bei genau einer Listenanfrage.
/// Die Wartephasen verwenden eine kurze UIA-Antwortfrist (<see cref="PollTimeout"/>), damit <see cref="TransientWait"/> innerhalb der Gesamtfrist mehrfach neu versuchen kann;
/// ein nicht mehr verfügbares Element (Liste wird neu aufgebaut) gilt dort wie ein UIA-Timeout als „noch nicht erfüllt“. Die Zeitgrenze des Sortierwechsels bezieht sich auf
/// die Messzeit abzüglich der in vorübergehenden Fehlern verbrachten Zeit (<c>timeoutTime</c> von <see cref="TransientWait"/>); eine Ausnahme für UIA-Timeouts gibt es nicht mehr.
/// Einen Container „Search.Results“ gibt es im UIA-Baum nicht (das <c>VerticalStackLayout</c> erzeugt kein Automation-Element), die Suche wird daher nicht auf ihn begrenzt.
/// </remarks>
public partial class SearchE2ETests_LargeResult : SearchE2ETestBase
{
    private const int PageSize = 25;

    // Die Antwortfrist einer einzelnen UIA-Anfrage in den Wartephasen.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

    // Die Gesamtfrist einer Wartephase.
    private static readonly TimeSpan PhaseDeadline = TimeSpan.FromSeconds(90);

    // Höchstdauer des Sortierwechsels, gemessen ohne die in UIA-Timeouts verbrachte Zeit.
    private static readonly TimeSpan MaxSortChange = TimeSpan.FromSeconds(10);

    // Antwortfrist einer UIA-Anfrage, die etwas auslöst (Invoke): nicht wiederholbar, daher großzügig.
    private static readonly TimeSpan InvokeTimeout = TimeSpan.FromSeconds(60);

    private AutomationElement? _showMore;

    /// <summary>
    /// Prüft, dass nur die erste Seite angezeigt wird, „Weitere anzeigen“ die nächste ergänzt (50 Einträge) und ein Sortierwechsel zügig auf die erste Seite zurückführt.
    /// </summary>
    [Fact]
    public void LargeResult_ShowsOnePageAndSortChangeStaysFast()
    {
        Server.AddGeneratedStations(300);
        RunWithDiagnostics(() =>
        {
            OpenSearch();

            // Das Element der Sortierung wird vor der großen Ergebnisliste gesucht, danach sucht der Test nur noch am Rand des Baums.
            var sortChip = WaitForAutomationId("Search.Sort.Name");
            Automation.TransactionTimeout = InvokeTimeout;
            Submit();
            Automation.TransactionTimeout = PollTimeout;

            var remainingOnFirstPage = WaitForRemaining(null, "Die erste Seite der Ergebnisliste wurde nicht angezeigt (keine Schaltfläche „Weitere anzeigen“).").Remaining;
            Assert.True(remainingOnFirstPage >= 300 - PageSize, $"Es sollten mindestens {300 - PageSize} Treffer fehlen, gemeldet: {remainingOnFirstPage}.");

            // Nach „Weitere anzeigen“ und Ruhe der App einmal zählen: Die Zahl der Schaltfläche allein bewiese nicht, dass wirklich 50 Einträge aufgebaut sind.
            InvokeWithLongTimeout(_showMore!);
            WaitForRemaining(remainingOnFirstPage - PageSize, "Die zweite Seite wurde nicht angezeigt.");
            CountEntries(2 * PageSize);

            var stopwatch = Stopwatch.StartNew();
            InvokeWithLongTimeout(sortChip);
            var change = WaitForRemaining(remainingOnFirstPage, "Nach dem Sortierwechsel wird nicht die erste Seite angezeigt.");
            stopwatch.Stop();

            var measured = stopwatch.Elapsed - change.UiaWaiting;
            Assert.True(
                measured < MaxSortChange,
                $"Der Sortierwechsel dauerte {stopwatch.Elapsed.TotalSeconds:0.0} s, davon {change.UiaWaiting.TotalSeconds:0.0} s Wartezeit auf die UI-Automation (UIA-Timeouts oder neu aufgebaute Elemente); ohne sie {measured.TotalSeconds:0.0} s (erlaubt {MaxSortChange.TotalSeconds:0} s; App zu langsam oder UI-Thread blockiert).");
            Assert.Equal(1, Server.ListRequests);
        });
    }

    private void CountEntries(int expected)
    {
        Automation.TransactionTimeout = TimeSpan.FromSeconds(30);
        try
        {
            WaitUntil(() => FindAllOrdered("Search.Station.Name").Count == expected, $"Die Liste zeigt nicht genau {expected} Einträge.", TimeSpan.FromSeconds(120));
        }
        finally
        {
            Automation.TransactionTimeout = PollTimeout;
        }
    }

    private void InvokeWithLongTimeout(AutomationElement element)
    {
        // Ein Invoke darf nicht wiederholt werden (ein zweites "Weitere anzeigen" lüde eine dritte Seite); es bekommt daher die lange Antwortfrist.
        Automation.TransactionTimeout = InvokeTimeout;
        try
        {
            element.Patterns.Invoke.Pattern.Invoke();
        }
        finally
        {
            Automation.TransactionTimeout = PollTimeout;
        }
    }

    [GeneratedRegex(@"\((\d[\d.]*) übrig\)")]
    private static partial Regex RemainingPattern();

    /// <summary>
    /// Wartet, bis die Schaltfläche „Weitere anzeigen“ die erwartete Zahl noch nicht angezeigter Treffer nennt (ohne Angabe: irgendeine Zahl).
    /// Trennt dabei die Wartezeit auf die UI-Automation von der Dauer der App und meldet einen UIA-Timeout getrennt von einer zu langsamen App.
    /// </summary>
    /// <param name="expected">Die erwartete Zahl oder <see langword="null"/>.</param>
    /// <param name="failureMessage">Die Beschreibung des erwarteten Zustands für die Fehlermeldung.</param>
    /// <returns>Die gelesene Zahl und die in vorübergehenden UIA-Fehlern verbrachte Zeit.</returns>
    private PhaseResult WaitForRemaining(int? expected, string failureMessage)
    {
        var seen = "keine Schaltfläche";
        var total = Stopwatch.StartNew();
        var result = 0;
        var reached = TransientWait.Until(
            () =>
            {
                var remaining = ReadRemaining();
                seen = remaining?.ToString(CultureInfo.InvariantCulture) ?? "keine Schaltfläche";
                if (remaining is { } value && (expected is null || value == expected))
                {
                    result = value;
                    return true;
                }

                return false;
            },
            PhaseDeadline,
            out var lastTimeout,
            out var uiaWaiting);

        if (!reached)
        {
            var uia = lastTimeout is null
                ? "Die UI-Automation hat jeweils geantwortet."
                : $"Die UI-Automation antwortete zeitweise nicht rechtzeitig oder das Element war nicht verfügbar (zusammen {uiaWaiting.TotalSeconds:0.0} s): {lastTimeout.Message}";
            Assert.Fail($"{failureMessage} Nach {total.Elapsed.TotalSeconds:0.0} s zuletzt gelesen: {seen}, erwartet: {expected?.ToString(CultureInfo.InvariantCulture) ?? "eine Zahl"}. {uia}");
        }

        return new PhaseResult(result, uiaWaiting);
    }

    /// <summary>
    /// Liest die Zahl der noch nicht angezeigten Treffer aus dem Namen der Schaltfläche „Weitere anzeigen (N übrig)“.
    /// Das einmal gefundene Element wird weiterverwendet (kein Durchsuchen des großen Baums je Abfrage); nur wenn es nicht mehr verfügbar ist, wird neu gesucht.
    /// </summary>
    /// <returns>Die Zahl oder <see langword="null"/>, wenn die Schaltfläche nicht (mehr) vorhanden ist.</returns>
    private int? ReadRemaining()
    {
        string? name = null;
        if (_showMore is not null)
        {
            try
            {
                name = _showMore.Properties.Name.ValueOrDefault;
            }
            catch (Exception ex) when (ex is COMException or global::FlaUI.Core.Exceptions.ElementNotAvailableException)
            {
                // Das Element wurde neu aufgebaut. Ein UIA-Timeout (ebenfalls eine COMException) wird nicht verdeckt, sondern an die Wartephase weitergereicht.
                if (TransientRetry.IsTransient(ex))
                {
                    throw;
                }

                _showMore = null;
            }
        }

        if (name is null || !RemainingPattern().IsMatch(name))
        {
            // Nicht gelesen (Element neu aufgebaut, Eigenschaft kurz nicht unterstützt) oder noch ohne Zahl: neu suchen.
            _showMore = FindByAutomationId("Search.ShowMore");
            name = _showMore?.Properties.Name.ValueOrDefault;
        }

        var match = name is null ? null : RemainingPattern().Match(name);
        return match is { Success: true } ? int.Parse(match.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture) : null;
    }

    private sealed class PhaseResult
    {
        public PhaseResult(int remaining, TimeSpan uiaWaiting)
        {
            Remaining = remaining;
            UiaWaiting = uiaWaiting;
        }

        public int Remaining { get; }

        public TimeSpan UiaWaiting { get; }
    }
}
