namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>PriceEntries</c>: ein abgerufener Preis einer Sorte mit Zeitstempel. Ältere Stände bleiben für Auswertungen erhalten.
/// </summary>
public class PriceEntryEntity
{
    /// <summary>
    /// Primärschlüssel (fortlaufend).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Kennung der Tankstelle.
    /// </summary>
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Name des <see cref="Models.FuelType"/>-Werts.
    /// </summary>
    public string FuelTypeKey { get; set; } = string.Empty;

    /// <summary>
    /// Preis in Euro je Liter.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Abrufzeitpunkt in UTC.
    /// </summary>
    public DateTime RetrievedUtc { get; set; }

    /// <summary>
    /// Die zugehörige Tankstelle.
    /// </summary>
    public StationEntity? Station { get; set; }
}
