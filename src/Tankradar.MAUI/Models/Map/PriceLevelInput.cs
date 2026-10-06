namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Eingabe der Preisniveau-Einstufung für eine Tankstelle.
/// </summary>
/// <param name="Price">Der Preis der maßgeblichen Spritsorte in Euro je Liter; <see langword="null"/>, wenn die Tankstelle diese Sorte nicht führt.</param>
/// <param name="IsClosed">Gibt an, ob die Tankstelle laut Quelle geschlossen ist (unbekannt gilt als nicht geschlossen).</param>
/// <returns>Der Wert.</returns>
public readonly record struct PriceLevelInput(decimal? Price, bool IsClosed);
