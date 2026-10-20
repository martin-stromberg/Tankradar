namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ergebnis der Erzeugung eines Exports.
/// </summary>
/// <param name="Status">Das Ergebnis.</param>
/// <param name="FilePath">Der vollständige Pfad der erzeugten Datei; <see langword="null"/>, wenn keine entstand.</param>
/// <param name="FileName">Der Dateiname; <see langword="null"/>, wenn keine Datei entstand.</param>
/// <param name="EntryCount">Die Zahl der exportierten Vorgänge; 0, wenn keine Datei entstand.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookExportResult(TankbookExportStatus Status, string? FilePath, string? FileName, int EntryCount)
{
    /// <summary>
    /// Erstellt ein Ergebnis ohne Datei.
    /// </summary>
    /// <param name="status">Das Ergebnis.</param>
    /// <returns>Das Ergebnis.</returns>
    public static TankbookExportResult WithoutFile(TankbookExportStatus status)
    {
        return new TankbookExportResult(status, null, null, 0);
    }
}
