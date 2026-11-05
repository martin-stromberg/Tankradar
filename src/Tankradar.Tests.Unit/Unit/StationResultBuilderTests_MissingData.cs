using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass fehlende Quelldaten ausgeblendet und nie durch Platzhalter ersetzt werden.
/// </summary>
public class StationResultBuilderTests_MissingData : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    /// <summary>
    /// Prüft, dass fehlende Entfernung und fehlender Öffnungsstatus leere Texte ergeben statt Platzhaltern.
    /// </summary>
    [Fact]
    public void Build_MissingDistanceAndOpeningStatus_AreEmptyNotPlaceholders()
    {
        var station = StationFactory.CreateCustom(1, "Eins", null, Now, null, null, (FuelType.SuperE5, 1.8m));

        var item = StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();

        Assert.Null(item.DistanceKm);
        Assert.Equal(string.Empty, item.DistanceText);
        Assert.False(item.HasDistance);
        Assert.Equal(string.Empty, item.OpeningStatusText);
        Assert.False(item.HasOpeningStatus);
    }

    /// <summary>
    /// Prüft, dass ein bekannter Öffnungsstatus als Text erscheint.
    /// </summary>
    [Fact]
    public void Build_KnownOpeningStatus_IsShown()
    {
        var open = StationFactory.CreateCustom(1, "Auf", 1.0, Now, null, true, (FuelType.SuperE5, 1.8m));
        var closed = StationFactory.CreateCustom(2, "Zu", 2.0, Now, null, false, (FuelType.SuperE5, 1.8m));

        var items = StationResultBuilder.Build([open, closed], SearchTestData.AllFuels, null, ResultSortOrder.Distance, Now);

        Assert.Equal(["Geöffnet", "Geschlossen"], items.Select(item => item.OpeningStatusText));
    }

    /// <summary>
    /// Prüft, dass eine Sorte ohne Preis keine Preiszeile erhält und nichts ausgegeben wird, was wie ein Platzhalter aussieht.
    /// </summary>
    [Fact]
    public void Build_FuelWithoutPrice_HasNoLine()
    {
        var station = StationFactory.CreateCustom(1, "Eins", 1.0, Now, null, null, (FuelType.Diesel, 1.5m));

        var item = StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();

        var line = Assert.Single(item.PriceLines);
        Assert.Equal(FuelType.Diesel, line.FuelType);
        Assert.Equal("1,500 €", line.PriceText);
    }

    /// <summary>
    /// Prüft, dass eine Station ohne Preise und ohne Hinweise ohne Ausnahme aufbereitet wird.
    /// </summary>
    [Fact]
    public void Build_StationWithoutPrices_ProducesItemWithoutLinesOrHints()
    {
        var station = new StationInfo { Id = "00000001-0000-4000-8000-000000000000", Name = "Leer" };

        var item = StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Name, Now).Single();

        Assert.Empty(item.PriceLines);
        Assert.False(item.HasHints);
    }
}
