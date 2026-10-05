using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Search;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Adresszeile der Ergebniskarten; fehlende Angaben werden ausgelassen und nie ersetzt.
/// </summary>
public class StationResultBuilderTests_Address : BaseTest
{
    /// <summary>
    /// Prüft die Formatierung vollständiger und teilweiser Adressen.
    /// </summary>
    /// <param name="street">Die Straße.</param>
    /// <param name="houseNumber">Die Hausnummer.</param>
    /// <param name="postCode">Die Postleitzahl.</param>
    /// <param name="place">Der Ort.</param>
    /// <param name="expected">Die erwartete Adresszeile.</param>
    [Theory]
    [InlineData("Hauptstraße", "1", "10115", "Berlin", "Hauptstraße 1, 10115 Berlin")]
    [InlineData("Hauptstraße", null, "10115", "Berlin", "Hauptstraße, 10115 Berlin")]
    [InlineData(null, null, null, "Berlin", "Berlin")]
    [InlineData(" ", "", null, null, "")]
    public void FormatAddress_OmitsMissingParts(string? street, string? houseNumber, string? postCode, string? place, string expected)
    {
        Assert.Equal(expected, SearchTexts.FormatAddress(street, houseNumber, postCode, place));
    }

    /// <summary>
    /// Prüft, dass die Ergebnisliste die Adresszeile der Quelle übernimmt und ohne Adresse leer lässt.
    /// </summary>
    [Fact]
    public void Build_UsesAddressFromSource()
    {
        var withAddress = StationFactory.CreateCustom(1, "A", 1.0, SearchTestData.Now, null, null, (FuelType.SuperE5, 1.8m));
        var items = StationResultBuilder.Build(
            [new StationInfo { Id = withAddress.Id, Name = "A", Street = "Weg", HouseNumber = "2", PostCode = "10115", Place = "Berlin", Prices = withAddress.Prices }, StationFactory.CreateCustom(2, "B", 2.0, SearchTestData.Now, null, null, (FuelType.SuperE5, 1.9m))],
            SearchTestData.AllFuels,
            null,
            ResultSortOrder.Price,
            SearchTestData.Now);

        Assert.Equal("Weg 2, 10115 Berlin", items[0].AddressText);
        Assert.True(items[0].HasAddress);
        Assert.Equal(string.Empty, items[1].AddressText);
        Assert.False(items[1].HasAddress);
    }
}
