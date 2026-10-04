using Tankradar.MAUI.Models;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Lädt und speichert die App-Einstellungen in der lokalen Datenbank.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Lädt die gespeicherten Einstellungen; fehlende oder ungültige Werte werden durch Standardwerte ersetzt.
    /// </summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die geladenen, vollständigen Einstellungen.</returns>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Speichert die Einstellungen atomar.
    /// </summary>
    /// <param name="settings">Die zu speichernden Einstellungen.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Ein Task, der nach dem Speichern abgeschlossen ist.</returns>
    /// <exception cref="ArgumentException">Die Einstellungen sind ungültig.</exception>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
