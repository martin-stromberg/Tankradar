namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Das Ergebnis einer Standortabfrage.
/// </summary>
/// <param name="Status">Der Status der Abfrage.</param>
/// <param name="Position">Die Position; nur bei <see cref="LocationStatus.Available"/> gesetzt.</param>
/// <returns>Der Wert.</returns>
public sealed record LocationResult(LocationStatus Status, GeoPosition? Position = null)
{
    /// <summary>
    /// Erstellt ein Ergebnis mit ermittelter Position.
    /// </summary>
    /// <param name="position">Die Position.</param>
    /// <returns>Das Ergebnis.</returns>
    public static LocationResult Success(GeoPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new LocationResult(LocationStatus.Available, position);
    }

    /// <summary>
    /// Erstellt ein Ergebnis ohne Position.
    /// </summary>
    /// <param name="status">Der Status; nicht <see cref="LocationStatus.Available"/>.</param>
    /// <returns>Das Ergebnis.</returns>
    public static LocationResult Failure(LocationStatus status)
    {
        return new LocationResult(status);
    }
}
