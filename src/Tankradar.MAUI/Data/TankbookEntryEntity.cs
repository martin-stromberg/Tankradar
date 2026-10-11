namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>TankbookEntries</c>: ein Tank- oder Ladevorgang eines Fahrzeugs. In Schritt 12 bewusst minimal (Kennung, Fahrzeug, Anlagezeitpunkt),
/// damit das Mitlöschen der Vorgänge beim Löschen eines Fahrzeugs prüfbar ist; Schritt 13 ergänzt die übrigen Angaben per eigener Migration.
/// </summary>
public class TankbookEntryEntity
{
    /// <summary>
    /// Primärschlüssel.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Fremdschlüssel auf <see cref="VehicleEntity"/>; beim Löschen des Fahrzeugs entfallen seine Vorgänge (<c>ON DELETE CASCADE</c>).
    /// </summary>
    public long VehicleId { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC) des Anlegens.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Das Fahrzeug.
    /// </summary>
    public VehicleEntity? Vehicle { get; set; }
}
