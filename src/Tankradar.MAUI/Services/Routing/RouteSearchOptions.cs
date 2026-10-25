using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Einstellungen der Routensuche: Geometrie der Umkreisabfragen entlang der Route und Obergrenzen (siehe ADR 0014).
/// Die Grenzen der Preisabfragen (<see cref="DefaultMaxQueries"/>, <see cref="DefaultMinQueryInterval"/>) sind vorläufig, bis Tankerkönig die Zulässigkeit
/// mehrerer Abfragen je Nutzeraktion geklärt hat; sie werden nur hier angepasst.
/// </summary>
public sealed class RouteSearchOptions
{
    /// <summary>
    /// Die vorläufige Höchstzahl der Umkreisabfragen je Routensuche (ADR 0014, Abschnitt „Obergrenze, Takt und Nutzungsbedingungen von Tankerkönig“).
    /// </summary>
    public const int DefaultMaxQueries = 3;

    /// <summary>
    /// Die Standardobergrenze der Routenlänge in Kilometern.
    /// </summary>
    public const int DefaultMaxRouteLengthKm = 400;

    /// <summary>
    /// Der vorläufige Mindestabstand zwischen zwei Umkreisabfragen der Routensuche (ADR 0014). Die Bedingungen von Tankerkönig nennen eine Abfragefrequenz von einer Anfrage je Minute;
    /// die Routensuche weicht davon bewusst und vorläufig ab, weil sie ausschließlich auf Nutzeraktion läuft.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly TimeSpan DefaultMinQueryInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Die Standardreserve auf den Mindestabstand: Der Takt wird ab der Reservierung gemessen, die Abfrage verlässt die App aber erst nach der allgemeinen
    /// Drosselung des Preisdienstes (<c>RequestThrottle</c>) und dem Verbindungsaufbau; die Reserve deckt diese Verzögerung, damit der tatsächliche Abstand nicht unter den Mindestabstand fällt.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly TimeSpan DefaultQueryIntervalReserve = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Der größte Radius einer Umkreisabfrage beim Preisdienst in Kilometern.
    /// </summary>
    public double QueryRadiusKm { get; init; } = 25;

    /// <summary>
    /// Sicherheitsfaktor (größer 0, höchstens 1) gegen Rundungsfehler bei der lückenlosen Überdeckung des Abstandsbandes.
    /// </summary>
    public double CoverageFactor { get; init; } = 0.95;

    /// <summary>
    /// Die höchste Zahl der Umkreisabfragen je Suche. Routen, die mehr Abfragen bräuchten, werden vor der ersten Preisabfrage abgelehnt.
    /// </summary>
    public int MaxQueries { get; init; } = DefaultMaxQueries;

    /// <summary>
    /// Der Mindestabstand zwischen zwei Preisabfragen der Routensuche (auch über mehrere Suchen hinweg).
    /// </summary>
    public TimeSpan MinQueryInterval { get; init; } = DefaultMinQueryInterval;

    /// <summary>
    /// Die Reserve, die auf den Mindestabstand aufgeschlagen wird (nur wenn ein Mindestabstand gilt).
    /// </summary>
    public TimeSpan QueryIntervalReserve { get; init; } = DefaultQueryIntervalReserve;

    /// <summary>
    /// Der tatsächliche Abstand der Reservierungen: Mindestabstand zuzüglich Reserve; ohne Mindestabstand 0.
    /// </summary>
    public TimeSpan EffectiveQueryInterval
    {
        get { return MinQueryInterval > TimeSpan.Zero ? MinQueryInterval + QueryIntervalReserve : TimeSpan.Zero; }
    }

    /// <summary>
    /// Die längste Route in Kilometern, die gesucht wird.
    /// </summary>
    public double MaxRouteLengthKm { get; init; } = DefaultMaxRouteLengthKm;

    /// <summary>
    /// Liegen Start und Ziel weniger weit auseinander (Kilometer), gilt die Suche als „selber Ort“.
    /// </summary>
    public double SamePlaceKm { get; init; } = 0.2;

    /// <summary>
    /// Ermittelt die Einstellungen aus der Umgebung. Produktiv gelten die Standardwerte; im Testmodus (<c>TANKATLAS_TEST_DATA_PATH</c> gesetzt) ist der Mindestabstand 0 oder
    /// der Wert der Variable <see cref="TestDataPaths.RouteQueryIntervalEnvironmentVariable"/> in Millisekunden, damit Tests schnell bleiben.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert den Wert einer Umgebungsvariable oder <see langword="null"/>.</param>
    /// <returns>Die validierten Einstellungen.</returns>
    public static RouteSearchOptions FromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        var testMode = !string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        var options = new RouteSearchOptions();
        if (testMode)
        {
            var interval = int.TryParse(getEnvironmentVariable(TestDataPaths.RouteQueryIntervalEnvironmentVariable), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var milliseconds)
                ? TimeSpan.FromMilliseconds(milliseconds)
                : TimeSpan.Zero;
            options = new RouteSearchOptions { MinQueryInterval = interval };
        }

        options.Validate();
        return options;
    }

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

        if (MinQueryInterval < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Der Mindestabstand der Abfragen darf nicht negativ sein.");
        }

        if (QueryIntervalReserve < TimeSpan.Zero)
        {
            throw new InvalidOperationException("Die Reserve des Abfrageabstands darf nicht negativ sein.");
        }

        if (MaxQueries < 1 || !double.IsFinite(MaxRouteLengthKm) || MaxRouteLengthKm <= 0 || !double.IsFinite(SamePlaceKm) || SamePlaceKm < 0)
        {
            throw new InvalidOperationException("Die Obergrenzen der Routensuche sind ungültig.");
        }
    }
}
