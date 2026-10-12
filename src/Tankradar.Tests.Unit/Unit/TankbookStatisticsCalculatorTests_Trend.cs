using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Verlaufspunkte des <see cref="MAUI.Services.Tankbook.TankbookStatisticsCalculator"/>.
/// </summary>
public class TankbookStatisticsCalculatorTests_Trend : BaseTest
{
    /// <summary>
    /// Prüft, dass je berechnetem Vorgang ein Punkt entsteht, ältester zuerst.
    /// </summary>
    [Fact]
    public void Trend_OnePointPerCalculatedEntry_OldestFirst()
    {
        var kind = Statistics([Create(1, 10_000, 40m), Create(2, 10_500, 30m), Create(3, 11_000, 25m)]).Kinds.Single();

        Assert.Equal([6.00m, 5.00m], kind.ConsumptionTrend.Select(point => point.Value));
        Assert.Equal([Start.AddDays(2), Start.AddDays(3)], kind.ConsumptionTrend.Select(point => point.OccurredAt));
    }

    /// <summary>
    /// Prüft, dass ohne berechnete Vorgänge kein Verlauf entsteht.
    /// </summary>
    [Fact]
    public void Trend_NoCalculated_IsEmpty()
    {
        var kind = Statistics([Create(1, 10_000, 40m)]).Kinds.Single();

        Assert.Empty(kind.ConsumptionTrend);
    }
}
