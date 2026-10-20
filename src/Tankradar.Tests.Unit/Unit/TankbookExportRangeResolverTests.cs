using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Export;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Auflösung der Zeitraumvoreinstellungen durch den <see cref="TankbookExportRangeResolver"/> für alle Werte von <see cref="TankbookExportRangeKind"/>.
/// </summary>
public class TankbookExportRangeResolverTests : BaseTest
{
    private static readonly DateTime Today = new(2026, 10, 19, 14, 30, 0);

    /// <summary>
    /// Prüft, dass „Alle Vorgänge“ keine Grenzen setzt.
    /// </summary>
    [Fact]
    public void Resolve_All_HasNoBounds()
    {
        Assert.Equal(new TankbookExportRange(null, null), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.All, Today));
    }

    /// <summary>
    /// Prüft, dass „Dieses Jahr“ am 1. Januar beginnt und heute endet, ohne Uhrzeit.
    /// </summary>
    [Fact]
    public void Resolve_ThisYear_IsJanuaryFirstToToday()
    {
        Assert.Equal(new TankbookExportRange(new DateTime(2026, 1, 1), new DateTime(2026, 10, 19)), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.ThisYear, Today));
    }

    /// <summary>
    /// Prüft, dass „Letztes Jahr“ das gesamte Vorjahr umfasst.
    /// </summary>
    [Fact]
    public void Resolve_LastYear_IsWholePreviousYear()
    {
        Assert.Equal(new TankbookExportRange(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.LastYear, Today));
    }

    /// <summary>
    /// Prüft den Jahreswechsel: Am 1. Januar umfasst „Dieses Jahr“ genau einen Tag und „Letztes Jahr“ das alte Jahr.
    /// </summary>
    [Fact]
    public void Resolve_OnNewYearsDay_SwitchesYear()
    {
        var newYear = new DateTime(2027, 1, 1, 0, 5, 0);

        Assert.Equal(new TankbookExportRange(new DateTime(2027, 1, 1), new DateTime(2027, 1, 1)), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.ThisYear, newYear));
        Assert.Equal(new TankbookExportRange(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31)), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.LastYear, newYear));
    }

    /// <summary>
    /// Prüft, dass „Letzte 12 Monate“ zwölf Monate einschließlich heute umfasst.
    /// </summary>
    [Fact]
    public void Resolve_Last12Months_IsTwelveMonthsIncludingToday()
    {
        Assert.Equal(new TankbookExportRange(new DateTime(2025, 10, 20), new DateTime(2026, 10, 19)), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.Last12Months, Today));
    }

    /// <summary>
    /// Prüft den Schalttag: Am 29.02.2028 beginnen die letzten 12 Monate am 01.03.2027.
    /// </summary>
    [Fact]
    public void Resolve_Last12Months_OnLeapDay_StartsDayAfterOneYearBefore()
    {
        var leapDay = new DateTime(2028, 2, 29, 10, 0, 0);

        Assert.Equal(new TankbookExportRange(new DateTime(2027, 3, 1), new DateTime(2028, 2, 29)), TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.Last12Months, leapDay));
    }

    /// <summary>
    /// Prüft, dass „Eigener Zeitraum“ die Datumsfelder ohne Uhrzeit übernimmt.
    /// </summary>
    [Fact]
    public void Resolve_Custom_UsesGivenDatesWithoutTime()
    {
        var result = TankbookExportRangeResolver.Resolve(TankbookExportRangeKind.Custom, Today, new DateTime(2026, 3, 5, 17, 0, 0), new DateTime(2026, 4, 6, 8, 0, 0));

        Assert.Equal(new TankbookExportRange(new DateTime(2026, 3, 5), new DateTime(2026, 4, 6)), result);
    }

    /// <summary>
    /// Prüft die Zeitraumprüfung: gültig, Start nach Ende, Ende in der Zukunft; heute ist gültig.
    /// </summary>
    [Fact]
    public void Validate_ReportsStartAfterEndAndEndInFuture()
    {
        var today = new DateTime(2026, 10, 19, 14, 30, 0);

        Assert.Equal(TankbookExportRangeError.None, TankbookExportRangeResolver.Validate(null, null, today));
        Assert.Equal(TankbookExportRangeError.None, TankbookExportRangeResolver.Validate(new DateTime(2026, 10, 19), new DateTime(2026, 10, 19), today));
        Assert.Equal(TankbookExportRangeError.StartAfterEnd, TankbookExportRangeResolver.Validate(new DateTime(2026, 10, 19), new DateTime(2026, 10, 18), today));
        Assert.Equal(TankbookExportRangeError.EndInFuture, TankbookExportRangeResolver.Validate(null, new DateTime(2026, 10, 20), today));
    }

    /// <summary>
    /// Prüft, dass ein unbekannter Wert abgelehnt wird.
    /// </summary>
    [Fact]
    public void Resolve_UnknownKind_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TankbookExportRangeResolver.Resolve((TankbookExportRangeKind)99, Today));
    }
}
