using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Sortierung der Ergebnisliste nach Preis, Entfernung und Name einschließlich Gleichstand und fehlendem Preis.
/// </summary>
public class StationResultBuilderTests_Sorting : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static IReadOnlyList<string> Names(IEnumerable<StationInfo> stations, FuelType? filter, ResultSortOrder order, IReadOnlyList<FuelTypeSelection>? fuels = null)
    {
        return StationResultBuilder.Build(stations.ToList(), fuels ?? SearchTestData.AllFuels, filter, order, Now).Select(item => item.Name).ToList();
    }

    /// <summary>
    /// Prüft die Preissortierung nach der ersten gewählten Sorte, wenn kein Filter gesetzt ist.
    /// </summary>
    [Fact]
    public void Build_PriceSort_UsesFirstSelectedFuelWithoutFilter()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "Teuer", 1.0, Now, null, null, (FuelType.SuperE5, 1.90m), (FuelType.Diesel, 1.50m)),
            StationFactory.CreateCustom(2, "Billig", 2.0, Now, null, null, (FuelType.SuperE5, 1.80m), (FuelType.Diesel, 1.70m)),
        ];

        Assert.Equal(["Billig", "Teuer"], Names(stations, null, ResultSortOrder.Price));
        Assert.Equal(["Teuer", "Billig"], Names(stations, null, ResultSortOrder.Price, SearchTestData.Only(FuelType.Diesel)));
    }

    /// <summary>
    /// Prüft, dass die Preissortierung bei gesetztem Filter den Preis der gefilterten Sorte nutzt.
    /// </summary>
    [Fact]
    public void Build_PriceSort_UsesFilteredFuel()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "A", 1.0, Now, null, null, (FuelType.SuperE5, 1.90m), (FuelType.Diesel, 1.50m)),
            StationFactory.CreateCustom(2, "B", 2.0, Now, null, null, (FuelType.SuperE5, 1.80m), (FuelType.Diesel, 1.70m)),
        ];

        Assert.Equal(["A", "B"], Names(stations, FuelType.Diesel, ResultSortOrder.Price));
    }

    /// <summary>
    /// Prüft, dass Stationen ohne Preis der Sortiersorte am Ende stehen.
    /// </summary>
    [Fact]
    public void Build_PriceSort_StationsWithoutSortPriceComeLast()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "OhneE5", 0.1, Now, null, null, (FuelType.Diesel, 1.50m)),
            StationFactory.CreateCustom(2, "MitE5", 5.0, Now, null, null, (FuelType.SuperE5, 1.99m)),
        ];

        Assert.Equal(["MitE5", "OhneE5"], Names(stations, null, ResultSortOrder.Price));
    }

    /// <summary>
    /// Prüft den Gleichstand beim Preis: dann entscheidet die Entfernung, danach der Name.
    /// </summary>
    [Fact]
    public void Build_PriceSort_TieBreaksByDistanceThenName()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "Zeta", 3.0, Now, null, null, (FuelType.SuperE5, 1.80m)),
            StationFactory.CreateCustom(2, "Beta", 2.0, Now, null, null, (FuelType.SuperE5, 1.80m)),
            StationFactory.CreateCustom(3, "Alpha", 2.0, Now, null, null, (FuelType.SuperE5, 1.80m)),
        ];

        Assert.Equal(["Alpha", "Beta", "Zeta"], Names(stations, null, ResultSortOrder.Price));
    }

    /// <summary>
    /// Prüft die Sortierung nach Entfernung; unbekannte Entfernungen stehen am Ende.
    /// </summary>
    [Fact]
    public void Build_DistanceSort_OrdersByDistanceUnknownLast()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "Unbekannt", null, Now, null, null, (FuelType.SuperE5, 1.50m)),
            StationFactory.CreateCustom(2, "Weit", 9.0, Now, null, null, (FuelType.SuperE5, 1.50m)),
            StationFactory.CreateCustom(3, "Nah", 1.0, Now, null, null, (FuelType.SuperE5, 1.90m)),
        ];

        Assert.Equal(["Nah", "Weit", "Unbekannt"], Names(stations, null, ResultSortOrder.Distance));
    }

    /// <summary>
    /// Prüft die Sortierung nach Name ohne Rücksicht auf Groß- und Kleinschreibung und mit deutschen Umlauten.
    /// </summary>
    [Fact]
    public void Build_NameSort_UsesGermanCultureOrdering()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "zebra", 1.0, Now, null, null, (FuelType.SuperE5, 1.50m)),
            StationFactory.CreateCustom(2, "Äpfel", 2.0, Now, null, null, (FuelType.SuperE5, 1.50m)),
            StationFactory.CreateCustom(3, "Beta", 3.0, Now, null, null, (FuelType.SuperE5, 1.50m)),
        ];

        Assert.Equal(["Äpfel", "Beta", "zebra"], Names(stations, null, ResultSortOrder.Name));
    }

    /// <summary>
    /// Prüft, dass ein undefinierter Sortierwert wie die Preissortierung wirkt.
    /// </summary>
    [Fact]
    public void Build_UndefinedSortOrder_FallsBackToPrice()
    {
        StationInfo[] stations =
        [
            StationFactory.CreateCustom(1, "Teuer", 1.0, Now, null, null, (FuelType.SuperE5, 1.90m)),
            StationFactory.CreateCustom(2, "Billig", 2.0, Now, null, null, (FuelType.SuperE5, 1.70m)),
        ];

        Assert.Equal(["Billig", "Teuer"], Names(stations, null, (ResultSortOrder)99));
    }
}
