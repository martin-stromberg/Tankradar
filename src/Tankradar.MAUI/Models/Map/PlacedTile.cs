namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Eine sichtbare Kachel mit der Lage ihrer oberen linken Ecke im Kartenausschnitt.
/// </summary>
/// <param name="Key">Die Kachel (Spalte bereits in den Wertebereich umgebrochen).</param>
/// <param name="Left">Der Abstand der linken Kante vom linken Rand des Ausschnitts in Pixeln.</param>
/// <param name="Top">Der Abstand der oberen Kante vom oberen Rand des Ausschnitts in Pixeln.</param>
/// <returns>Der Wert.</returns>
public readonly record struct PlacedTile(TileKey Key, double Left, double Top);
