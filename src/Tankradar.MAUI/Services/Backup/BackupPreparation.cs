using Tankradar.MAUI.Models.Backup;

namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Ergebnis der Vorbereitung der Datenbank einer Sicherung.
/// </summary>
/// <param name="Result">Das Ergebnis.</param>
/// <param name="Counts">Die aus der geprüften Datenbank gezählten Mengen (ohne Belegfotos); nur bei <see cref="BackupResult.Ok"/>.</param>
/// <returns>Der Wert.</returns>
public sealed record BackupPreparation(BackupResult Result, BackupManifestCounts? Counts = null);
