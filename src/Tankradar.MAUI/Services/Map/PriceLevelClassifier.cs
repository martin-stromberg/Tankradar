using Tankradar.MAUI.Models.Map;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Stuft die Preise einer Ergebnismenge in Preisniveaus ein. Geschlossene Tankstellen sind grau und gehen nicht in die Preisspanne ein
/// (ein geschlossener Preis ist kein Tankangebot); die Spanne ergibt sich aus den übrigen Preisen der Menge.
/// Gleiche Preise sind gleich eingestuft: der niedrigste Preis ist für alle Tankstellen mit diesem Preis „günstigster Preis“,
/// bei fehlender Spanne (eine Tankstelle oder nur gleiche Preise) gibt es kein oberes Drittel.
/// </summary>
public static class PriceLevelClassifier
{
    /// <summary>
    /// Stuft die Tankstellen ein.
    /// </summary>
    /// <param name="inputs">Preis und Öffnungszustand je Tankstelle.</param>
    /// <returns>Das Preisniveau je Tankstelle in der Reihenfolge der Eingabe.</returns>
    public static IReadOnlyList<PriceLevel> Classify(IReadOnlyList<PriceLevelInput> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var open = inputs.Where(input => !input.IsClosed && input.Price is not null).Select(input => input.Price!.Value).ToList();
        if (open.Count == 0)
        {
            return inputs.Select(input => input.IsClosed ? PriceLevel.Closed : PriceLevel.Normal).ToList();
        }

        var min = open.Min();
        var max = open.Max();
        var span = max - min;
        var result = new List<PriceLevel>(inputs.Count);
        foreach (var input in inputs)
        {
            result.Add(Level(input, min, span));
        }

        return result;
    }

    private static PriceLevel Level(PriceLevelInput input, decimal min, decimal span)
    {
        if (input.IsClosed)
        {
            return PriceLevel.Closed;
        }

        if (input.Price is not { } price)
        {
            return PriceLevel.Normal;
        }

        if (price == min)
        {
            return PriceLevel.Cheapest;
        }

        // Oberes Drittel der Spanne: Preis >= min + 2/3 * Spanne, exakt in Dezimalarithmetik (ohne Rundung eines Drittels).
        return 3 * (price - min) >= 2 * span ? PriceLevel.Expensive : PriceLevel.Normal;
    }
}
