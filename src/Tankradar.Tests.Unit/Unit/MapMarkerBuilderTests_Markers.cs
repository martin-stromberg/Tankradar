using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Markierungen der Karte: Preis der maßgeblichen Sorte, Preisniveau, Beschreibung, fehlende Positionen und die Wahl der Sorte.
/// </summary>
public class MapMarkerBuilderTests_Markers : BaseTest
{
    private static readonly DateTime Now = SearchTestData.Now;

    private static IReadOnlyList<StationListItem> Items(FuelType? filter, params StationInfo[] stations)
    {
        return StationResultBuilder.Build(stations, SearchTestData.AllFuels, filter, ResultSortOrder.Price, Now);
    }

    /// <summary>
    /// Prüft Preistext, Preisniveau und Beschreibung für die zuerst gewählte Sorte ohne Filter.
    /// </summary>
    [Fact]
    public void Build_WithoutFilter_UsesFirstSelectedFuel()
    {
        var items = Items(
            null,
            StationFactory.CreateCustom(1, "Billig", 1, Now, null, true, (FuelType.SuperE5, 1.80m), (FuelType.Diesel, 1.50m)),
            StationFactory.CreateCustom(2, "Teuer", 2, Now, null, true, (FuelType.SuperE5, 1.90m), (FuelType.Diesel, 1.40m)));
        var fuel = StationResultBuilder.ResolveFuelType(SearchTestData.AllFuels, null);

        var markers = MapMarkerBuilder.Build(items, fuel);

        Assert.Equal(FuelType.SuperE5, fuel);
        Assert.Equal(["Billig", "Teuer"], markers.Select(marker => marker.Station.Name));
        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Expensive], markers.Select(marker => marker.Level));
        Assert.Equal(["1,800 €", "1,900 €"], markers.Select(marker => marker.PriceText));
        Assert.Equal("Billig, Super E5 1,800 Euro pro Liter", markers[0].Description);
        Assert.All(markers, marker => Assert.True(marker.HasPrice));
    }

    /// <summary>
    /// Prüft, dass bei gewähltem Filter dessen Preise maßgeblich sind (umgekehrte Rangfolge gegenüber Super E5).
    /// </summary>
    [Fact]
    public void Build_WithFilter_UsesFilteredFuel()
    {
        var items = Items(
            FuelType.Diesel,
            StationFactory.CreateCustom(1, "Billig", 1, Now, null, true, (FuelType.SuperE5, 1.80m), (FuelType.Diesel, 1.50m)),
            StationFactory.CreateCustom(2, "Teuer", 2, Now, null, true, (FuelType.SuperE5, 1.90m), (FuelType.Diesel, 1.40m)));
        var fuel = StationResultBuilder.ResolveFuelType(SearchTestData.AllFuels, FuelType.Diesel);

        var markers = MapMarkerBuilder.Build(items, fuel);

        Assert.Equal(FuelType.Diesel, fuel);
        Assert.Equal(["Teuer", "Billig"], markers.Select(marker => marker.Station.Name));
        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Expensive], markers.Select(marker => marker.Level));
    }

    /// <summary>
    /// Prüft, dass ein nicht gewählter Filter auf die erste gewählte Sorte zurückfällt und ohne gewählte Sorte keine bestimmt wird.
    /// </summary>
    [Fact]
    public void ResolveFuelType_IgnoresUnselectedFilterAndHandlesNoSelection()
    {
        var onlyDiesel = SearchTestData.Only(FuelType.Diesel);

        Assert.Equal(FuelType.Diesel, StationResultBuilder.ResolveFuelType(onlyDiesel, FuelType.SuperE5));
        Assert.Equal(FuelType.Diesel, StationResultBuilder.ResolveFuelType(onlyDiesel, null));
        Assert.Null(StationResultBuilder.ResolveFuelType([new FuelTypeSelection(FuelType.SuperE5, false)], null));
        Assert.Throws<ArgumentNullException>(() => StationResultBuilder.ResolveFuelType(null!, null));
    }

    /// <summary>
    /// Prüft, dass geschlossene Stationen grau sind und die Spanne der offenen Stationen nicht verändern, unbekannter Öffnungszustand als offen gilt.
    /// </summary>
    [Fact]
    public void Build_ClosedStation_IsClosedAndIgnoredForSpan()
    {
        var items = Items(
            null,
            StationFactory.CreateCustom(1, "Zu", 1, Now, null, false, (FuelType.SuperE5, 1.10m)),
            StationFactory.CreateCustom(2, "Unbekannt", 2, Now, null, null, (FuelType.SuperE5, 1.80m)),
            StationFactory.CreateCustom(3, "Offen", 3, Now, null, true, (FuelType.SuperE5, 1.95m)));

        var markers = MapMarkerBuilder.Build(items, FuelType.SuperE5);

        Assert.Equal(PriceLevel.Closed, markers.Single(marker => marker.Station.Name == "Zu").Level);
        Assert.Equal(PriceLevel.Cheapest, markers.Single(marker => marker.Station.Name == "Unbekannt").Level);
        Assert.Equal(PriceLevel.Expensive, markers.Single(marker => marker.Station.Name == "Offen").Level);
    }

    /// <summary>
    /// Prüft, dass eine Station ohne Preis der Sorte eine Markierung ohne Preis bekommt, mit passender Beschreibung.
    /// </summary>
    [Fact]
    public void Build_StationWithoutPriceOfFuel_HasNoPriceText()
    {
        var items = Items(
            null,
            StationFactory.CreateCustom(1, "Nur Diesel", 1, Now, null, true, (FuelType.Diesel, 1.60m)),
            StationFactory.CreateCustom(2, "Mit E5", 2, Now, null, true, (FuelType.SuperE5, 1.80m)));

        var markers = MapMarkerBuilder.Build(items, FuelType.SuperE5);

        var without = markers.Single(marker => marker.Station.Name == "Nur Diesel");
        Assert.False(without.HasPrice);
        Assert.Equal(string.Empty, without.PriceText);
        Assert.Equal("Nur Diesel, kein Preis für Super E5", without.Description);
        Assert.Equal(PriceLevel.Cheapest, markers.Single(marker => marker.Station.Name == "Mit E5").Level);
    }

    /// <summary>
    /// Prüft, dass Stationen ohne gültige Position nicht auf der Karte erscheinen (auch nicht die Platzhalterposition 0/0) und ohne Sorte nur die Namen beschrieben werden.
    /// </summary>
    [Fact]
    public void Build_StationWithoutPosition_IsSkipped()
    {
        var invalid = new StationInfo
        {
            Id = "99999999-0000-4000-8000-000000000000",
            Name = "Ohne Ort",
            Prices = [new FuelPrice(FuelType.SuperE5, 1.7m, Now)],
        };
        var items = Items(null, invalid, StationFactory.CreateCustom(1, "Mit Ort", 1, Now, null, true, (FuelType.SuperE5, 1.8m)));

        Assert.False(items.Single(item => item.Name == "Ohne Ort").HasPosition);
        var markers = MapMarkerBuilder.Build(items, null);

        Assert.Equal(["Mit Ort"], markers.Select(marker => marker.Station.Name));
        Assert.Equal("Mit Ort", markers[0].Description);
        Assert.Equal(PriceLevel.Normal, markers[0].Level);
        Assert.Throws<ArgumentNullException>(() => MapMarkerBuilder.Build(null!, null));
    }

    /// <summary>
    /// Prüft die Texte der Karte: Bezeichnungen der Preisniveaus und Positionsarten, Zähler mit Einzahl und Mehrzahl, Zoomstufe und Quellenangabe.
    /// </summary>
    [Fact]
    public void MapTexts_FormatLabelsAndCounters()
    {
        Assert.Equal("Günstigster Preis", MapTexts.GetLevelLabel(PriceLevel.Cheapest));
        Assert.Equal("Mittlerer Preis", MapTexts.GetLevelLabel(PriceLevel.Normal));
        Assert.Equal("Hoher Preis", MapTexts.GetLevelLabel(PriceLevel.Expensive));
        Assert.Equal("Geschlossen", MapTexts.GetLevelLabel(PriceLevel.Closed));
        Assert.Equal("Mein Standort", MapTexts.GetOriginLabel(MapOriginKind.CurrentLocation));
        Assert.Equal("Gesuchte Position", MapTexts.GetOriginLabel(MapOriginKind.SearchedPlace));
        Assert.Equal("1 von 1 Station sichtbar", MapTexts.FormatStationCount(1, 1));
        Assert.Equal("3 von 12 Stationen sichtbar", MapTexts.FormatStationCount(3, 12));
        Assert.Equal("Zoom 14", MapTexts.FormatZoom(14));
        Assert.Equal("© OpenStreetMap-Mitwirkende", MapTexts.Attribution);
    }

    /// <summary>
    /// Prüft, dass die Textdarstellung der Suchposition keine Koordinaten enthält und die Farben der Preisniveaus verschieden sind.
    /// </summary>
    [Fact]
    public void MapOrigin_ToStringHidesCoordinates_AndPaletteIsDistinct()
    {
        var origin = new MapOrigin(MapOriginKind.CurrentLocation, 52.5200123, 13.4050456);

        Assert.DoesNotContain("52.52", origin.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("13.40", origin.ToString(), StringComparison.Ordinal);
        var colors = Enum.GetValues<PriceLevel>().Select(MAUI.Resources.MapPalette.For).Select(color => color.ToArgbHex()).Distinct().ToList();
        Assert.Equal(Enum.GetValues<PriceLevel>().Length, colors.Count);
    }
}
