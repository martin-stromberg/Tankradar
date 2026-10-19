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
/// UIA-Timeout laufen. Der Test zählt die Einträge deshalb nicht bei jeder Abfrage: Die Schaltfläche „Weitere anzeigen (N weitere)“ nennt (für die Sprachausgabe
/// ohnehin) die Zahl der noch nicht angezeigten Treffer; aus ihr folgt, wie viele angezeigt sind (Gesamtzahl minus N). Das ist eine Eigenschaftsabfrage an einem
/// bereits gefundenen Element. Die Einträge werden nur einmal am Ende gezählt. Die Wartephasen verwenden eine kurze UIA-Antwortfrist (<see cref="PollTimeout"/>),
/// damit <see cref="TransientWait"/> innerhalb der Gesamtfrist mehrfach neu versuchen kann; ein UIA-Timeout wird getrennt von einer zu langsamen App gemeldet.
/// Einen Container „Search.Results“ gibt es im UIA-Baum nicht (das <c>VerticalStackLayout</c> erzeugt kein Automation-Element), die Suche wird daher nicht auf ihn begrenzt.
/// </remarks>
public partial class SearchE2ETests_LargeResult : SearchE2ETestBase
{
    private const int PageSize = 25;

    // Die Antwortfrist einer einzelnen UIA-Anfrage in den Wartephasen.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(5);

    // Die Gesamtfrist einer Wartephase.
    private static readonly TimeSpan PhaseDeadline = TimeSpan.FromSeconds(90);

    private static readonly TimeSpan MaxSortChange = TimeSpan.FromSeconds(10);

    private AutomationElement? _showMore;

    /// <summary>
    /// Prüft, dass nur die erste Seite angezeigt wird, „Weitere anzeigen“ die nächste ergänzt und ein Sortierwechsel zügig auf die erste Seite zurückführt.
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
            Automation.TransactionTimeout = PollTimeout;
            Submit();

            var remainingOnFirstPage = WaitForRemaining(null, "Die erste Seite der Ergebnisliste wurde nicht angezeigt (keine Schaltfläche „Weitere anzeigen“).").Remaining;
            Assert.True(remainingOnFirstPage >= 300 - PageSize, $"Es sollten mindestens {300 - PageSize} Treffer fehlen, gemeldet: {remainingOnFirstPage}.");

            _showMore!.Patterns.Invoke.Pattern.Invoke();
            WaitForRemaining(remainingOnFirstPage - PageSize, "Die zweite Seite wurde nicht angezeigt.");

            var stopwatch = Stopwatch.StartNew();
            sortChip.Patterns.Invoke.Pattern.Invoke();
            var change = WaitForRemaining(remainingOnFirstPage, "Nach dem Sortierwechsel wird nicht die erste Seite angezeigt.");
            stopwatch.Stop();

            var appTime = stopwatch.Elapsed - change.UiaWaiting;
            Assert.True(
                appTime < MaxSortChange,
                $"Der Sortierwechsel dauerte {appTime.TotalSeconds:0.0} s in der App (Gesamt {stopwatch.Elapsed.TotalSeconds:0.0} s, davon {change.UiaWaiting.TotalSeconds:0.0} s Wartezeit auf die UI-Automation in {change.UiaTimeouts} Timeouts).");

            // Einmal zählen, wie viele Einträge tatsächlich in der Liste stehen (die Zahl der Schaltfläche allein bewiese nicht, dass nur eine Seite aufgebaut ist).
            Automation.TransactionTimeout = TimeSpan.FromSeconds(30);
            WaitUntil(() => FindAllOrdered("Search.Station.Name").Count == PageSize, $"Die Liste zeigt nicht genau {PageSize} Einträge.", TimeSpan.FromSeconds(120));
            Assert.Equal(1, Server.ListRequests);
        });
    }

    [GeneratedRegex(@"\((\d[\d.]*) weitere\)")]
    private static partial Regex RemainingPattern();

    /// <summary>
    /// Wartet, bis die Schaltfläche „Weitere anzeigen“ die erwartete Zahl noch nicht angezeigter Treffer nennt (ohne Angabe: irgendeine Zahl).
    /// Trennt dabei die Wartezeit auf die UI-Automation von der Dauer der App und meldet einen UIA-Timeout getrennt von einer zu langsamen App.
    /// </summary>
    /// <param name="expected">Die erwartete Zahl oder <see langword="null"/>.</param>
    /// <param name="failureMessage">Die Beschreibung des erwarteten Zustands für die Fehlermeldung.</param>
    /// <returns>Die gelesene Zahl, die in UIA-Timeouts verbrachte Zeit und deren Anzahl.</returns>
    private PhaseResult WaitForRemaining(int? expected, string failureMessage)
    {
        var uiaWaiting = TimeSpan.Zero;
        var uiaTimeouts = 0;
        var seen = "keine Schaltfläche";
        var total = Stopwatch.StartNew();
        var result = 0;
        var reached = TransientWait.Until(
            () =>
            {
                var started = Stopwatch.StartNew();
                try
                {
                    var remaining = ReadRemaining();
                    seen = remaining?.ToString(CultureInfo.InvariantCulture) ?? "keine Schaltfläche";
                    if (remaining is { } value && (expected is null || value == expected))
                    {
                        result = value;
                        return true;
                    }

                    return false;
                }
                catch (Exception ex) when (TransientRetry.IsTransient(ex))
                {
                    uiaWaiting += started.Elapsed;
                    uiaTimeouts++;
                    throw;
                }
            },
            PhaseDeadline,
            out var lastTimeout);

        if (!reached)
        {
            var uia = lastTimeout is null
                ? "Die UI-Automation hat jeweils geantwortet."
                : $"Die UI-Automation antwortete {uiaTimeouts}-mal nicht rechtzeitig (UIA-Timeout, zusammen {uiaWaiting.TotalSeconds:0.0} s): {lastTimeout.Message}";
            Assert.Fail($"{failureMessage} Nach {total.Elapsed.TotalSeconds:0.0} s zuletzt gelesen: {seen}, erwartet: {expected?.ToString(CultureInfo.InvariantCulture) ?? "eine Zahl"}. {uia}");
        }

        return new PhaseResult(result, uiaWaiting, uiaTimeouts);
    }

    /// <summary>
    /// Liest die Zahl der noch nicht angezeigten Treffer aus dem Namen der Schaltfläche „Weitere anzeigen (N weitere)“.
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

        if (_showMore is null)
        {
            _showMore = FindByAutomationId("Search.ShowMore");
            name = _showMore?.Properties.Name.ValueOrDefault;
        }

        var match = name is null ? null : RemainingPattern().Match(name);
        return match is { Success: true } ? int.Parse(match.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture) : null;
    }

    private sealed class PhaseResult
    {
        public PhaseResult(int remaining, TimeSpan uiaWaiting, int uiaTimeouts)
        {
            Remaining = remaining;
            UiaWaiting = uiaWaiting;
            UiaTimeouts = uiaTimeouts;
        }

        public int Remaining { get; }

        public TimeSpan UiaWaiting { get; }

        public int UiaTimeouts { get; }
    }
}
