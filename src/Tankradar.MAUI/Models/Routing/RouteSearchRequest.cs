using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Der Auftrag einer Routensuche. Start, Ziel und Positionen leben nur im Arbeitsspeicher und werden nie gespeichert oder protokolliert.
/// </summary>
/// <param name="StartPosition">Die Startposition (aktueller Standort); <see langword="null"/>, wenn der Start über <paramref name="StartText"/> aufgelöst wird.</param>
/// <param name="StartText">Der Starttext; wird nur ohne <paramref name="StartPosition"/> verwendet.</param>
/// <param name="DestinationText">Der Zieltext.</param>
/// <param name="DistanceKm">Der Abstand zur Route in Kilometern.</param>
/// <param name="FuelTypes">Die gewünschten Spritsorten.</param>
/// <returns>Der Wert.</returns>
public sealed record RouteSearchRequest(
    GeoPosition? StartPosition,
    string? StartText,
    string? DestinationText,
    int DistanceKm,
    IReadOnlyList<FuelType> FuelTypes)
{
    /// <summary>
    /// Gibt eine Darstellung ohne Orte zurück, damit der Auftrag nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Ein fester Text.</returns>
    public override string ToString()
    {
        return nameof(RouteSearchRequest);
    }
}
