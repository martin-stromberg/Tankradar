namespace Tankradar.MAUI.Services;

/// <summary>
/// Setzt plattformspezifischen Dateischutz auf die Datenbankdateien und weitere lokal gespeicherte Dateien (z. B. Belegfotos).
/// </summary>
public interface IDatabaseFileProtector
{
    /// <summary>
    /// Schützt das Datenverzeichnis sowie die Datenbankdatei samt Journaldateien, soweit vorhanden.
    /// </summary>
    /// <param name="databasePath">Der vollständige Pfad der Datenbankdatei.</param>
    void Protect(string databasePath);

    /// <summary>
    /// Schützt eine einzelne Datei und ihr Verzeichnis, soweit vorhanden.
    /// </summary>
    /// <param name="filePath">Der vollständige Pfad der Datei.</param>
    void ProtectFile(string filePath);
}
