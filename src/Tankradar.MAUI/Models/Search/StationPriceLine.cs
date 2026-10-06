namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Eine Preiszeile einer Tankstelle in der Ergebnisliste.
/// </summary>
/// <param name="FuelType">Die Spritsorte.</param>
/// <param name="FuelLabel">Der Anzeigename der Sorte.</param>
/// <param name="Price">Der Preis in Euro je Liter.</param>
/// <param name="PriceText">Der Preis als Text mit Eurozeichen (deutsche Schreibweise).</param>
/// <param name="AgeText">Das Alter des Preises („vor X Min.“).</param>
/// <param name="IsStale">Gibt an, ob der Preis veraltet ist (mindestens 60 Minuten).</param>
/// <param name="Age">Das Alter des Preises (für die Preisaktualität der Detailansicht); die Null-Zeitspanne bei Preisen ohne bekanntes Alter.</param>
/// <returns>Der Wert.</returns>
public sealed record StationPriceLine(FuelType FuelType, string FuelLabel, decimal Price, string PriceText, string AgeText, bool IsStale, TimeSpan Age = default)
{
    private static readonly System.Globalization.CultureInfo German = System.Globalization.CultureInfo.GetCultureInfo("de-DE");

    private string Formatted => Price.ToString("0.000", German);

    /// <summary>
    /// Der Preis ohne die dritte Nachkommastelle („1,85“) für die Darstellung im Designentwurf.
    /// </summary>
    public string PriceMainText => Formatted[..^1];

    /// <summary>
    /// Die dritte Nachkommastelle („9“), die in der Detailansicht hochgestellt dargestellt wird.
    /// </summary>
    public string PriceFractionText => Formatted[^1..];

    /// <summary>
    /// Die Einheit des Preises („€/L“).
    /// </summary>
    public string PriceUnitText => Resources.Texts.DetailTexts.PriceUnit;

    /// <summary>
    /// Der Preis für Bedienhilfen („1,859 Euro pro Liter“), da die Anzeige aus mehreren Teilen besteht.
    /// </summary>
    public string PriceSpokenText => Formatted + " Euro pro Liter";
}
