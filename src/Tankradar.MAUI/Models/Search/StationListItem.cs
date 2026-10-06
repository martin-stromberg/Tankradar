namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Eine Tankstelle in der Ergebnisliste der Umkreissuche. Fehlende Quelldaten sind leer bzw. <see langword="null"/>, nie durch Platzhalter ersetzt.
/// </summary>
/// <param name="Id">Die Kennung der Tankstelle.</param>
/// <param name="Name">Der Name.</param>
/// <param name="DistanceKm">Die Entfernung in Kilometern; <see langword="null"/>, wenn unbekannt.</param>
/// <param name="DistanceText">Die Entfernung als Text; leer, wenn unbekannt.</param>
/// <param name="PriceLines">Die Preiszeilen in der Reihenfolge der Einstellungen.</param>
/// <param name="HasUnconfirmedPrice">Gibt an, ob der Hinweis „Preis unbestätigt“ gilt.</param>
/// <param name="IsAutomatedStation">Gibt an, ob der Hinweis „Automatentankstelle“ gilt.</param>
/// <param name="OpeningStatusText">Der Öffnungsstatus als Text; leer, wenn die Quelle ihn nicht liefert.</param>
/// <param name="AddressText">Die Adresszeile; leer, wenn die Quelle keine Adresse liefert.</param>
/// <param name="Latitude">Der Breitengrad der Tankstelle; <see langword="null"/>, wenn die Quelle keine gültige Position liefert (die Tankstelle erscheint dann nicht auf der Karte).</param>
/// <param name="Longitude">Der Längengrad der Tankstelle; <see langword="null"/>, wenn die Quelle keine gültige Position liefert.</param>
/// <param name="IsOpen">Gibt an, ob die Tankstelle laut Quelle geöffnet ist; <see langword="null"/>, wenn unbekannt.</param>
/// <returns>Der Wert.</returns>
public sealed record StationListItem(
    string Id,
    string Name,
    double? DistanceKm,
    string DistanceText,
    IReadOnlyList<StationPriceLine> PriceLines,
    bool HasUnconfirmedPrice,
    bool IsAutomatedStation,
    string OpeningStatusText,
    string AddressText = "",
    double? Latitude = null,
    double? Longitude = null,
    bool? IsOpen = null)
{
    /// <summary>
    /// Gibt an, ob eine gültige Position vorliegt (Voraussetzung für die Markierung auf der Karte).
    /// </summary>
    public bool HasPosition => Latitude is not null && Longitude is not null;

    /// <summary>
    /// Gibt an, ob eine Adresse vorliegt.
    /// </summary>
    public bool HasAddress => AddressText.Length > 0;

    /// <summary>
    /// Gibt an, ob die Entfernung bekannt ist.
    /// </summary>
    public bool HasDistance => DistanceText.Length > 0;

    /// <summary>
    /// Gibt an, ob ein Öffnungsstatus vorliegt.
    /// </summary>
    public bool HasOpeningStatus => OpeningStatusText.Length > 0;

    /// <summary>
    /// Gibt an, ob mindestens ein Hinweis angezeigt wird.
    /// </summary>
    public bool HasHints => HasUnconfirmedPrice || IsAutomatedStation;
}
