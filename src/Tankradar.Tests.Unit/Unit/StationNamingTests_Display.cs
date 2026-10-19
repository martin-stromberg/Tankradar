using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Regeln für den angezeigten Namen einer Tankstelle: eigener Name vor Marke vor Name der Quelle, Stammdatenname klein darunter.
/// </summary>
public class StationNamingTests_Display : BaseTest
{
    /// <summary>
    /// Prüft, dass die Marke vorn steht und der Stammdatenname darunter, wenn eine Marke vorliegt.
    /// </summary>
    [Fact]
    public void Brand_IsPrimary_NameIsSecondary()
    {
        Assert.Equal("Westfalen", StationNaming.GetPrimary(null, "Westfalen", "Klaus Heinrich"));
        Assert.Equal("Klaus Heinrich", StationNaming.GetSecondary(null, "Westfalen", "Klaus Heinrich"));
    }

    /// <summary>
    /// Prüft, dass ohne Marke (fehlend, leer oder nur Leerraum) der Name bleibt und nichts darunter steht.
    /// </summary>
    /// <param name="brand">Die Marke der Quelle.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoBrand_NameStays_NoSecondary(string? brand)
    {
        Assert.Equal("Alpha Tankstelle", StationNaming.GetPrimary(null, brand, "Alpha Tankstelle"));
        Assert.Equal(string.Empty, StationNaming.GetSecondary(null, brand, "Alpha Tankstelle"));
    }

    /// <summary>
    /// Prüft, dass der eigene Anzeigename Vorrang hat und der Stammdatenname darunter steht.
    /// </summary>
    [Fact]
    public void CustomName_HasPrecedence()
    {
        Assert.Equal("Meine Tankstelle", StationNaming.GetPrimary("  Meine Tankstelle ", "Westfalen", "Klaus Heinrich"));
        Assert.Equal("Klaus Heinrich", StationNaming.GetSecondary("Meine Tankstelle", "Westfalen", "Klaus Heinrich"));
    }

    /// <summary>
    /// Prüft, dass ein leerer eigener Name als nicht vergeben gilt.
    /// </summary>
    [Fact]
    public void BlankCustomName_FallsBackToBrand()
    {
        Assert.Equal("Westfalen", StationNaming.GetPrimary("  ", "Westfalen", "Klaus Heinrich"));
    }

    /// <summary>
    /// Prüft, dass ein Name, der der Marke nur in der Schreibweise gleicht, nicht doppelt erscheint.
    /// </summary>
    [Fact]
    public void BrandEqualToName_IgnoringCase_HasNoSecondary()
    {
        Assert.Equal("ARAL", StationNaming.GetPrimary(null, "ARAL", "Aral"));
        Assert.Equal(string.Empty, StationNaming.GetSecondary(null, "ARAL", "Aral"));
    }

    /// <summary>
    /// Prüft, dass ein eigener Name, der dem Namen der Quelle entspricht, keinen zweiten Namen erzeugt.
    /// </summary>
    [Fact]
    public void CustomNameEqualToName_HasNoSecondary()
    {
        Assert.Equal(string.Empty, StationNaming.GetSecondary("alpha tankstelle", "Westfalen", "Alpha Tankstelle"));
    }

    /// <summary>
    /// Prüft die Ableitung der Anzeigenamen am Modell <see cref="StationInfo"/> einschließlich der Kopie mit anderem eigenen Namen.
    /// </summary>
    [Fact]
    public void StationInfo_ExposesDisplayNames()
    {
        var station = new StationInfo { Id = "00000001-0000-4000-8000-000000000000", Name = "Klaus Heinrich", Brand = "Westfalen" };

        Assert.Equal("Westfalen", station.DisplayName);
        Assert.Equal("Klaus Heinrich", station.SubName);

        var custom = station.WithCustomName("Mein Tanken");
        Assert.Equal("Mein Tanken", custom.DisplayName);
        Assert.Equal("Klaus Heinrich", custom.SubName);
        Assert.Equal("Westfalen", custom.Brand);
        Assert.Equal("Westfalen", custom.WithCustomName(null).DisplayName);
    }

    /// <summary>
    /// Prüft, dass alle Kopiermethoden von <see cref="StationInfo"/> den eigenen Namen behalten.
    /// </summary>
    [Fact]
    public void StationInfo_Copies_KeepCustomName()
    {
        var station = new StationInfo { Id = "00000001-0000-4000-8000-000000000000", Name = "Klaus Heinrich", CustomName = "Mein Tanken" };

        Assert.Equal("Mein Tanken", station.WithDistance(1.5).CustomName);
        Assert.Equal("Mein Tanken", station.With([]).CustomName);
        Assert.Equal("Mein Tanken", station.WithDetails(station).CustomName);
        Assert.Equal("Mein Tanken", station.WithoutStaleDetails(DateTime.UtcNow).CustomName);
        Assert.Equal("Mein Tanken", new StationInfo { Id = station.Id, Name = station.Name, CustomName = "Mein Tanken", DetailsUpdatedUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) }.WithoutStaleDetails(DateTime.UtcNow).CustomName);
    }
}
