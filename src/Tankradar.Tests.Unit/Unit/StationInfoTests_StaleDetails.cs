using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass Detailangaben einer Tankstelle nach Ablauf der Altersgrenze ausgeblendet werden, Preise und Stammdaten aber erhalten bleiben.
/// </summary>
public class StationInfoTests_StaleDetails : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo WithDetails(DateTime? updated)
    {
        return new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            Street = "Hauptstraße",
            IsOpen = true,
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            DetailsUpdatedUtc = updated,
            Prices = [new FuelPrice(FuelType.Diesel, 1.6m, Now)],
        };
    }

    /// <summary>
    /// Prüft, dass verwendbare Detailangaben unverändert (dieselbe Instanz) bleiben.
    /// </summary>
    [Fact]
    public void WithoutStaleDetails_Fresh_ReturnsSameInstance()
    {
        var station = WithDetails(Now.AddHours(-23));

        Assert.Same(station, station.WithoutStaleDetails(Now));
    }

    /// <summary>
    /// Prüft, dass zu alte Detailangaben entfernt werden und Stammdaten, Status und Preise erhalten bleiben.
    /// </summary>
    [Fact]
    public void WithoutStaleDetails_TooOld_RemovesDetailsOnly()
    {
        var result = WithDetails(Now.AddHours(-25)).WithoutStaleDetails(Now);

        Assert.Null(result.WholeDay);
        Assert.Empty(result.OpeningTimes);
        Assert.Null(result.DetailsUpdatedUtc);
        Assert.Equal("Alpha", result.Name);
        Assert.Equal("Hauptstraße", result.Street);
        Assert.True(result.IsOpen);
        Assert.Single(result.Prices);
    }

    /// <summary>
    /// Prüft, dass Detailangaben ohne bekannten Zeitpunkt als zu alt gelten.
    /// </summary>
    [Fact]
    public void WithoutStaleDetails_UnknownAge_RemovesDetails()
    {
        var result = WithDetails(null).WithoutStaleDetails(Now);

        Assert.Null(result.WholeDay);
        Assert.Empty(result.OpeningTimes);
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle ohne jede Detailangabe unverändert bleibt.
    /// </summary>
    [Fact]
    public void WithoutStaleDetails_NoDetails_ReturnsSameInstance()
    {
        var station = StationFactory.Create(1, Now, (FuelType.Diesel, 1.6m));

        Assert.Same(station, station.WithoutStaleDetails(Now));
    }
}
