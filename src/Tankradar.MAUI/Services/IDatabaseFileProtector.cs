namespace Tankradar.MAUI.Services;

/// <summary>
/// Setzt plattformspezifischen Dateischutz auf die Datenbankdateien.
/// </summary>
public interface IDatabaseFileProtector
{
    /// <summary>
    /// Schützt das Datenverzeichnis sowie die Datenbankdatei samt Journaldateien, soweit vorhanden.
    /// </summary>
    /// <param name="databasePath">Der vollständige Pfad der Datenbankdatei.</param>
    void Protect(string databasePath);
}
