using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.MAUI.Models.Routing;

/// <summary>
/// Das Ergebnis einer Routensuche. Route, Orte und Stationen leben nur im Arbeitsspeicher; es wird nichts davon gespeichert oder protokolliert.
/// </summary>
public sealed class RouteSearchResult
{
    private RouteSearchResult(RouteSearchStatus status)
    {
        Status = status;
    }

    /// <summary>
    /// Der Ausgang der Suche.
    /// </summary>
    public RouteSearchStatus Status { get; }

    /// <summary>
    /// Die berechnete Route; bei <see cref="RouteSearchStatus.Success"/> gesetzt.
    /// </summary>
    public RouteProfile? Route { get; private init; }

    /// <summary>
    /// Der aufgelöste Name des Starts (Anzeigetext); kann leer sein.
    /// </summary>
    public string? StartName { get; private init; }

    /// <summary>
    /// Der aufgelöste Name des Ziels (Anzeigetext); kann leer sein.
    /// </summary>
    public string? DestinationName { get; private init; }

    /// <summary>
    /// Die Tankstellen im Abstandsband, nach Streckenposition sortiert.
    /// </summary>
    public IReadOnlyList<RouteStation> Stations { get; private init; } = [];

    /// <summary>
    /// Die Position des Starts; bei <see cref="RouteSearchStatus.Success"/> gesetzt.
    /// </summary>
    public GeoPosition? StartPosition { get; private init; }

    /// <summary>
    /// Die Position des Ziels; bei <see cref="RouteSearchStatus.Success"/> gesetzt.
    /// </summary>
    public GeoPosition? DestinationPosition { get; private init; }

    /// <summary>
    /// Der Grund bei <see cref="RouteSearchStatus.GeocodingFailed"/>.
    /// </summary>
    public GeocodingStatus GeocodingStatus { get; private init; } = GeocodingStatus.Found;

    /// <summary>
    /// Der Grund bei <see cref="RouteSearchStatus.RoutingFailed"/>.
    /// </summary>
    public RoutingFailure RoutingFailure { get; private init; }

    /// <summary>
    /// Der Grund bei <see cref="RouteSearchStatus.PriceFailed"/>.
    /// </summary>
    public PriceFailure PriceFailure { get; private init; }

    /// <summary>
    /// Die Länge der Route in Kilometern; bei <see cref="RouteSearchStatus.Success"/> gesetzt, sonst 0.
    /// </summary>
    public double LengthKm
    {
        get { return Route?.LengthKm ?? 0; }
    }

    /// <summary>
    /// Erstellt ein erfolgreiches Ergebnis.
    /// </summary>
    /// <param name="route">Die Route.</param>
    /// <param name="start">Die Startposition.</param>
    /// <param name="destination">Die Zielposition.</param>
    /// <param name="startName">Der aufgelöste Startname.</param>
    /// <param name="destinationName">Der aufgelöste Zielname.</param>
    /// <param name="stations">Die Tankstellen im Abstandsband.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RouteSearchResult Success(
        RouteProfile route,
        GeoPosition start,
        GeoPosition destination,
        string? startName,
        string? destinationName,
        IReadOnlyList<RouteStation> stations)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(stations);
        return new RouteSearchResult(RouteSearchStatus.Success)
        {
            Route = route,
            StartPosition = start,
            DestinationPosition = destination,
            StartName = startName,
            DestinationName = destinationName,
            Stations = stations,
        };
    }

    /// <summary>
    /// Erstellt ein Ergebnis ohne Stationen mit einem einfachen Fehlerstatus.
    /// </summary>
    /// <param name="status">Der Status; nicht <see cref="RouteSearchStatus.Success"/>.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RouteSearchResult Fail(RouteSearchStatus status)
    {
        if (status == RouteSearchStatus.Success)
        {
            throw new ArgumentException("Ein Fehlschlag darf nicht den Status 'Success' tragen.", nameof(status));
        }

        return new RouteSearchResult(status);
    }

    /// <summary>
    /// Erstellt ein Ergebnis für eine fehlgeschlagene Ortsauflösung.
    /// </summary>
    /// <param name="geocoding">Der Grund der Auflösung.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RouteSearchResult FailGeocoding(GeocodingStatus geocoding)
    {
        return new RouteSearchResult(RouteSearchStatus.GeocodingFailed) { GeocodingStatus = geocoding };
    }

    /// <summary>
    /// Erstellt ein Ergebnis für eine fehlgeschlagene Routenberechnung.
    /// </summary>
    /// <param name="routing">Der Grund der Routenberechnung.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RouteSearchResult FailRouting(RoutingFailure routing)
    {
        return new RouteSearchResult(RouteSearchStatus.RoutingFailed) { RoutingFailure = routing };
    }

    /// <summary>
    /// Erstellt ein Ergebnis für einen fehlgeschlagenen Tankstellenabruf.
    /// </summary>
    /// <param name="price">Der Grund des Abrufs.</param>
    /// <returns>Das Ergebnis.</returns>
    public static RouteSearchResult FailPrice(PriceFailure price)
    {
        return new RouteSearchResult(RouteSearchStatus.PriceFailed) { PriceFailure = price };
    }

    /// <summary>
    /// Gibt nur den Status zurück, damit Orte und Route nicht versehentlich protokolliert werden.
    /// </summary>
    /// <returns>Der Status als Text.</returns>
    public override string ToString()
    {
        return Status.ToString();
    }
}
