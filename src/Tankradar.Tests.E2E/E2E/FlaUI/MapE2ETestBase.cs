using FlaUI.Core.AutomationElements;
using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests der Kartenansicht der Suchergebnisse: Umschalten zwischen Liste und Karte, Lesen der Kartenanzeigen und Bedienen der Karte ausschließlich über UI-Automation-Muster (nie per Mausklick).
/// Die Kacheln liefert der Mock-Kachelserver der Basisklasse; produktive Kachelserver werden nie berührt.
/// </summary>
public abstract class MapE2ETestBase : SearchE2ETestBase
{
    /// <summary>
    /// Die AutomationId der Markierung der Teststation Alpha.
    /// </summary>
    protected const string AlphaMarker = "Map.Marker." + MockTankerkoenigServer.StationAlpha;

    /// <summary>
    /// Die AutomationId der Markierung der Teststation Beta.
    /// </summary>
    protected const string BetaMarker = "Map.Marker." + MockTankerkoenigServer.StationBeta;

    /// <summary>
    /// Wechselt auf die Kartenansicht und wartet, bis der Auswahl-Chip „Karte“ ausgewählt ist.
    /// </summary>
    protected void ShowMapView()
    {
        SelectChip("Search.View.Map");
    }

    /// <summary>
    /// Wechselt auf die Listenansicht und wartet, bis der Auswahl-Chip „Liste“ ausgewählt ist.
    /// </summary>
    protected void ShowListView()
    {
        SelectChip("Search.View.List");
    }

    /// <summary>
    /// Wartet, bis der Zähler der Karte den Text zeigt („2 von 2 Stationen sichtbar“).
    /// </summary>
    /// <param name="expected">Der erwartete Text.</param>
    protected void WaitForMapCount(string expected)
    {
        var last = string.Empty;
        WaitUntil(
            () =>
            {
                last = ReadMapCount();
                return last == expected;
            },
            $"Der Kartenzähler lautet nicht '{expected}', zuletzt: '{last}'.");
    }

    /// <summary>
    /// Liefert den Text des Kartenzählers.
    /// </summary>
    /// <returns>Der Text; leer, wenn die Karte nicht angezeigt wird.</returns>
    protected string ReadMapCount()
    {
        try
        {
            return MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("Map.StationCount"))?.Name ?? string.Empty;
        }
        catch (global::FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            // Das Element wird gerade neu aufgebaut; der nächste Abruf liefert den Text.
            return string.Empty;
        }
    }

    /// <summary>
    /// Liefert die Zoomstufe der Karte.
    /// </summary>
    /// <returns>Die Zoomstufe.</returns>
    protected int ReadZoom()
    {
        var text = WaitForAutomationId("Map.Zoom").Name;
        return int.Parse(text.Replace("Zoom ", string.Empty, StringComparison.Ordinal), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Betätigt eine Schaltfläche der Karte.
    /// </summary>
    /// <param name="automationId">Die AutomationId (z. B. <c>Map.ZoomIn</c>).</param>
    protected void PressMapButton(string automationId)
    {
        WaitForAutomationId(automationId).Patterns.Invoke.Pattern.Invoke();
    }

    /// <summary>
    /// Liefert den Hinweistext (Preisniveau) der Markierung.
    /// </summary>
    /// <param name="markerId">Die AutomationId der Markierung.</param>
    /// <returns>Der Hinweistext.</returns>
    protected string ReadMarkerLevel(string markerId)
    {
        return WaitForAutomationId(markerId).Properties.HelpText.ValueOrDefault ?? string.Empty;
    }

    /// <summary>
    /// Sucht am Teststandort mit dem Standardradius, schaltet auf die Karte um und wartet auf beide Teststationen.
    /// </summary>
    protected void SearchAndShowMap()
    {
        OpenSearch();
        Submit();
        WaitForStationNames("Alpha Tankstelle", "Beta Tankstelle");
        ShowMapView();
        WaitForMapCount("2 von 2 Stationen sichtbar");
    }

    /// <summary>
    /// Liefert alle Markierungen der Karte.
    /// </summary>
    /// <returns>Die Elemente der sichtbaren Markierungen.</returns>
    protected IReadOnlyList<AutomationElement> FindMarkers()
    {
        return MainWindow.FindAllDescendants().Where(element => (element.Properties.AutomationId.ValueOrDefault ?? string.Empty).StartsWith("Map.Marker.", StringComparison.Ordinal)).ToList();
    }
}
