using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass fehlende Quelldaten in der Detailansicht ausgeblendet statt durch Platzhalter ersetzt werden, und die Öffnungszeiten samt Altersgrenze.
/// </summary>
public class StationDetailBuilderTests_MissingData : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static StationInfo Bare()
    {
        return new StationInfo { Id = "00000001-0000-4000-8000-000000000000", Name = "Nur Name" };
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle nur mit Namen keine leeren Felder liefert.
    /// </summary>
    [Fact]
    public void Build_OnlyName_HidesEverythingElse()
    {
        var item = StationDetailBuilder.Build(Bare(), SearchTestData.AllFuels, null, Now);

        Assert.Equal("Nur Name", item.Name);
        Assert.False(item.HasBrand);
        Assert.False(item.HasAddress);
        Assert.False(item.HasDistance);
        Assert.False(item.HasOpeningStatus);
        Assert.False(item.HasPrices);
        Assert.False(item.HasUnconfirmedPrice);
        Assert.False(item.HasOpeningHours);
        Assert.Empty(item.OpeningHoursAgeText);
    }

    /// <summary>
    /// Prüft, dass eine Marke, die dem Namen entspricht, nicht doppelt erscheint und der Unterschied der Schreibweise unerheblich ist.
    /// </summary>
    [Fact]
    public void Build_BrandEqualToName_IsHidden()
    {
        var station = new StationInfo { Id = "00000001-0000-4000-8000-000000000000", Name = "Aral", Brand = "ARAL" };

        Assert.False(StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now).HasBrand);
    }

    /// <summary>
    /// Prüft, dass für abgewählte Sorten keine Preise erscheinen, auch wenn die Quelle sie liefert.
    /// </summary>
    [Fact]
    public void Build_OnlyDisabledFuelsAvailable_HasNoPrices()
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            Prices = [new FuelPrice(FuelType.Diesel, 1.6m, Now)],
        };

        var item = StationDetailBuilder.Build(station, SearchTestData.Only(FuelType.SuperE5), null, Now);

        Assert.False(item.HasPrices);
    }

    /// <summary>
    /// Prüft die Darstellung der Öffnungszeiten (HH:mm, „24:00“ bleibt erhalten) mit Altersangabe.
    /// </summary>
    [Fact]
    public void Build_OpeningTimes_AreFormattedWithAge()
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            OpeningTimes = [new OpeningTimeEntry("Mo-Fr", "06:00:00", "22:00:00"), new OpeningTimeEntry("Sa", "07:00:00", "24:00:00")],
            DetailsUpdatedUtc = Now.AddHours(-3),
        };

        var item = StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now);

        Assert.Equal(["Mo-Fr: 06:00 – 22:00 Uhr", "Sa: 07:00 – 24:00 Uhr"], item.OpeningHours.Select(line => line.DisplayText));
        Assert.Equal("Stand: vor 3 Std.", item.OpeningHoursAgeText);
    }

    /// <summary>
    /// Prüft, dass Abschnitte ohne Bezeichnung oder ohne Zeiten nur die vorhandene Angabe zeigen und leere Abschnitte entfallen.
    /// </summary>
    [Fact]
    public void Build_PartialOpeningEntries_ShowOnlyWhatExists()
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            OpeningTimes = [new OpeningTimeEntry(string.Empty, "06:00:00", "22:00:00"), new OpeningTimeEntry("So", string.Empty, string.Empty), new OpeningTimeEntry(string.Empty, string.Empty, string.Empty)],
            DetailsUpdatedUtc = Now,
        };

        var item = StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now);

        Assert.Equal(["06:00 – 22:00 Uhr", "So"], item.OpeningHours.Select(line => line.DisplayText));
    }

    /// <summary>
    /// Prüft, dass Öffnungszeiten und der Hinweis „Automatentankstelle“ aus Detailangaben, die älter als 24 Stunden sind, ausgeblendet werden.
    /// </summary>
    [Fact]
    public void Build_OldDetails_HideOpeningHoursAndAutomatedHint()
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            DetailsUpdatedUtc = Now.AddHours(-25),
            Prices = [new FuelPrice(FuelType.Diesel, 1.6m, Now)],
        };

        var item = StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now);

        Assert.False(item.HasOpeningHours);
        Assert.False(item.IsAutomatedStation);
        Assert.Empty(item.OpeningHoursAgeText);
        Assert.True(item.HasPrices);
    }

    /// <summary>
    /// Prüft, dass Öffnungszeiten ohne bekannten Abrufzeitpunkt nicht angezeigt werden.
    /// </summary>
    [Fact]
    public void Build_OpeningTimesWithoutTimestamp_AreHidden()
    {
        var station = new StationInfo
        {
            Id = "00000001-0000-4000-8000-000000000000",
            Name = "Alpha",
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
        };

        Assert.False(StationDetailBuilder.Build(station, SearchTestData.AllFuels, null, Now).HasOpeningHours);
    }
}
