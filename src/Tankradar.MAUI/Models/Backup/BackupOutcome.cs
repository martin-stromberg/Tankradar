namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Ergebnis einer Sicherungsoperation mit der betroffenen Datei und gegebenenfalls dem gelesenen Manifest.
/// </summary>
/// <param name="Result">Das Ergebnis.</param>
/// <param name="File">Die betroffene Sicherung; <see langword="null"/>, wenn es keine gibt.</param>
/// <param name="Manifest">Das gelesene Manifest (nur bei der Prüfung); sonst <see langword="null"/>.</param>
/// <returns>Der Wert.</returns>
public sealed record BackupOutcome(BackupResult Result, BackupFileInfo? File = null, BackupManifest? Manifest = null);
