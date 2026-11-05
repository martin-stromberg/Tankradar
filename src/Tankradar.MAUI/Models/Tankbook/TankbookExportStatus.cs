namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis der Erzeugung eines Exports.
/// </summary>
public enum TankbookExportStatus
{
    /// <summary>
    /// Die Datei wurde erzeugt.
    /// </summary>
    Exported,

    /// <summary>
    /// Im Zeitraum gibt es keine Vorgänge; es wurde keine Datei erzeugt.
    /// </summary>
    NoEntries,

    /// <summary>
    /// Das Fahrzeug existiert nicht (mehr).
    /// </summary>
    VehicleNotFound,

    /// <summary>
    /// Der Zeitraum ist ungültig (Start nach Ende oder Ende in der Zukunft).
    /// </summary>
    InvalidRange,

    /// <summary>
    /// Die Datei konnte nicht erzeugt werden.
    /// </summary>
    Failed,
}
