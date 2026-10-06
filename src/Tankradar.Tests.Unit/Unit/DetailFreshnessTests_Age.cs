using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Altersgrenze der Detailangaben (Öffnungszeiten, abgeleitete Hinweise) und die Formatierung ihres Alters.
/// </summary>
public class DetailFreshnessTests_Age : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    /// <summary>
    /// Prüft, dass Detailangaben bis einschließlich 24 Stunden verwendbar sind und danach nicht mehr.
    /// </summary>
    [Fact]
    public void IsUsable_UpToMaxAge_IsTrueAndBeyondIsFalse()
    {
        Assert.True(DetailFreshness.IsUsable(Now, Now));
        Assert.True(DetailFreshness.IsUsable(Now - DetailFreshness.MaxAge, Now));
        Assert.False(DetailFreshness.IsUsable(Now - DetailFreshness.MaxAge - TimeSpan.FromSeconds(1), Now));
    }

    /// <summary>
    /// Prüft, dass ein unbekannter Zeitpunkt als nicht verwendbar gilt und ein Zeitpunkt in der Zukunft (Uhrenabweichung) als frisch.
    /// </summary>
    [Fact]
    public void IsUsable_UnknownIsFalse_FutureIsTrue()
    {
        Assert.False(DetailFreshness.IsUsable(null, Now));
        Assert.True(DetailFreshness.IsUsable(Now.AddMinutes(5), Now));
    }

    /// <summary>
    /// Prüft die Altersangabe in Minuten unter einer Stunde und in vollen Stunden darüber.
    /// </summary>
    /// <param name="minutes">Das Alter in Minuten.</param>
    /// <param name="expected">Der erwartete Text.</param>
    [Theory]
    [InlineData(0, "vor 0 Min.")]
    [InlineData(59, "vor 59 Min.")]
    [InlineData(60, "vor 1 Std.")]
    [InlineData(185, "vor 3 Std.")]
    [InlineData(1440, "vor 24 Std.")]
    public void FormatAge_UsesMinutesThenHours(int minutes, string expected)
    {
        Assert.Equal(expected, DetailFreshness.FormatAge(Now.AddMinutes(-minutes), Now));
    }
}
