using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Inhalt der Datei <c>manifest.json</c> einer Sicherung: Format, Versionen, Zeitpunkt, Dateiliste mit Prüfsummen und Zählwerte.
/// </summary>
public sealed class BackupManifest
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Erstellt ein leeres Manifest mit der aktuellen Formatkennung.
    /// </summary>
    public BackupManifest()
    {
        Counts = new BackupManifestCounts();
    }

    /// <summary>
    /// Die Formatkennung; muss <see cref="BackupFormat.FormatName"/> sein.
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = BackupFormat.FormatName;

    /// <summary>
    /// Die Version des Sicherungsformats.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = BackupFormat.SchemaVersion;

    /// <summary>
    /// Die Version der App, die die Sicherung erstellt hat.
    /// </summary>
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// Die letzte angewendete Datenbankmigration der gesicherten Datenbank.
    /// </summary>
    [JsonPropertyName("databaseMigration")]
    public string DatabaseMigration { get; set; } = string.Empty;

    /// <summary>
    /// Der Erstellungszeitpunkt (UTC).
    /// </summary>
    [JsonPropertyName("createdUtc")]
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Die Dateien des Archivs (ohne das Manifest selbst).
    /// </summary>
    [JsonPropertyName("files")]
    public List<BackupManifestFile> Files { get; set; } = [];

    /// <summary>
    /// Die Zählwerte des Inhalts.
    /// </summary>
    [JsonPropertyName("counts")]
    public BackupManifestCounts Counts { get; set; }

    /// <summary>
    /// Serialisiert das Manifest als JSON.
    /// </summary>
    /// <returns>Die UTF-8-Bytes des JSON-Dokuments.</returns>
    public byte[] ToJson()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);
    }

    /// <summary>
    /// Liest ein Manifest aus einem JSON-Datenstrom.
    /// </summary>
    /// <param name="json">Der Datenstrom.</param>
    /// <returns>Das Manifest oder <see langword="null"/>, wenn der Inhalt kein gültiges Manifest ist.</returns>
    public static BackupManifest? FromJson(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            var manifest = JsonSerializer.Deserialize<BackupManifest>(json, JsonOptions);
            return manifest is { Files: not null, Counts: not null } ? manifest : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
