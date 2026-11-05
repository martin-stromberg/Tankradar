namespace Tankradar.MAUI.Models.Map;

/// <summary>
/// Art der markierten Suchposition.
/// </summary>
public enum MapOriginKind
{
    /// <summary>
    /// Der eigene Standort (Standortsuche).
    /// </summary>
    CurrentLocation,

    /// <summary>
    /// Die gesuchte Position (Adresssuche).
    /// </summary>
    SearchedPlace,
}

/// <summary>
/// Die auf der Karte markierte Suchposition. Sie wird nur im Arbeitsspeicher gehalten (nie gespeichert oder protokolliert) und bei jeder neuen Suche ersetzt.
/// </summary>
public sealed record MapOrigin
{
    /// <summary>
    /// Erstellt die Suchposition.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <param name="latitude">Der Breitengrad.</param>
    /// <param name="longitude">Der Längengrad.</param>
    public MapOrigin(MapOriginKind kind, double latitude, double longitude)
    {
        Kind = kind;
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Die Art der Position.
    /// </summary>
    public MapOriginKind Kind { get; }

    /// <summary>
    /// Der Breitengrad.
    /// </summary>
    public double Latitude { get; }

    /// <summary>
    /// Der Längengrad.
    /// </summary>
    public double Longitude { get; }

    /// <summary>
    /// Gibt eine Darstellung ohne Koordinaten zurück, damit die Position nicht versehentlich protokolliert wird.
    /// </summary>
    /// <returns>Ein fester Text.</returns>
    public override string ToString()
    {
        return nameof(MapOrigin);
    }
}
