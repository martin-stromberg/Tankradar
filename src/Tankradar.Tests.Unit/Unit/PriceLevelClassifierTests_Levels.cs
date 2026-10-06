using Tankradar.MAUI.Models.Map;
using Tankradar.MAUI.Services.Map;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einstufung der Preisniveaus: günstigster Preis, oberes Drittel der Spanne, übrige Preise und geschlossene Tankstellen, inklusive Grenzfällen.
/// </summary>
public class PriceLevelClassifierTests_Levels : BaseTest
{
    private static PriceLevelInput Open(decimal? price)
    {
        return new PriceLevelInput(price, false);
    }

    private static PriceLevelInput Closed(decimal? price)
    {
        return new PriceLevelInput(price, true);
    }

    /// <summary>
    /// Prüft die Grundeinstufung: niedrigster Preis grün, oberes Drittel rot, Rest teal.
    /// </summary>
    [Fact]
    public void Classify_SpreadOfPrices_AssignsCheapestNormalAndExpensive()
    {
        var levels = PriceLevelClassifier.Classify([Open(1.60m), Open(1.70m), Open(1.80m), Open(1.90m), Open(2.00m)]);

        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Normal, PriceLevel.Normal, PriceLevel.Expensive, PriceLevel.Expensive], levels);
    }

    /// <summary>
    /// Prüft die Grenze des oberen Drittels: genau min + 2/3 der Spanne zählt bereits zum oberen Drittel, knapp darunter nicht.
    /// </summary>
    [Fact]
    public void Classify_ExactlyAtUpperThirdBoundary_IsExpensive()
    {
        var levels = PriceLevelClassifier.Classify([Open(1.500m), Open(1.899m), Open(1.900m), Open(2.100m)]);

        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Normal, PriceLevel.Expensive, PriceLevel.Expensive], levels);
    }

    /// <summary>
    /// Prüft, dass gleiche Preise gleich eingestuft werden: alle grün, kein rot.
    /// </summary>
    [Fact]
    public void Classify_EqualPrices_AreAllCheapest()
    {
        var levels = PriceLevelClassifier.Classify([Open(1.799m), Open(1.799m), Open(1.799m)]);

        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Cheapest, PriceLevel.Cheapest], levels);
    }

    /// <summary>
    /// Prüft, dass mehrere Stationen mit dem niedrigsten Preis alle grün sind.
    /// </summary>
    [Fact]
    public void Classify_TiedCheapest_AreAllCheapest()
    {
        var levels = PriceLevelClassifier.Classify([Open(1.70m), Open(1.70m), Open(1.90m)]);

        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Cheapest, PriceLevel.Expensive], levels);
    }

    /// <summary>
    /// Prüft, dass eine einzelne Tankstelle grün ist (sie ist der günstigste Preis) und nicht rot.
    /// </summary>
    [Fact]
    public void Classify_SingleStation_IsCheapest()
    {
        Assert.Equal([PriceLevel.Cheapest], PriceLevelClassifier.Classify([Open(1.99m)]));
    }

    /// <summary>
    /// Prüft den Fall zweier verschiedener Preise: der teurere liegt im oberen Drittel.
    /// </summary>
    [Fact]
    public void Classify_TwoDifferentPrices_CheapestAndExpensive()
    {
        Assert.Equal([PriceLevel.Cheapest, PriceLevel.Expensive], PriceLevelClassifier.Classify([Open(1.70m), Open(1.71m)]));
    }

    /// <summary>
    /// Prüft, dass geschlossene Tankstellen grau sind und nicht in die Preisspanne eingehen (auch nicht als günstigster oder teuerster Preis).
    /// </summary>
    [Fact]
    public void Classify_ClosedStations_AreGreyAndIgnoredForSpan()
    {
        var levels = PriceLevelClassifier.Classify([Closed(1.20m), Open(1.70m), Open(1.80m), Closed(2.50m)]);

        Assert.Equal([PriceLevel.Closed, PriceLevel.Cheapest, PriceLevel.Expensive, PriceLevel.Closed], levels);
    }

    /// <summary>
    /// Prüft, dass bei ausschließlich geschlossenen Tankstellen alle grau sind.
    /// </summary>
    [Fact]
    public void Classify_OnlyClosedStations_AreAllClosed()
    {
        Assert.Equal([PriceLevel.Closed, PriceLevel.Closed], PriceLevelClassifier.Classify([Closed(1.70m), Closed(null)]));
    }

    /// <summary>
    /// Prüft, dass eine Tankstelle ohne Preis der Sorte zu den übrigen (teal) zählt und die Spanne nicht beeinflusst.
    /// </summary>
    [Fact]
    public void Classify_StationWithoutPrice_IsNormalAndDoesNotChangeSpan()
    {
        var levels = PriceLevelClassifier.Classify([Open(null), Open(1.70m), Open(1.90m)]);

        Assert.Equal([PriceLevel.Normal, PriceLevel.Cheapest, PriceLevel.Expensive], levels);
    }

    /// <summary>
    /// Prüft, dass eine leere Ergebnismenge eine leere Einstufung liefert und ein fehlender Parameter abgelehnt wird.
    /// </summary>
    [Fact]
    public void Classify_EmptyAndNull()
    {
        Assert.Empty(PriceLevelClassifier.Classify([]));
        Assert.Throws<ArgumentNullException>(() => PriceLevelClassifier.Classify(null!));
    }
}
