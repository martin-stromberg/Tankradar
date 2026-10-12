using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Plausibilitätsprüfung von <see cref="AmountCalculator"/>: Toleranz 0,02 €, Meldung der Abweichung mit rechnerischem Betrag und fehlende Werte.
/// </summary>
public class AmountCalculatorTests_Plausibility : BaseTest
{
    /// <summary>
    /// Prüft, dass drei Werte innerhalb der Toleranz (auch genau 0,02 €) gültig sind.
    /// </summary>
    [Fact]
    public void Complete_ThreeValues_WithinTolerance_IsValid()
    {
        Assert.False(AmountCalculator.Complete(40m, 1.859m, 74.36m).HasMismatch);
        Assert.False(AmountCalculator.Complete(40m, 1.859m, 74.38m).HasMismatch);
        Assert.False(AmountCalculator.Complete(40m, 1.859m, 74.34m).HasMismatch);
        Assert.Null(AmountCalculator.Complete(40m, 1.859m, 74.36m).ComputedField);
    }

    /// <summary>
    /// Prüft, dass eine Abweichung über der Toleranz gemeldet wird und den rechnerischen Gesamtbetrag nennt.
    /// </summary>
    [Fact]
    public void Complete_ThreeValues_Deviation_ReportsMismatchAndExpectedTotal()
    {
        var result = AmountCalculator.Complete(40m, 1.859m, 75.00m);

        Assert.True(result.HasMismatch);
        Assert.Equal(74.36m, result.ExpectedTotal);
        Assert.True(AmountCalculator.Complete(40m, 1.859m, 74.39m).HasMismatch);
    }

    /// <summary>
    /// Prüft, dass ein berechneter Preis (gerundeter Quotient) beim erneuten Prüfen als stimmig gilt, auch wenn Menge × Preis um mehr als 0,02 € abweicht.
    /// </summary>
    [Fact]
    public void Complete_ThreeValues_ComputedRoundedQuotient_IsConsistent()
    {
        var computed = AmountCalculator.Complete(999m, null, 1857.62m);

        Assert.False(AmountCalculator.Complete(999m, computed.UnitPrice, 1857.62m).HasMismatch);
        Assert.True(AmountCalculator.IsConsistent(999m, computed.UnitPrice!.Value, 1857.62m));
    }

    /// <summary>
    /// Prüft, dass weniger als zwei Werte als fehlend gemeldet werden.
    /// </summary>
    [Fact]
    public void Complete_LessThanTwoValues_IsMissing()
    {
        Assert.True(AmountCalculator.Complete(40m, null, null).IsMissing);
        Assert.True(AmountCalculator.Complete(null, null, 74.36m).IsMissing);
        Assert.True(AmountCalculator.Complete(null, null, null).IsMissing);
        Assert.Null(AmountCalculator.Complete(40m, null, null).ComputedField);
    }
}
