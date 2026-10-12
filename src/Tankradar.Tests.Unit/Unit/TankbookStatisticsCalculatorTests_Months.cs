using Tankradar.MAUI.Models.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookEntryFactory;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Monatskosten des <see cref="MAUI.Services.Tankbook.TankbookStatisticsCalculator"/>: Gruppierung, Monatsgrenzen, leere Monate und lokale Wanduhrzeit.
/// </summary>
public class TankbookStatisticsCalculatorTests_Months : BaseTest
{
    private static IReadOnlyList<(int Year, int Month, decimal Cost, int Count)> Months(TankbookStatistics statistics, TankbookEntryKind kind = TankbookEntryKind.Fuel)
    {
        return statistics.Kinds.Single(block => block.Kind == kind).Months
            .Select(month => (month.Year, month.Month, month.TotalCost, month.EntryCount))
            .ToList();
    }

    /// <summary>
    /// Prüft die Gruppierung nach Kalendermonat des Zeitpunkts.
    /// </summary>
    [Fact]
    public void Months_GroupsByCalendarMonthOfOccurredAt()
    {
        var statistics = Statistics(
        [
            CreateAt(1, new DateTime(2026, 9, 10, 8, 0, 0), 10_000, 10m, 10m),
            CreateAt(2, new DateTime(2026, 9, 20, 8, 0, 0), 10_300, 10m, 20m),
            CreateAt(3, new DateTime(2026, 10, 2, 8, 0, 0), 10_600, 10m, 30m),
        ]);

        Assert.Equal([(2026, 10, 30m, 1), (2026, 9, 30m, 2)], Months(statistics));
    }

    /// <summary>
    /// Prüft die Jahresgrenze: 31.12. 23:59 gehört zum Dezember, 01.01. 00:00 zum Januar.
    /// </summary>
    [Fact]
    public void Months_Boundary_Dec31_2359_And_Jan01_0000()
    {
        var statistics = Statistics(
            [CreateAt(1, new DateTime(2025, 12, 31, 23, 59, 0), 10_000, 10m, 10m), CreateAt(2, new DateTime(2026, 1, 1, 0, 0, 0), 10_300, 10m, 20m)],
            today: new DateTime(2026, 1, 15, 12, 0, 0));

        Assert.Equal([(2026, 1, 20m, 1), (2025, 12, 10m, 1)], Months(statistics));
    }

    /// <summary>
    /// Prüft die Grenze im Schaltjahr: 29.02. 23:59 gehört zum Februar, 01.03. 00:00 zum März.
    /// </summary>
    [Fact]
    public void Months_Boundary_EndOfFebruaryLeapYear()
    {
        var statistics = Statistics(
            [CreateAt(1, new DateTime(2028, 2, 29, 23, 59, 0), 10_000, 10m, 10m), CreateAt(2, new DateTime(2028, 3, 1, 0, 0, 0), 10_300, 10m, 20m)],
            today: new DateTime(2028, 3, 10, 12, 0, 0));

        Assert.Equal([(2028, 3, 20m, 1), (2028, 2, 10m, 1)], Months(statistics));
    }

    /// <summary>
    /// Prüft, dass Monate ohne Vorgang mit 0,00 € erscheinen.
    /// </summary>
    [Fact]
    public void Months_EmptyMonthsBetween_AreZero()
    {
        var statistics = Statistics(
            [CreateAt(1, new DateTime(2026, 7, 15, 8, 0, 0), 10_000, 10m, 10m), CreateAt(2, new DateTime(2026, 9, 15, 8, 0, 0), 10_300, 10m, 20m)],
            today: new DateTime(2026, 9, 20, 12, 0, 0));

        Assert.Equal([(2026, 9, 20m, 1), (2026, 8, 0m, 0), (2026, 7, 10m, 1)], Months(statistics));
    }

    /// <summary>
    /// Prüft, dass die Reihe bis zum aktuellen Monat läuft.
    /// </summary>
    [Fact]
    public void Months_RunsUntilCurrentMonth()
    {
        var statistics = Statistics([CreateAt(1, new DateTime(2026, 8, 1, 8, 0, 0), 10_000, 10m, 10m)]);

        Assert.Equal([(2026, 10, 0m, 0), (2026, 9, 0m, 0), (2026, 8, 10m, 1)], Months(statistics));
    }

    /// <summary>
    /// Prüft, dass Teilbetankungen, auch vor dem ersten vollen Vorgang, in den Monatskosten stehen.
    /// </summary>
    [Fact]
    public void Months_IncludePartialAndPartialBeforeFirstFull()
    {
        var statistics = Statistics(
        [
            CreateAt(1, new DateTime(2026, 9, 1, 8, 0, 0), 9_800, 10m, 15m, isPartial: true),
            CreateAt(2, new DateTime(2026, 9, 5, 8, 0, 0), 10_000, 10m, 20m),
            CreateAt(3, new DateTime(2026, 9, 6, 8, 0, 0), 10_100, 10m, 5m, isPartial: true),
        ]);

        Assert.Equal((2026, 9, 40m, 3), Months(statistics)[1]);
    }

    /// <summary>
    /// Prüft, dass die lokale Wanduhrzeit ohne Zeitzonenumrechnung gilt (31.10. 23:30 bleibt im Oktober).
    /// </summary>
    [Fact]
    public void Months_UnspecifiedKind_NoTimeZoneShift()
    {
        var late = new DateTime(2026, 10, 31, 23, 30, 0, DateTimeKind.Unspecified);

        var statistics = Statistics([CreateAt(1, late, 10_000, 10m, 10m)], today: new DateTime(2026, 11, 2, 12, 0, 0));

        Assert.Equal([(2026, 11, 0m, 0), (2026, 10, 10m, 1)], Months(statistics));
    }

    /// <summary>
    /// Prüft, dass der neueste Monat zuerst steht.
    /// </summary>
    [Fact]
    public void Months_NewestFirst()
    {
        var statistics = Statistics(
            [CreateAt(1, new DateTime(2026, 8, 3, 8, 0, 0), 10_000, 10m, 10m), CreateAt(2, new DateTime(2026, 10, 3, 8, 0, 0), 10_300, 10m, 10m)]);

        var months = statistics.Kinds.Single().Months;

        Assert.Equal([10, 9, 8], months.Select(month => month.Month));
    }

    /// <summary>
    /// Prüft, dass Kraftstoff und Strom getrennte Monatsreihen haben.
    /// </summary>
    [Fact]
    public void Months_Hybrid_SeparatePerKind()
    {
        var statistics = Statistics(
        [
            CreateAt(1, new DateTime(2026, 9, 3, 8, 0, 0), 10_000, 10m, 10m),
            CreateAt(2, new DateTime(2026, 10, 3, 8, 0, 0), 10_100, 10m, 7m, kind: TankbookEntryKind.Electricity),
        ],
        VehicleFuelKind.Hybrid);

        Assert.Equal([(2026, 10, 0m, 0), (2026, 9, 10m, 1)], Months(statistics, TankbookEntryKind.Fuel));
        Assert.Equal([(2026, 10, 7m, 1)], Months(statistics, TankbookEntryKind.Electricity));
    }
}
