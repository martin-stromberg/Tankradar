using Tankradar.MAUI.Models.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Durchschnittsverbrauch des <see cref="MAUI.Services.Tankbook.TankbookStatisticsCalculator"/>: Gewichtung, Ausschluss von Intervallen ohne Strecke und Rundung.
/// </summary>
public class TankbookStatisticsCalculatorTests_Average : BaseTest
{
    /// <summary>
    /// Prüft den Durchschnitt aus einem Intervall.
    /// </summary>
    [Fact]
    public void Average_TwoFullEntries_IsWeightedConsumption()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_500, 30m)]).Kinds.Single();

        Assert.Equal(6.00m, kind.AverageConsumption);
        Assert.Equal(TankbookAverageSource.Calculated, kind.AverageSource);
        Assert.Equal(1, kind.CalculatedIntervals);
        Assert.Equal(500, kind.TotalDistanceKm);
    }

    /// <summary>
    /// Prüft, dass nach Strecke gewichtet wird und nicht das Mittel der Einzelwerte gilt (10,00 und 6,00 ergeben 6,67 statt 8,00).
    /// </summary>
    [Fact]
    public void Average_UnequalDistances_IsWeightedNotMeanOfValues()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_100, 10m), Create(3, 10_600, 30m)]).Kinds.Single();

        Assert.Equal(6.67m, kind.AverageConsumption);
        Assert.Equal(2, kind.CalculatedIntervals);
        Assert.Equal(600, kind.TotalDistanceKm);
    }

    /// <summary>
    /// Prüft, dass ein Intervall ohne Strecke nicht einfließt.
    /// </summary>
    [Fact]
    public void Average_NoDistanceInterval_IsExcluded()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_000, 30m), Create(3, 10_500, 35m)]).Kinds.Single();

        Assert.Equal(7.00m, kind.AverageConsumption);
        Assert.Equal(1, kind.CalculatedIntervals);
    }

    /// <summary>
    /// Prüft, dass auf zwei Nachkommastellen von Null weg gerundet wird (12,345 ergibt 12,35).
    /// </summary>
    [Fact]
    public void Average_RoundsAwayFromZeroToTwoDecimals()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_100, 12.345m)]).Kinds.Single();

        Assert.Equal(12.35m, kind.AverageConsumption);
    }
}
