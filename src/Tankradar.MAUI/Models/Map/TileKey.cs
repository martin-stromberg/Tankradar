namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Kennung einer Kartenkachel im üblichen Schema (Zoomstufe, Spalte, Zeile; Ursprung oben links).
/// </summary>
/// <param name="Zoom">Die Zoomstufe.</param>
/// <param name="X">Die Spalte, 0 bis 2^Zoom - 1.</param>
/// <param name="Y">Die Zeile, 0 bis 2^Zoom - 1.</param>
/// <returns>Der Wert.</returns>
public readonly record struct TileKey(int Zoom, int X, int Y);
