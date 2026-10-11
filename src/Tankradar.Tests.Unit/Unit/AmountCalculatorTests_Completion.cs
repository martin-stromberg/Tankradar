using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="AmountCalculator.Complete"/>: Aus zwei Werten ergibt sich der dritte, kaufmännisch gerundet.
/// </summary>
public class AmountCalculatorTests_Completion : BaseTest
{
    /// <summary>
    /// Prüft die Berechnung des fehlenden Werts je Feld samt Rundung.
    /// </summary>
    /// <param name="quantity">Die Menge oder leer.</param>
    /// <param name="unitPrice">Der Preis oder leer.</param>
    /// <param name="total">Der Gesamtbetrag oder leer.</param>
    /// <param name="field">Der erwartete berechnete Wert.</param>
    /// <param name="expected">Das erwartete Ergebnis (invariant).</param>
    [Theory]
    [InlineData("40", "1.859", "", AmountField.TotalAmount, "74.36")]
    [InlineData("12.345", "0.59", "", AmountField.TotalAmount, "7.28")]
    [InlineData("30", "", "55.77", AmountField.UnitPrice, "1.859")]
    [InlineData("40", "", "23.59", AmountField.UnitPrice, "0.590")]
    [InlineData("", "1.859", "55.77", AmountField.Quantity, "30.00")]
    [InlineData("", "0.59", "10", AmountField.Quantity, "16.95")]
    public void Complete_TwoValues_ComputesThird(string quantity, string unitPrice, string total, AmountField field, string expected)
    {
        var result = AmountCalculator.Complete(Parse(quantity), Parse(unitPrice), Parse(total));

        Assert.Equal(field, result.ComputedField);
        Assert.False(result.IsMissing);
        Assert.False(result.HasMismatch);
        var computed = field switch
        {
            AmountField.Quantity => result.Quantity,
            AmountField.UnitPrice => result.UnitPrice,
            _ => result.TotalAmount,
        };
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), computed);
    }

    private static decimal? Parse(string text)
    {
        return text.Length == 0 ? null : decimal.Parse(text, CultureInfo.InvariantCulture);
    }
}
