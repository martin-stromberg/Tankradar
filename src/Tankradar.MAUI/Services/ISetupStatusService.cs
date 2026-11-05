namespace Tankradar.MAUI.Services;

/// <summary>
/// Ermittelt, ob die Einrichtung beim ersten Start (Wahl der Spritsorten) noch aussteht.
/// </summary>
public interface ISetupStatusService
{
    /// <summary>
    /// Prüft, ob die Einrichtung nötig ist. Das ist der Fall, solange keine Einstellungszeile gespeichert ist.
    /// </summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns><see langword="true"/>, wenn die Einrichtung angezeigt werden soll, sonst <see langword="false"/>.</returns>
    Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default);
}
