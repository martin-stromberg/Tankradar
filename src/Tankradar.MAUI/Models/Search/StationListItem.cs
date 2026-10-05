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
    string AddressText = "")
{
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
