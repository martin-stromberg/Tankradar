namespace Tankradar.MAUI.Services;

/// <summary>
/// Abstraktion zur Ermittlung des aktuell gültigen App-Datenverzeichnisses.
/// </summary>
public interface IAppDataPathProvider
{
    /// <summary>
    /// Ermittelt das aktuell gültige App-Datenverzeichnis.
    /// </summary>
    /// <returns>Der Pfad zum App-Datenverzeichnis.</returns>
    string GetDataDirectory();

    /// <summary>
    /// Ermittelt das Verzeichnis für wiederherstellbare Zwischenspeicher (z. B. Kartenkacheln). Es liegt unter iOS außerhalb der Datensicherung.
    /// </summary>
    /// <returns>Der Pfad zum Zwischenspeicher-Verzeichnis.</returns>
    string GetCacheDirectory();
}
