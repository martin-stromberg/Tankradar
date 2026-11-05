namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Dateiformat des Exports des Tankbuchs.
/// </summary>
public enum TankbookExportFormat
{
    /// <summary>
    /// CSV für deutschsprachige Tabellenkalkulationen (UTF-8 mit BOM, Semikolon, Dezimalkomma).
    /// </summary>
    Csv,

    /// <summary>
    /// PDF-Bericht mit Kennzahlen und Vorgängen.
    /// </summary>
    Pdf,
}
