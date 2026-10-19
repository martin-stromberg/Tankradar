using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Lässt den Nutzer eine externe Sicherungsdatei auswählen (in der App über den Dateiauswahldialog des Betriebssystems, im Testmodus über eine vorgegebene Datei).
/// </summary>
public interface IBackupFilePicker
{
    /// <summary>
    /// Lässt eine ZIP-Datei auswählen.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Datei oder <see langword="null"/>, wenn der Nutzer abbricht.</returns>
    Task<PickedBackupFile?> PickAsync(CancellationToken cancellationToken = default);
}
