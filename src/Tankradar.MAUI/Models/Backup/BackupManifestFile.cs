using System.Text.Json.Serialization;

namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Ein Eintrag der Dateiliste des Manifests mit Pfad, Größe und Prüfsumme.
/// </summary>
public sealed class BackupManifestFile
{
    /// <summary>
    /// Der Pfad im Archiv.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Die entpackte Größe in Bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Die SHA-256-Prüfsumme in Kleinbuchstaben (hexadezimal).
    /// </summary>
    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;
}
