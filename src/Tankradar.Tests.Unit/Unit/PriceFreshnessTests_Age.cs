using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Aktualitätsbewertung von Preisen anhand des Zeitstempels (Grenze 60 Minuten) und die Altersanzeige.
/// </summary>
public class PriceFreshnessTests_Age : BaseTest
{
    private static readonly DateTime Now = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Prüft die Grenze: bis 59 Minuten aktuell, ab 60 Minuten veraltet.
    /// </summary>
    /// <param name="minutes">Alter in Minuten.</param>
    /// <param name="expectedStale">Erwartete Bewertung.</param>
    [Theory]
    [InlineData(0, false)]
    [InlineData(59, false)]
    [InlineData(60, true)]
    [InlineData(61, true)]
    [InlineData(600, true)]
    public void IsStale_UsesSixtyMinuteBoundary(int minutes, bool expectedStale)
    {
        Assert.Equal(expectedStale, PriceFreshness.IsStale(Now.AddMinutes(-minutes), Now));
    }

    /// <summary>
    /// Prüft, dass 59 Minuten und 59 Sekunden noch aktuell sind.
    /// </summary>
    [Fact]
    public void IsStale_OneSecondBeforeBoundary_IsFresh()
    {
        Assert.False(PriceFreshness.IsStale(Now.AddMinutes(-60).AddSeconds(1), Now));
    }

    /// <summary>
    /// Prüft, dass ein Zeitstempel in der Zukunft als Alter null gilt.
    /// </summary>
    [Fact]
    public void GetAge_FutureTimestamp_IsZero()
    {
        Assert.Equal(TimeSpan.Zero, PriceFreshness.GetAge(Now.AddMinutes(5), Now));
        Assert.False(PriceFreshness.IsStale(Now.AddMinutes(5), Now));
    }

    /// <summary>
    /// Prüft die Altersanzeige „vor X Min.“ in vollen Minuten, auch über 60 Minuten.
    /// </summary>
    /// <param name="seconds">Alter in Sekunden.</param>
    /// <param name="expected">Erwartete Anzeige.</param>
    [Theory]
    [InlineData(0, "vor 0 Min.")]
    [InlineData(59, "vor 0 Min.")]
    [InlineData(60, "vor 1 Min.")]
    [InlineData(1800, "vor 30 Min.")]
    [InlineData(4500, "vor 75 Min.")]
    public void FormatAge_ShowsFullMinutes(int seconds, string expected)
    {
        Assert.Equal(expected, PriceFreshness.FormatAge(Now.AddSeconds(-seconds), Now));
    }
}
