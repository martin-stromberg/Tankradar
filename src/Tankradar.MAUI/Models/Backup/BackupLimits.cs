namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Grenzen für das Lesen einer Sicherung (Schutz vor Archiven, die beim Entpacken unverhältnismäßig viel Platz belegen).
/// </summary>
public static class BackupLimits
{
    /// <summary>
    /// Puffergröße beim Kopieren und Archivieren von Dateien in Bytes.
    /// </summary>
    public const int BufferSize = 81_920;

    /// <summary>
    /// Höchstzahl der Einträge eines Archivs.
    /// </summary>
    public const int MaxEntries = 20_000;

    /// <summary>
    /// Höchstsumme der entpackten Größen aller Einträge in Bytes (2 GiB).
    /// </summary>
    public const long MaxTotalBytes = 2L * 1024 * 1024 * 1024;

    /// <summary>
    /// Höchstgröße der entpackten Datenbank in Bytes (1 GiB).
    /// </summary>
    public const long MaxDatabaseBytes = 1L * 1024 * 1024 * 1024;

    /// <summary>
    /// Höchstgröße des entpackten Manifests in Bytes (1 MiB).
    /// </summary>
    public const long MaxManifestBytes = 1L * 1024 * 1024;

    /// <summary>
    /// Höchstdauer der Prüfung und Aufbereitung der Datenbank einer Sicherung in Sekunden (zwei Minuten); danach bricht der Vorgang ab, ohne den Bestand zu verändern.
    /// </summary>
    public const int PrepareTimeoutSeconds = 120;
}
