using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Erzeugt Teststationen mit stabilen Kennungen.
/// </summary>
public static class StationFactory
{
    /// <summary>
    /// Berlin-Mitte als gemeinsame Testposition (Breitengrad).
    /// </summary>
    public const double CenterLatitude = 52.52;

    /// <summary>
    /// Berlin-Mitte als gemeinsame Testposition (Längengrad).
    /// </summary>
    public const double CenterLongitude = 13.405;

    /// <summary>
    /// Erzeugt eine Station.
    /// </summary>
    /// <param name="number">Laufende Nummer (bestimmt Kennung und Lage nahe der Testposition).</param>
    /// <param name="retrievedUtc">Abrufzeitpunkt der Preise.</param>
    /// <param name="prices">Sorte und Preis.</param>
    /// <returns>Die Station.</returns>
    public static StationInfo Create(int number, DateTime retrievedUtc, params (FuelType, decimal)[] prices)
    {
        return new StationInfo
        {
            Id = $"{number:D8}-0000-4000-8000-000000000000",
            Name = $"Station {number}",
            Latitude = CenterLatitude + (number * 0.01),
            Longitude = CenterLongitude,
            DistanceKm = number * 1.11,
            Prices = prices.Select(p => new FuelPrice(p.Item1, p.Item2, retrievedUtc)).ToList(),
        };
    }

    /// <summary>
    /// Erzeugt eine Station mit frei wählbaren Angaben für die Aufbereitung der Ergebnisliste.
    /// </summary>
    /// <param name="number">Laufende Nummer (bestimmt die Kennung).</param>
    /// <param name="name">Der Name.</param>
    /// <param name="distanceKm">Die Entfernung oder <see langword="null"/>.</param>
    /// <param name="retrievedUtc">Abrufzeitpunkt der Preise.</param>
    /// <param name="wholeDay">Angabe „durchgehend geöffnet“ oder <see langword="null"/>.</param>
    /// <param name="isOpen">Angabe „geöffnet“ oder <see langword="null"/>.</param>
    /// <param name="prices">Sorte und Preis.</param>
    /// <returns>Die Station.</returns>
    public static StationInfo CreateCustom(
        int number,
        string name,
        double? distanceKm,
        DateTime retrievedUtc,
        bool? wholeDay,
        bool? isOpen,
        params (FuelType, decimal)[] prices)
    {
        return new StationInfo
        {
            Id = $"{number:D8}-0000-4000-8000-000000000000",
            Name = name,
            Latitude = CenterLatitude,
            Longitude = CenterLongitude,
            DistanceKm = distanceKm,
            WholeDay = wholeDay,
            DetailsUpdatedUtc = wholeDay is null ? null : retrievedUtc,
            IsOpen = isOpen,
            Prices = prices.Select(p => new FuelPrice(p.Item1, p.Item2, retrievedUtc)).ToList(),
        };
    }
}
