namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Einstellungen der Routensuche: Geometrie der Umkreisabfragen entlang der Route und Obergrenzen (siehe ADR 0014).
/// </summary>
public sealed class RouteSearchOptions
{
    /// <summary>
    /// Die Standardobergrenze der Routenlänge in Kilometern.
    /// </summary>
    public const int DefaultMaxRouteLengthKm = 400;

    /// <summary>
    /// Der größte Radius einer Umkreisabfrage beim Preisdienst in Kilometern.
    /// </summary>
    public double QueryRadiusKm { get; init; } = 25;

    /// <summary>
    /// Sicherheitsfaktor (größer 0, höchstens 1) gegen Rundungsfehler bei der lückenlosen Überdeckung des Abstandsbandes.
    /// </summary>
    public double CoverageFactor { get; init; } = 0.95;

    /// <summary>
    /// Die höchste Zahl der Umkreisabfragen je Suche.
    /// </summary>
    public int MaxQueries { get; init; } = 15;

    /// <summary>
    /// Die längste Route in Kilometern, die gesucht wird.
    /// </summary>
    public double MaxRouteLengthKm { get; init; } = DefaultMaxRouteLengthKm;

    /// <summary>
    /// Liegen Start und Ziel weniger weit auseinander (Kilometer), gilt die Suche als „selber Ort“.
    /// </summary>
    public double SamePlaceKm { get; init; } = 0.2;

    /// <summary>
    /// Prüft die Einstellungen.
    /// </summary>
    /// <exception cref="InvalidOperationException">Die Einstellungen sind ungültig.</exception>
    public void Validate()
    {
        if (!double.IsFinite(QueryRadiusKm) || QueryRadiusKm <= 0 || QueryRadiusKm > 25)
        {
            throw new InvalidOperationException("Der Abfrageradius muss zwischen 0 und 25 km liegen.");
        }

        if (!double.IsFinite(CoverageFactor) || CoverageFactor <= 0 || CoverageFactor > 1)
        {
            throw new InvalidOperationException("Der Überdeckungsfaktor muss größer 0 und höchstens 1 sein.");
        }

        if (MaxQueries < 1 || !double.IsFinite(MaxRouteLengthKm) || MaxRouteLengthKm <= 0 || !double.IsFinite(SamePlaceKm) || SamePlaceKm < 0)
        {
            throw new InvalidOperationException("Die Obergrenzen der Routensuche sind ungültig.");
        }
    }
}
