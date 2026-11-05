using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Altersangabe und Amber-Kennzeichnung (ab 60 Minuten) der Preiszeilen.
/// </summary>
public class StationResultBuilderTests_Age : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationPriceLine LineFor(DateTime retrievedUtc)
    {
        var station = StationFactory.CreateCustom(1, "Eins", 1.0, retrievedUtc, null, null, (FuelType.SuperE5, 1.80m));
        return StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single().PriceLines.Single();
    }

    /// <summary>
    /// Prüft Alterstext und Veraltet-Kennzeichen an der Grenze von 60 Minuten.
    /// </summary>
    /// <param name="minutes">Alter in Minuten.</param>
    /// <param name="expectedText">Erwarteter Alterstext.</param>
    /// <param name="expectedStale">Erwartetes Veraltet-Kennzeichen.</param>
    [Theory]
    [InlineData(0, "vor 0 Min.", false)]
    [InlineData(59, "vor 59 Min.", false)]
    [InlineData(60, "vor 60 Min.", true)]
    [InlineData(61, "vor 61 Min.", true)]
    public void Build_AgeAndStaleFlag_FollowSixtyMinuteBoundary(int minutes, string expectedText, bool expectedStale)
    {
        var line = LineFor(Now.AddMinutes(-minutes));

        Assert.Equal(expectedText, line.AgeText);
        Assert.Equal(expectedStale, line.IsStale);
    }

    /// <summary>
    /// Prüft, dass ein Zeitstempel in der Zukunft (Uhrenabweichung) als Alter null gilt.
    /// </summary>
    [Fact]
    public void Build_FutureTimestamp_IsAgeZero()
    {
        var line = LineFor(Now.AddMinutes(5));

        Assert.Equal("vor 0 Min.", line.AgeText);
        Assert.False(line.IsStale);
    }
}
