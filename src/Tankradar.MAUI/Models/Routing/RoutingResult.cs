namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Das Ergebnis einer Routenberechnung. Die Route lebt nur im Arbeitsspeicher und wird nie gespeichert oder protokolliert.
/// </summary>
public sealed class RoutingResult
{
    private RoutingResult(RoutingFailure failure, RouteProfile? route)
    {
        Failure = failure;
        Route = route;
    }

    /// <summary>
    /// Der Grund eines Fehlschlags; <see cref="RoutingFailure.None"/> bei Erfolg.
    /// </summary>
    public RoutingFailure Failure { get; }

    /// <summary>
    /// Die berechnete Route; nur bei Erfolg gesetzt.
    /// </summary>
    public RouteProfile? Route { get; }

    /// <summary>
    /// Erstellt ein erfolgreiches Ergebnis.
    /// </summary>
    /// <param name="route">Die Route.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RoutingResult Success(RouteProfile route)
    {
        ArgumentNullException.ThrowIfNull(route);
        return new RoutingResult(RoutingFailure.None, route);
    }

    /// <summary>
    /// Erstellt ein Ergebnis ohne Route.
    /// </summary>
    /// <param name="failure">Der Grund; nicht <see cref="RoutingFailure.None"/>.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RoutingResult Fail(RoutingFailure failure)
    {
        if (failure == RoutingFailure.None)
        {
            throw new ArgumentException("Ein Fehlschlag darf nicht den Grund 'None' tragen.", nameof(failure));
        }

        return new RoutingResult(failure, null);
    }

    /// <summary>
    /// Gibt nur den Fehlergrund zurück, damit die Route nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Der Fehlergrund als Text.</returns>
    public override string ToString()
    {
        return Failure.ToString();
    }
}
