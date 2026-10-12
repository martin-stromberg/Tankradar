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
            var originalHeight = MainWindow.BoundingRectangle.Height;

            foreach (var width in new[] { 360, 390 })
            {
                if (resizable)
                {
                    MainWindow.Patterns.Transform.Pattern.Resize(width * scale, originalHeight);
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
        WaitUntil(
            () => label.BoundingRectangle.Height <= fontSizePixels * 2.0,
            $"Die Zahl '{element}' ist {label.BoundingRectangle.Height} Pixel hoch (erlaubt {fontSizePixels * 2.0:0}, zwei Zeilen wären mindestens doppelt so hoch wie eine): Sie bricht um.");
        AssertInsideWindow(label);
    }

    private void AssertInsideWindow(AutomationElement label)
    {
        var window = MainWindow.BoundingRectangle;
        var bounds = label.BoundingRectangle;
        Assert.True(bounds.Left >= window.Left - 1 && bounds.Right <= window.Right + 1, $"Die Zahl ragt aus dem Fenster: {bounds} gegenüber {window}.");
    }
}
