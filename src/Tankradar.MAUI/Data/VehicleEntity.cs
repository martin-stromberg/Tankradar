namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>Vehicles</c>: die Fahrzeuge des Tankbuchs. Der Name ist ohne Beachtung der Groß- und Kleinschreibung eindeutig.
/// </summary>
public class VehicleEntity
{
    /// <summary>
    /// Primärschlüssel.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Name des Fahrzeugs.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Name des <see cref="Models.Tankbook.VehicleFuelKind"/>-Werts.
    /// </summary>
    public string FuelKind { get; set; } = string.Empty;

    /// <summary>
    /// Start-Kilometerstand in ganzen Kilometern.
    /// </summary>
    public int StartOdometerKm { get; set; }

    /// <summary>
    /// Optionaler Startverbrauch pro 100 km (L bzw. kWh), gespeichert als <c>TEXT</c>.
    /// </summary>
    public decimal? StartConsumption { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC) des Anlegens.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Die Tank- und Ladevorgänge des Fahrzeugs.
    /// </summary>
    public List<TankbookEntryEntity> Entries { get; set; } = [];
}
