using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft <see cref="TankbookEntryValidator.ValidateForm"/>: alle Feldfehler gleichzeitig, berechneter dritter Wert, Zukunft, Längen.
/// </summary>
public class TankbookEntryValidatorTests_Form : BaseTest
{
    private static readonly DateTime Now = new(2026, 5, 18, 14, 30, 0);

    private static TankbookEntryFormValidation Validate(
        DateTime? occurredAt = null,
        string station = "Alpha Tankstelle",
        string quantity = "40",
        string unitPrice = "1,859",
        string total = "",
        string odometer = "10500",
        string note = "")
    {
        return TankbookEntryValidator.ValidateForm(
            occurredAt ?? Now.AddHours(-1),
            Now,
            TankbookEntryKind.Fuel,
            station,
            null,
            quantity,
            unitPrice,
            total,
            odometer,
            false,
            note,
            null);
    }

    /// <summary>
    /// Prüft, dass jedes fehlerhafte Feld gleichzeitig gemeldet und keine Eingabe gebaut wird.
    /// </summary>
    [Fact]
    public void ValidateForm_AllFieldsInvalid_ReportsEveryField()
    {
        var result = TankbookEntryValidator.ValidateForm(null, Now, TankbookEntryKind.Fuel, " ", null, "abc", "-1", "1,234", "", false, new string('x', 501), null);

        Assert.Equal(TankbookEntryFieldError.Required, result.OccurredAtError);
        Assert.Equal(TankbookEntryFieldError.Required, result.StationError);
        Assert.Equal(TankbookEntryFieldError.NotANumber, result.QuantityError);
        Assert.Equal(TankbookEntryFieldError.Negative, result.UnitPriceError);
        Assert.Equal(TankbookEntryFieldError.TooManyDecimals, result.TotalAmountError);
        Assert.Equal(TankbookEntryFieldError.Required, result.OdometerError);
        Assert.Equal(TankbookEntryFieldError.TooLong, result.NoteError);
        Assert.True(result.HasErrors);
        Assert.Null(result.Input);
    }

    /// <summary>
    /// Prüft, dass eine gültige Eingabe den berechneten Gesamtbetrag enthält, Sekunden verwirft und Texte trimmt.
    /// </summary>
    [Fact]
    public void ValidateForm_Valid_BuildsInputWithComputedValue()
    {
        var result = Validate(occurredAt: new DateTime(2026, 5, 18, 9, 15, 42), station: "  Alpha Tankstelle ", odometer: "10.500", note: " Urlaub ");

        Assert.False(result.HasErrors);
        var input = Assert.IsType<TankbookEntryInput>(result.Input);
        Assert.Equal(74.36m, input.TotalAmount);
        Assert.Equal(40m, input.Quantity);
        Assert.Equal(1.859m, input.UnitPrice);
        Assert.Equal(10_500, input.OdometerKm);
        Assert.Equal(new DateTime(2026, 5, 18, 9, 15, 0), input.OccurredAt);
        Assert.Equal("Alpha Tankstelle", input.StationName);
        Assert.Equal("Urlaub", input.Note);
    }

    /// <summary>
    /// Prüft, dass ein Zeitpunkt nach jetzt plus einer Minute als Zukunft gemeldet wird; innerhalb der Minute ist er erlaubt.
    /// </summary>
    [Fact]
    public void ValidateForm_FutureDate_ReportsInFuture()
    {
        Assert.Equal(TankbookEntryFieldError.InFuture, Validate(occurredAt: Now.AddMinutes(2)).OccurredAtError);
        Assert.Equal(TankbookEntryFieldError.None, Validate(occurredAt: Now.AddMinutes(1)).OccurredAtError);
    }

    /// <summary>
    /// Prüft die Höchstlänge des Tankstellennamens.
    /// </summary>
    [Fact]
    public void ValidateForm_StationTooLong()
    {
        Assert.Equal(TankbookEntryFieldError.TooLong, Validate(station: new string('a', 61)).StationError);
        Assert.Equal(TankbookEntryFieldError.None, Validate(station: new string('a', 60)).StationError);
    }

    /// <summary>
    /// Prüft die Höchstlänge der Notiz.
    /// </summary>
    [Fact]
    public void ValidateForm_NoteTooLong()
    {
        Assert.Equal(TankbookEntryFieldError.TooLong, Validate(note: new string('a', 501)).NoteError);
        Assert.Equal(TankbookEntryFieldError.None, Validate(note: new string('a', 500)).NoteError);
    }

    /// <summary>
    /// Prüft fehlende Werte, eine Abweichung mit rechnerischem Betrag und einen berechneten Wert außerhalb des Bereichs.
    /// </summary>
    [Fact]
    public void ValidateForm_Amounts_ReportMissingMismatchAndRange()
    {
        Assert.Equal(TankbookEntryFieldError.MissingAmounts, Validate(unitPrice: "").TotalAmountError);

        var mismatch = Validate(total: "75,00");
        Assert.Equal(TankbookEntryFieldError.AmountMismatch, mismatch.TotalAmountError);
        Assert.Equal(74.36m, mismatch.ExpectedTotal);

        Assert.Equal(TankbookEntryFieldError.OutOfRange, Validate(quantity: "1", unitPrice: "", total: "50").UnitPriceError);
    }
}
