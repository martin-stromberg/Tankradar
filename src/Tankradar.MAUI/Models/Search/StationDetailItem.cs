using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Eine Zeile der Öffnungszeiten in der Detailansicht.
/// </summary>
/// <param name="DayText">Die Bezeichnung des Abschnitts (z. B. „Mo-Fr“); leer, wenn die Quelle keine liefert.</param>
/// <param name="TimeText">Die Zeitspanne (z. B. „06:00 – 22:00 Uhr“); leer, wenn die Quelle keine liefert.</param>
/// <param name="DisplayText">Die zusammengesetzte Zeile für die Anzeige.</param>
/// <returns>Der Wert.</returns>
public sealed record OpeningHoursLine(string DayText, string TimeText, string DisplayText);

/// <summary>
/// Die aufbereiteten Angaben der Tankstellen-Detailansicht. Angaben, die die Quelle nicht liefert (oder deren Alter die Grenze überschreitet), sind leer
/// und werden in der Oberfläche ausgeblendet, nie durch Platzhalter ersetzt.
/// </summary>
/// <param name="Id">Die Kennung der Tankstelle.</param>
/// <param name="Name">Der Name.</param>
/// <param name="BrandText">Die Marke; leer, wenn die Quelle keine liefert oder sie dem Namen entspricht.</param>
/// <param name="AddressText">Die Adresszeile; leer, wenn die Quelle keine Adresse liefert.</param>
/// <param name="DistanceText">Die Entfernung als Text; leer, wenn unbekannt.</param>
/// <param name="OpeningStatusText">Der Öffnungsstatus („Geöffnet“ oder „Geschlossen“); leer, wenn die Quelle ihn nicht liefert.</param>
/// <param name="PriceLines">Die Preiszeilen der in den Einstellungen aktivierten Sorten in deren Reihenfolge.</param>
/// <param name="HasUnconfirmedPrice">Gibt an, ob der Hinweis „Preis unbestätigt“ gilt.</param>
/// <param name="IsAutomatedStation">Gibt an, ob der Hinweis „Automatentankstelle“ gilt.</param>
/// <param name="OpeningHours">Die Öffnungszeiten; leer, wenn die Quelle keine liefert oder sie zu alt sind.</param>
/// <param name="OpeningHoursAgeText">Das Alter der Öffnungszeiten („Stand: vor X Min.“); leer, wenn keine Öffnungszeiten angezeigt werden.</param>
/// <returns>Der Wert.</returns>
public sealed record StationDetailItem(
    string Id,
    string Name,
    string BrandText,
    string AddressText,
    string DistanceText,
    string OpeningStatusText,
    IReadOnlyList<StationPriceLine> PriceLines,
    bool HasUnconfirmedPrice,
    bool IsAutomatedStation,
    IReadOnlyList<OpeningHoursLine> OpeningHours,
    string OpeningHoursAgeText)
{
    /// <summary>
    /// Gibt an, ob eine Marke angezeigt wird.
    /// </summary>
    public bool HasBrand => BrandText.Length > 0;

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
    /// Gibt an, ob mindestens ein Preis angezeigt wird.
    /// </summary>
    public bool HasPrices => PriceLines.Count > 0;

    /// <summary>
    /// Gibt an, ob die Zeile mit Öffnungsstatus und Chip „Automat 24/7“ angezeigt wird.
    /// </summary>
    public bool HasOpeningStatusOrAutomat => HasOpeningStatus || IsAutomatedStation;

    /// <summary>
    /// Gibt an, ob die Info-Box (Entfernung, Öffnungsstatus, Automat 24/7) angezeigt wird.
    /// </summary>
    public bool HasInfoBox => HasDistance || HasOpeningStatus || IsAutomatedStation;

    /// <summary>
    /// Gibt an, ob mindestens ein Preis veraltet ist (ab 60 Minuten) und die Aktualität deshalb als Altersangabe statt „Live-Preise“ erscheint.
    /// </summary>
    public bool HasStalePrice
    {
        get
        {
            foreach (var line in PriceLines)
            {
                if (line.IsStale)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gibt an, ob der Chip „Live-Preise“ gilt (mindestens ein Preis, keiner veraltet).
    /// </summary>
    public bool HasLivePrices => HasPrices && !HasStalePrice;

    /// <summary>
    /// Die Preisaktualität: „Live-Preise“, wenn alle Preise frisch sind, sonst die Altersangabe des ältesten Preises („Preise: vor 135 Min.“); leer ohne Preise.
    /// </summary>
    public string PriceStatusText => !HasPrices
        ? string.Empty
        : HasStalePrice
            ? DetailTexts.FormatPriceAge(PriceLines.MaxBy(line => line.Age)!.AgeText)
            : DetailTexts.LivePrices;

    /// <summary>
    /// Gibt an, ob Öffnungszeiten angezeigt werden.
    /// </summary>
    public bool HasOpeningHours => OpeningHours.Count > 0;
}
