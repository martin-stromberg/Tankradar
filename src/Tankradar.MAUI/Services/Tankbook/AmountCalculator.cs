using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Rechenbaustein für Menge, Preis pro Einheit und Gesamtbetrag eines Tank- oder Ladevorgangs: Aus zwei Werten ergibt sich der dritte
/// (kaufmännisch gerundet), drei Werte werden mit der Toleranz <see cref="TankbookEntryLimits.AmountTolerance"/> auf Abweichungen geprüft.
/// </summary>
public static class AmountCalculator
{
    /// <summary>
    /// Ergänzt den fehlenden Wert bzw. prüft drei angegebene Werte.
    /// Gesamtbetrag = Menge × Preis (2 Nachkommastellen), Preis = Gesamtbetrag / Menge (3 Nachkommastellen), Menge = Gesamtbetrag / Preis (2 Nachkommastellen).
    /// </summary>
    /// <param name="quantity">Die Menge oder <see langword="null"/>.</param>
    /// <param name="unitPrice">Der Preis pro Einheit oder <see langword="null"/>.</param>
    /// <param name="totalAmount">Der Gesamtbetrag oder <see langword="null"/>.</param>
    /// <returns>Die ergänzten Werte, der berechnete Wert sowie Abweichung bzw. fehlende Werte.</returns>
    public static AmountCompletion Complete(decimal? quantity, decimal? unitPrice, decimal? totalAmount)
    {
        var given = (quantity.HasValue ? 1 : 0) + (unitPrice.HasValue ? 1 : 0) + (totalAmount.HasValue ? 1 : 0);
        if (given < 2)
        {
            return new AmountCompletion(quantity, unitPrice, totalAmount, null, false, null, true);
        }

        if (given == 3)
        {
            var consistent = IsConsistent(quantity!.Value, unitPrice!.Value, totalAmount!.Value);
            return new AmountCompletion(
                quantity,
                unitPrice,
                totalAmount,
                null,
                !consistent,
                consistent ? null : Round(quantity.Value * unitPrice.Value, TankbookEntryLimits.MaxTotalAmountDecimals),
                false);
        }

        if (totalAmount is null)
        {
            var total = Round(quantity!.Value * unitPrice!.Value, TankbookEntryLimits.MaxTotalAmountDecimals);
            return new AmountCompletion(quantity, unitPrice, total, AmountField.TotalAmount, false, null, false);
        }

        if (unitPrice is null)
        {
            return quantity!.Value > 0m
                ? new AmountCompletion(quantity, Round(totalAmount.Value / quantity.Value, TankbookEntryLimits.MaxUnitPriceDecimals), totalAmount, AmountField.UnitPrice, false, null, false)
                : new AmountCompletion(quantity, null, totalAmount, null, false, null, false);
        }

        return unitPrice.Value > 0m
            ? new AmountCompletion(Round(totalAmount.Value / unitPrice.Value, TankbookEntryLimits.ComputedQuantityDecimals), unitPrice, totalAmount, AmountField.Quantity, false, null, false)
            : new AmountCompletion(null, unitPrice, totalAmount, null, false, null, false);
    }

    /// <summary>
    /// Prüft, ob drei Werte zueinander passen: |Menge × Preis − Gesamtbetrag| höchstens <see cref="TankbookEntryLimits.AmountTolerance"/>,
    /// oder einer der Werte ist genau der gerundete Quotient der beiden anderen (so berechnete Werte bleiben beim Korrigieren speicherbar).
    /// </summary>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="unitPrice">Der Preis pro Einheit.</param>
    /// <param name="totalAmount">Der Gesamtbetrag.</param>
    /// <returns><see langword="true"/>, wenn die Werte zueinander passen.</returns>
    public static bool IsConsistent(decimal quantity, decimal unitPrice, decimal totalAmount)
    {
        if (Math.Abs((quantity * unitPrice) - totalAmount) <= TankbookEntryLimits.AmountTolerance)
        {
            return true;
        }

        if (quantity > 0m && Round(totalAmount / quantity, TankbookEntryLimits.MaxUnitPriceDecimals) == unitPrice)
        {
            return true;
        }

        return unitPrice > 0m && Round(totalAmount / unitPrice, TankbookEntryLimits.ComputedQuantityDecimals) == quantity;
    }

    private static decimal Round(decimal value, int decimals)
    {
        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }
}
