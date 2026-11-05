using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Umkreissuche zum sichtbaren Kartenausschnitt (<see cref="MapAreaQuery"/>): Mittelpunkt, Radius aus der halben Diagonale in ganzen Kilometern (aufgerundet),
/// Mindestradius 1 km, Grenze 25 km und Übernahme der Spritsorten.
/// </summary>
public class MapAreaQueryTests_Radius : BaseTest
{
    private static readonly IReadOnlyList<FuelType> SuperE5 = [FuelType.SuperE5];

    /// <summary>
    /// Prüft Mittelpunkt und Radius: Ein Ausschnitt von 0,08° Nord-Süd-Ausdehnung (rund 8,9 km Diagonale) ergibt die Mitte und aufgerundet 5 km,
    /// ein Ausschnitt von 0,1° × 0,2° (rund 17,5 km Diagonale) aufgerundet 9 km.
    /// </summary>
    /// <param name="minLatitude">Südlicher Rand.</param>
    /// <param name="minLongitude">Westlicher Rand.</param>
    /// <param name="maxLatitude">Nördlicher Rand.</param>
    /// <param name="maxLongitude">Östlicher Rand.</param>
    /// <param name="centerLatitude">Erwartete Breite der Mitte.</param>
    /// <param name="centerLongitude">Erwartete Länge der Mitte.</param>
    /// <param name="radiusKm">Erwarteter Radius.</param>
    [Theory]
    [InlineData(52.00, 13.00, 52.08, 13.00, 52.04, 13.00, 5)]
    [InlineData(52.00, 13.00, 52.10, 13.20, 52.05, 13.10, 9)]
    public void From_UsesCenterAndHalfDiagonal(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude, double centerLatitude, double centerLongitude, int radiusKm)
    {
        var query = MapAreaQuery.From(new MapBounds(minLatitude, minLongitude, maxLatitude, maxLongitude), SuperE5);

        Assert.NotNull(query);
        Assert.Equal(centerLatitude, query!.Latitude, 6);
        Assert.Equal(centerLongitude, query.Longitude, 6);
        Assert.Equal(radiusKm, query.RadiusKm);
    }

    /// <summary>
    /// Prüft die Größenprüfung: knapp 24,5 km halbe Diagonale sind erlaubt, gut 25,5 km zu groß.
    /// </summary>
    /// <param name="maxLatitude">Nördlicher Rand bei festem Süd- und Westrand.</param>
    /// <param name="expected">Erwartetes Ergebnis.</param>
    [Theory]
    [InlineData(52.44, false)]
    [InlineData(52.46, true)]
    public void IsTooLarge_MatchesMaximumRadius(double maxLatitude, bool expected)
    {
        Assert.Equal(expected, MapAreaQuery.IsTooLarge(new MapBounds(52.00, 13.00, maxLatitude, 13.00)));
    }

    /// <summary>
    /// Prüft die Obergrenze der Preisquelle: knapp 24,5 km halbe Diagonale ergeben noch eine Abfrage mit 25 km.
    /// </summary>
    [Fact]
    public void From_AllowsMaximumRadius()
    {
        Assert.Equal(25, MapAreaQuery.From(new MapBounds(52.00, 13.00, 52.44, 13.00), SuperE5)!.RadiusKm);
    }

    /// <summary>
    /// Prüft, dass Ausschnitte mit mehr als 25 km halber Diagonale keine Abfrage ergeben.
    /// </summary>
    /// <param name="minLatitude">Südlicher Rand.</param>
    /// <param name="minLongitude">Westlicher Rand.</param>
    /// <param name="maxLatitude">Nördlicher Rand.</param>
    /// <param name="maxLongitude">Östlicher Rand.</param>
    [Theory]
    [InlineData(52.00, 13.00, 52.46, 13.00)]
    [InlineData(47.00, 5.00, 55.00, 15.00)]
    public void From_ReturnsNull_WhenRadiusExceeds25Km(double minLatitude, double minLongitude, double maxLatitude, double maxLongitude)
    {
        Assert.Null(MapAreaQuery.From(new MapBounds(minLatitude, minLongitude, maxLatitude, maxLongitude), SuperE5));
    }

    /// <summary>
    /// Prüft, dass sehr kleine Ausschnitte (auch ohne Ausdehnung) den Mindestradius von 1 km verwenden.
    /// </summary>
    /// <param name="maxLatitude">Nördlicher Rand bei Südrand 52,52.</param>
    /// <param name="maxLongitude">Östlicher Rand bei Westrand 13,405.</param>
    [Theory]
    [InlineData(52.52, 13.405)]
    [InlineData(52.521, 13.406)]
    public void From_UsesMinimumOneKilometer(double maxLatitude, double maxLongitude)
    {
        Assert.Equal(1, MapAreaQuery.From(new MapBounds(52.52, 13.405, maxLatitude, maxLongitude), SuperE5)!.RadiusKm);
    }

    /// <summary>
    /// Prüft, dass die übergebenen Spritsorten unverändert in die Abfrage übernommen werden.
    /// </summary>
    [Fact]
    public void From_PassesFuelTypes()
    {
        IReadOnlyList<FuelType> fuelTypes = [FuelType.Diesel, FuelType.SuperE10];

        var query = MapAreaQuery.From(new MapBounds(52.00, 13.00, 52.08, 13.00), fuelTypes);

        Assert.Equal([FuelType.Diesel, FuelType.SuperE10], query!.FuelTypes);
    }
}
