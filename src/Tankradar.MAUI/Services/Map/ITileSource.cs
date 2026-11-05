using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Liefert die Bilddaten von Kartenkacheln (zwischengespeichert; ein Fehler führt zu <see langword="null"/>, nie zu einer Ausnahme, damit die Karte ohne Kacheln benutzbar bleibt).
/// </summary>
public interface ITileSource
{
    /// <summary>
    /// Liefert die Bilddaten (PNG) einer Kachel.
    /// </summary>
    /// <param name="key">Die Kachel.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Bilddaten; <see langword="null"/>, wenn die Kachel nicht verfügbar ist.</returns>
    Task<byte[]?> GetTileAsync(TileKey key, CancellationToken cancellationToken = default);
}
