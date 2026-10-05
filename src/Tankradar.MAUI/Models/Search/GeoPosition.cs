namespace Tankradar.MAUI.Models.Search;

/// <summary>
/// Eine geografische Position (nur im Arbeitsspeicher; wird nie gespeichert oder protokolliert).
/// </summary>
public sealed record GeoPosition
{
    /// <summary>
    /// Erstellt die Position und prüft die Wertebereiche.
    /// </summary>
    /// <param name="latitude">Breitengrad, -90 bis 90.</param>
    /// <param name="longitude">Längengrad, -180 bis 180.</param>
    /// <exception cref="ArgumentOutOfRangeException">Ein Wert ist nicht endlich oder außerhalb des Wertebereichs.</exception>
    public GeoPosition(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Der Breitengrad muss zwischen -90 und 90 liegen.");
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Der Längengrad muss zwischen -180 und 180 liegen.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Versucht, eine Position zu erstellen.
    /// </summary>
    /// <param name="latitude">Breitengrad.</param>
    /// <param name="longitude">Längengrad.</param>
    /// <param name="position">Die Position bei Erfolg, sonst <see langword="null"/>.</param>
    /// <returns><see langword="true"/>, wenn beide Werte im gültigen Bereich liegen.</returns>
    public static bool TryCreate(double latitude, double longitude, out GeoPosition? position)
    {
        try
        {
            position = new GeoPosition(latitude, longitude);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            position = null;
            return false;
        }
    }

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
        return nameof(GeoPosition);
    }
}
