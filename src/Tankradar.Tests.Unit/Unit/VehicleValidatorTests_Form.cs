using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die feldbezogene Prüfung des Fahrzeugformulars: alle Fehler gleichzeitig, getrimmter Name und Übernahme der Werte.
/// </summary>
public class VehicleValidatorTests_Form : BaseTest
{
    /// <summary>
    /// Prüft, dass alle drei Feldfehler gleichzeitig gemeldet werden und keine Eingabe entsteht.
    /// </summary>
    [Fact]
    public void ValidateForm_AllFieldsInvalid_ReportsEveryFieldAndNoInput()
    {
        var result = VehicleValidator.ValidateForm("   ", VehicleFuelKind.Diesel, "-5", "abc");

        Assert.Equal(VehicleFieldError.Required, result.NameError);
        Assert.Equal(VehicleFieldError.Negative, result.OdometerError);
        Assert.Equal(VehicleFieldError.NotANumber, result.ConsumptionError);
        Assert.True(result.HasErrors);
        Assert.Null(result.Input);
    }

    /// <summary>
    /// Prüft, dass gültige Felder eine Eingabe mit getrimmtem Namen und übernommenen Werten ergeben.
    /// </summary>
    [Fact]
    public void ValidateForm_ValidFields_BuildsTrimmedInput()
    {
        var result = VehicleValidator.ValidateForm("  Golf  ", VehicleFuelKind.Electric, "84.350", "16,5");

        Assert.False(result.HasErrors);
        Assert.Equal(VehicleFieldError.None, result.NameError);
        Assert.Equal(new VehicleInput("Golf", VehicleFuelKind.Electric, 84350, 16.5m), result.Input);
    }

    /// <summary>
    /// Prüft, dass ein Name mit 41 Zeichen als zu lang gilt.
    /// </summary>
    [Fact]
    public void ValidateForm_NameTooLong_ReportsTooLong()
    {
        var result = VehicleValidator.ValidateForm(new string('A', VehicleLimits.MaxNameLength + 1), VehicleFuelKind.Petrol, "0", string.Empty);

        Assert.Equal(VehicleFieldError.TooLong, result.NameError);
        Assert.Equal(VehicleFieldError.None, result.OdometerError);
        Assert.Equal(VehicleFieldError.None, result.ConsumptionError);
        Assert.Null(result.Input);
    }
}
