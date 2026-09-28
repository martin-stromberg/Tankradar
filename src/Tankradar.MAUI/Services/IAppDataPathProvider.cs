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
}
