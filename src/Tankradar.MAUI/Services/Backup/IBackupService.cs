using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Lokale Sicherung und Wiederherstellung der Daten (Datenbank und Belegfotos) als ZIP-Datei im Datenverzeichnis.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Erstellt eine Sicherung aus einem konsistenten Datenbankstand und allen Belegfotos.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="BackupResult.Ok"/> mit der neuen Sicherung, <see cref="BackupResult.Busy"/> oder <see cref="BackupResult.Failed"/>; bei einem Fehler bleibt keine halbe Datei zurück.</returns>
    Task<BackupOutcome> CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Listet die vorhandenen Sicherungen, neueste zuerst. Nicht lesbare Dateien erscheinen gekennzeichnet.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Einträge.</returns>
    Task<IReadOnlyList<BackupFileInfo>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prüft die Struktur einer Sicherung und liefert ihr Manifest (für die Rückfrage vor der Wiederherstellung); verändert nichts.
    /// </summary>
    /// <param name="fileName">Der Dateiname aus der Liste.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit Datei und Manifest bei <see cref="BackupResult.Ok"/>.</returns>
    Task<BackupOutcome> InspectAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stellt die Daten aus einer Sicherung wieder her: vollständige Prüfung, Entpacken in ein Staging-Verzeichnis, Migration, atomarer Austausch mit Rollback.
    /// </summary>
    /// <param name="fileName">Der Dateiname aus der Liste.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis; außer bei <see cref="BackupResult.Ok"/> sind die bisherigen Daten unverändert.</returns>
    Task<BackupResult> RestoreAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Übernimmt eine externe Datei nach vollständiger Prüfung in die Liste der Sicherungen.
    /// </summary>
    /// <param name="file">Die gewählte Datei.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit der neuen Sicherung bei <see cref="BackupResult.Ok"/>; bei <see cref="BackupResult.TooNew"/> mit dem Manifest.</returns>
    Task<BackupOutcome> ImportAsync(PickedBackupFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht eine Sicherung.
    /// </summary>
    /// <param name="fileName">Der Dateiname aus der Liste.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis; <see cref="BackupResult.NotFound"/> auch bei nicht zulässigen Namen.</returns>
    Task<BackupResult> DeleteAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ermittelt den vollständigen Pfad einer Sicherung (für „Teilen“).
    /// </summary>
    /// <param name="fileName">Der Dateiname aus der Liste.</param>
    /// <returns>Der Pfad oder <see langword="null"/>, wenn der Name nicht zulässig ist oder die Datei fehlt.</returns>
    string? GetFullPath(string fileName);
}
