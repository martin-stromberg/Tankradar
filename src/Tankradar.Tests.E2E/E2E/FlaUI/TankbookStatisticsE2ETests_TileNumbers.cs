using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// E2E-Test: Große Kachelzahlen der Auswertung bleiben bei schmaler Fensterbreite einzeilig und innerhalb des Fensters (Restpunkt aus Abnahme 14).
/// </summary>
public class TankbookStatisticsE2ETests_TileNumbers : TankbookE2ETestBase
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Prüft bei 360 und 390 Punkten Breite (sofern die Größenänderung unterstützt wird, sonst bei Standardbreite), dass Verbrauch und Kosten pro 100 km nicht umbrechen.
    /// </summary>
    [Fact]
    public void LongTileNumbers_StayOnOneLine_AtNarrowWidths()
    {
        RunWithDiagnostics(() =>
        {
            OpenTankbook();
            CreateVehicle("Golf", "Diesel", "10000", string.Empty);
            Tankbook.OpenEntries("Golf");
            Tankbook.CreateEntry("Alpha Tankstelle", "10", "1,5", string.Empty, "10100");
            Tankbook.CreateEntry("Alpha Tankstelle", "99,89", "1,5", string.Empty, "10101");
            Tankbook.WaitForStatistic("Fuel", "Average", "9.989,00");

            var scale = GetDpiForWindow(MainWindow.Properties.NativeWindowHandle.ValueOrDefault) / 96.0;
            var resizable = MainWindow.Patterns.Transform.PatternOrDefault is { CanResize.ValueOrDefault: true };
            // Feste, kleine Fensterhöhe (wie im Vordergrundbetrieb des CI-Runners): Die Kacheln liegen dann außerhalb des sichtbaren Bereichs und müssen eingeblendet werden.
            const double WindowHeight = 520;

            foreach (var width in new[] { 360, 390 })
            {
                if (resizable)
                {
                    MainWindow.Patterns.Transform.Pattern.Resize(width * scale, WindowHeight * scale);
                    Thread.Sleep(500);
                }

                AssertSingleLine("Average", 21 * scale);
                AssertSingleLine("CostPer100Km", 18 * scale);
            }
        });
    }

    private void AssertSingleLine(string element, double fontSizePixels)
    {
        var label = WaitForAutomationId(TankbookE2EDriver.StatisticsId("Fuel", element));
        EnsureVisible(label);
        EnsureFullyVisible(label, fontSizePixels);

        // Ein nur teilweise sichtbares Element meldet nur die sichtbare Höhe; deshalb muss die Höhe stabil mindestens eine Schriftzeile betragen
        // und das Element vollständig im Fenster liegen (Härtung H1 aus der Abnahme von Schritt 15a).
        var bounds = ReadStableBounds(label);
        Assert.True(
            bounds.Height >= fontSizePixels,
            $"Die Zahl '{element}' ist nur {bounds.Height} Pixel hoch (mindestens eine Schriftzeile von {fontSizePixels:0} Pixel erwartet): Sie ist nicht vollständig sichtbar.");
        Assert.True(
            bounds.Height <= fontSizePixels * 2.0,
            $"Die Zahl '{element}' ist {bounds.Height} Pixel hoch (erlaubt {fontSizePixels * 2.0:0}, zwei Zeilen wären mindestens doppelt so hoch wie eine): Sie bricht um.");
        AssertInsideWindow(bounds);
    }

    /// <summary>
    /// Bringt das Element in den sichtbaren Bereich: Elemente außerhalb des Sichtfensters der Liste melden leere Maße. Zuerst über das ScrollItemPattern,
    /// sonst durch schrittweises Verschieben der umgebenden Bildlaufbereiche (nur UI-Automation-Muster, keine Maus- oder Tastatureingabe).
    /// </summary>
    /// <param name="label">Das Element.</param>
    private void EnsureVisible(AutomationElement label)
    {
        if (HasBounds(label))
        {
            return;
        }

        if (label.Patterns.ScrollItem.TryGetPattern(out var scrollItem))
        {
            scrollItem.ScrollIntoView();
            Thread.Sleep(300);
            if (HasBounds(label))
            {
                return;
            }
        }

        var scrollers = new List<global::FlaUI.Core.Patterns.IScrollPattern>();
        for (var parent = label.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent.Patterns.Scroll.TryGetPattern(out var scroll) && scroll.VerticallyScrollable.ValueOrDefault)
            {
                scrollers.Add(scroll);
            }
        }

        foreach (var scroll in scrollers)
        {
            for (var percent = 0; percent <= 100; percent += 10)
            {
                scroll.SetScrollPercent(global::FlaUI.Core.Patterns.ScrollPatternConstants.NoScroll, percent);
                Thread.Sleep(150);
                if (HasBounds(label))
                {
                    return;
                }
            }
        }

        WaitUntil(() => HasBounds(label), $"Das Element '{label.AutomationId}' liegt nicht im sichtbaren Bereich (leere Maße).");
    }

    private static bool HasBounds(AutomationElement label)
    {
        var bounds = label.BoundingRectangle;
        return bounds.Width > 0 && bounds.Height > 0;
    }

    /// <summary>
    /// Durchläuft den nächsten senkrecht scrollbaren Bereich von oben nach unten in kleinen Schritten (UI-Automation-Muster, keine Mausräder), bis das Element
    /// mit mindestens der Höhe einer Schriftzeile vollständig im Fenster liegt. Ein Element außerhalb des Sichtbereichs meldet leere Maße, aus denen sich die
    /// Richtung nicht ablesen lässt; das frühere schrittweise Abwärtsscrollen fand eine darüber liegende Zahl nie (CI-Lauf 39379778082: Höhe 0).
    /// </summary>
    /// <param name="label">Das Element.</param>
    /// <param name="minimumHeight">Die kleinste erwartete Höhe in Pixeln.</param>
    private void EnsureFullyVisible(AutomationElement label, double minimumHeight)
    {
        if (IsFullyVisible(label, minimumHeight))
        {
            return;
        }

        global::FlaUI.Core.Patterns.IScrollPattern? scroller = null;
        for (var parent = label.Parent; parent is not null && scroller is null; parent = parent.Parent)
        {
            if (parent.Patterns.Scroll.TryGetPattern(out var scroll) && scroll.VerticallyScrollable.ValueOrDefault)
            {
                scroller = scroll;
            }
        }

        if (scroller is null)
        {
            return;
        }

        for (var percent = 0.0; percent <= 100.0; percent += 2.5)
        {
            scroller.SetScrollPercent(global::FlaUI.Core.Patterns.ScrollPatternConstants.NoScroll, percent);
            if (IsFullyVisible(label, minimumHeight))
            {
                return;
            }
        }
    }

    private bool IsFullyVisible(AutomationElement label, double minimumHeight)
    {
        var bounds = ReadStableBounds(label);
        var window = MainWindow.BoundingRectangle;
        return bounds.Height >= minimumHeight && bounds.Top >= window.Top - 1 && bounds.Bottom <= window.Bottom + 1;
    }

    /// <summary>
    /// Liest die Maße des Elements, bis zwei aufeinanderfolgende Messungen gleich sind (Layout und Bildlauf sind dann abgeschlossen).
    /// </summary>
    /// <param name="label">Das Element.</param>
    /// <returns>Die stabilen Maße.</returns>
    private static System.Drawing.Rectangle ReadStableBounds(AutomationElement label)
    {
        var previous = label.BoundingRectangle;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            Thread.Sleep(100);
            var current = label.BoundingRectangle;
            if (current == previous)
            {
                return current;
            }

            previous = current;
        }

        return previous;
    }

    private void AssertInsideWindow(System.Drawing.Rectangle bounds)
    {
        var window = MainWindow.BoundingRectangle;
        Assert.True(bounds.Left >= window.Left - 1 && bounds.Right <= window.Right + 1, $"Die Zahl ragt seitlich aus dem Fenster: {bounds} gegenüber {window}.");
        Assert.True(bounds.Top >= window.Top - 1 && bounds.Bottom <= window.Bottom + 1, $"Die Zahl ragt oben oder unten aus dem Fenster: {bounds} gegenüber {window}.");
    }
}
