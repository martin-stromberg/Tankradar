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
/// <returns>Der Wert.</returns>
public sealed record StationPriceLine(FuelType FuelType, string FuelLabel, decimal Price, string PriceText, string AgeText, bool IsStale);
