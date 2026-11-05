using Tankradar.TestSupport;

namespace Tankradar.Tests.E2E.E2E.FlaUI;

/// <summary>
/// Basis für E2E-Tests der Routensuche: Die App läuft im Testmodus gegen die Mock-Dienste für Preise, Orte, Routen und Kacheln (Berlin-Mitte nach Oranienburg über eine gerade Mock-Route)
/// mit den Tankstellen <see cref="MockTankerkoenigServer.AddRouteStations"/> in definiertem Abstand zur Route. Bedient wird ausschließlich über UI-Automation-Muster
/// (Werte setzen, Invoke); es gibt weder Fokus noch Tastatur noch Maus, und breite Baumsuchen in Wartephasen entfallen (gezielte Suche nach AutomationId).
/// </summary>
public abstract class RouteE2ETestBase : SearchE2ETestBase
{
    /// <summary>
    /// Die Namen der Tankstellen im 2-km-Band in Reihenfolge der Streckenposition.
    /// </summary>
    protected static readonly string[] TwoKilometerStations = ["Alpha Tankstelle", "Beta Tankstelle", "Gamma Tankstelle", "Route Nah", "Epsilon Tankstelle"];

    /// <summary>
    /// Der aufgelöste Name des Starts „10115“ im Nominatim-Mock.
    /// </summary>
    protected const string StartName = "10115, Mitte, Berlin, Deutschland";

    /// <summary>
    /// Der aufgelöste Name des Ziels „Oranienburg“ im Nominatim-Mock.
    /// </summary>
    protected const string DestinationName = "Oranienburg, Landkreis Oberhavel, Brandenburg, Deutschland";

    /// <summary>
    /// Startet die App mit Mock-Servern, den Routen-Teststationen und dem Test-Schlüssel des Routingdienstes.
    /// </summary>
    protected RouteE2ETestBase()
        : this(MockOpenRouteServiceServer.AcceptedKey)
    {
    }

    /// <summary>
    /// Startet die App mit Mock-Servern, den Routen-Teststationen und dem angegebenen Schlüssel des Routingdienstes.
    /// </summary>
    /// <param name="routingKey">Der Test-Schlüssel des Routingdienstes oder <see langword="null"/> für „kein Schlüssel“.</param>
    protected RouteE2ETestBase(string? routingKey)
        : base(routingKey)
    {
        Server.AddRouteStations();
    }

    /// <summary>
    /// Wechselt auf die Suche, wählt die Suchart „Route“ und die Sortierung nach Entfernung (Streckenposition).
    /// </summary>
    protected void OpenRouteSearch()
    {
        OpenSearch();
        SelectSort("Distance");
        SelectRouteMode();
    }

    /// <summary>
    /// Trägt Start (Adresse) und Ziel ein.
    /// </summary>
    /// <param name="start">Der Starttext.</param>
    /// <param name="destination">Der Zieltext.</param>
    protected void FillRoute(string start, string destination)
    {
        SetRouteStart(start);
        SetRouteDestination(destination);
    }

    /// <summary>
    /// Erwartet die Zusammenfassung der Standardroute mit der Zahl der Tankstellen und dem Abstand.
    /// </summary>
    /// <param name="stationCount">Die Zahl der Tankstellen.</param>
    /// <param name="distanceKm">Der Abstand zur Route in Kilometern.</param>
    /// <returns>Der erwartete Text.</returns>
    protected static string Summary(int stationCount, int distanceKm)
    {
        var stations = stationCount == 1 ? "1 Tankstelle" : $"{stationCount} Tankstellen";
        return $"Von {StartName} nach {DestinationName}, 40 km, {stations} im Abstand von {distanceKm} km zur Route";
    }
}
