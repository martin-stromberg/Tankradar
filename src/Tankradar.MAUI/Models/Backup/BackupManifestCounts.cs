using System.Text.Json.Serialization;

namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Zählwerte des Inhalts einer Sicherung (für die Rückfrage und die Prüfung der Vollständigkeit).
/// </summary>
public sealed class BackupManifestCounts
{
    /// <summary>
    /// Zahl der Fahrzeuge.
    /// </summary>
    [JsonPropertyName("vehicles")]
    public int Vehicles { get; set; }

    /// <summary>
    /// Zahl der Tank- und Ladevorgänge.
    /// </summary>
    [JsonPropertyName("entries")]
    public int Entries { get; set; }

    /// <summary>
    /// Zahl der Favoritengruppen.
    /// </summary>
    [JsonPropertyName("groups")]
    public int Groups { get; set; }

    /// <summary>
    /// Zahl der Favoriten.
    /// </summary>
    [JsonPropertyName("favorites")]
    public int Favorites { get; set; }

    /// <summary>
    /// Zahl der lokal bekannten Tankstellen.
    /// </summary>
    [JsonPropertyName("stations")]
    public int Stations { get; set; }

    /// <summary>
    /// Zahl der gespeicherten Preisstände.
    /// </summary>
    [JsonPropertyName("prices")]
    public int Prices { get; set; }

    /// <summary>
    /// Zahl der Belegfotos.
    /// </summary>
    [JsonPropertyName("photos")]
    public int Photos { get; set; }
}
