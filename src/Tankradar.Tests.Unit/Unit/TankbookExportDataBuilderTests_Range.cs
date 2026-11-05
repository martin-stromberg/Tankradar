using Tankradar.MAUI.Models.Tankbook;
using static Tankradar.Tests.Unit.Unit.Support.TankbookExportTestData;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft den Zeitraumfilter des <see cref="MAUI.Services.Export.TankbookExportDataBuilder"/>: Grenzen, Reihenfolge und Verbrauch über die Zeitraumgrenze.
/// </summary>
public class TankbookExportDataBuilderTests_Range : BaseTest
{
    /// <summary>
    /// Prüft, dass ein Vorgang um 00:00 des Starttags und einer um 23:59 des Endtags im Zeitraum liegen, nicht aber die Minute davor und danach.
    /// </summary>
    [Fact]
    public void Build_Range_IncludesBothDaysCompletely()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 3, 9, 23, 59, 0), 10_500),
            CreateEntry(2, new DateTime(2026, 3, 10, 0, 0, 0), 11_000),
            CreateEntry(3, new DateTime(2026, 3, 12, 23, 59, 0), 11_500),
            CreateEntry(4, new DateTime(2026, 3, 13, 0, 0, 0), 12_000),
        };

        var data = Build(entries, new DateTime(2026, 3, 10), new DateTime(2026, 3, 12));

        Assert.Equal([2L, 3L], data.Entries.Select(overview => overview.Entry.Id));
        Assert.Equal(new DateTime(2026, 3, 10), data.From);
        Assert.Equal(new DateTime(2026, 3, 12), data.To);
    }

    /// <summary>
    /// Prüft, dass ohne Zeitraum alle Vorgänge aufsteigend (ältester zuerst) enthalten sind.
    /// </summary>
    [Fact]
    public void Build_WithoutRange_ContainsAllEntriesAscending()
    {
        var entries = CreateSeries(5).AsEnumerable().Reverse();

        var data = Build(entries);

        Assert.Equal([1L, 2L, 3L, 4L, 5L], data.Entries.Select(overview => overview.Entry.Id));
        Assert.Null(data.From);
        Assert.Null(data.To);
    }

    /// <summary>
    /// Prüft, dass der erste Vorgang im Zeitraum seinen Verbrauch aus dem Vorgänger davor behält.
    /// </summary>
    [Fact]
    public void Build_FirstEntryInRange_KeepsConsumptionFromPredecessorBeforeRange()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 1, 5, 8, 0, 0), 10_500, 40m, 60m),
            CreateEntry(2, new DateTime(2026, 2, 5, 8, 0, 0), 11_000, 30m, 45m),
        };

        var data = Build(entries, new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));

        var overview = Assert.Single(data.Entries);
        Assert.Equal(500, overview.DistanceKm);
        Assert.Equal(6.00m, overview.Consumption);
        Assert.Equal(6.00m, data.Statistics.Kinds.Single().AverageConsumption);
    }

    /// <summary>
    /// Prüft, dass Teilbetankungen im Zeitraum ihre Eigenschaft behalten und keinen eigenen Verbrauch haben.
    /// </summary>
    [Fact]
    public void Build_PartialEntry_HasNoOwnConsumption()
    {
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 1, 5, 8, 0, 0), 10_500, 40m, 60m),
            CreateEntry(2, new DateTime(2026, 1, 10, 8, 0, 0), 10_700, 10m, 15m, isPartial: true),
            CreateEntry(3, new DateTime(2026, 1, 20, 8, 0, 0), 11_000, 30m, 45m),
        };

        var data = Build(entries);

        Assert.True(data.Entries[1].Entry.IsPartial);
        Assert.Null(data.Entries[1].Consumption);
        Assert.Equal(TankbookConsumptionState.Calculated, data.Entries[2].ConsumptionState);
    }

    /// <summary>
    /// Prüft, dass ein Hybridfahrzeug beide Arten mit eigener Kette im Zeitraum behält.
    /// </summary>
    [Fact]
    public void Build_Hybrid_ContainsBothKinds()
    {
        var vehicle = CreateVehicle("Hybrid", VehicleFuelKind.Hybrid);
        var entries = new[]
        {
            CreateEntry(1, new DateTime(2026, 1, 5, 8, 0, 0), 10_500, 40m, 60m),
            CreateEntry(2, new DateTime(2026, 1, 6, 8, 0, 0), 10_600, 20m, 8m, kind: TankbookEntryKind.Electricity),
            CreateEntry(3, new DateTime(2026, 1, 20, 8, 0, 0), 11_000, 30m, 45m),
        };

        var data = Build(entries, vehicle: vehicle);

        Assert.Equal(2, data.Statistics.Kinds.Count);
        Assert.Contains(data.Entries, overview => overview.Entry.Kind == TankbookEntryKind.Electricity);
    }

    /// <summary>
    /// Prüft, dass ein Zeitraum ohne Vorgänge leere Daten liefert.
    /// </summary>
    [Fact]
    public void Build_RangeWithoutEntries_IsEmpty()
    {
        var data = Build(CreateSeries(3), new DateTime(2030, 1, 1), new DateTime(2030, 12, 31));

        Assert.Empty(data.Entries);
        Assert.False(data.Statistics.HasEntries);
    }

    /// <summary>
    /// Prüft, dass die Monatsreihe im Zeitraum nicht über das Zeitraumende hinausläuft.
    /// </summary>
    [Fact]
    public void Build_PastRange_LimitsMonthsToRangeEnd()
    {
        var entries = new[] { CreateEntry(1, new DateTime(2026, 1, 5, 8, 0, 0), 10_500), CreateEntry(2, new DateTime(2026, 2, 5, 8, 0, 0), 11_000) };

        var data = Build(entries, new DateTime(2026, 1, 1), new DateTime(2026, 2, 28));

        var newest = data.Statistics.Kinds.Single().Months[0];
        Assert.Equal((2026, 2), (newest.Year, newest.Month));
    }
}
