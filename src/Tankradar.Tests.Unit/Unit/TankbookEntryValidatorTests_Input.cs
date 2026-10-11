using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Schutzprüfung <see cref="TankbookEntryValidator.Validate"/> und die erlaubten Arten je Kraftstoffart.
/// </summary>
public class TankbookEntryValidatorTests_Input : BaseTest
{
    private static readonly DateTime Now = new(2026, 5, 18, 14, 30, 0);

    /// <summary>
    /// Liefert die Fälle der Schutzprüfung.
    /// </summary>
    /// <returns>Eingabe und erwartetes Ergebnis.</returns>
    public static TheoryData<TankbookEntryInput, TankbookEntryResult> Cases()
    {
        var valid = new TankbookEntryInput(Now.AddDays(-1), TankbookEntryKind.Fuel, 40m, 1.859m, 74.36m, 10_500, false, "Alpha Tankstelle", null, null, null);
        return new TheoryData<TankbookEntryInput, TankbookEntryResult>
        {
            { valid, TankbookEntryResult.Ok },
            { valid with { OccurredAt = Now.AddMinutes(5) }, TankbookEntryResult.InvalidOccurredAt },
            { valid with { OccurredAt = default }, TankbookEntryResult.InvalidOccurredAt },
            { valid with { Kind = (TankbookEntryKind)99 }, TankbookEntryResult.KindNotAllowed },
            { valid with { Quantity = 0m }, TankbookEntryResult.InvalidQuantity },
            { valid with { Quantity = 1.2345m }, TankbookEntryResult.InvalidQuantity },
            { valid with { UnitPrice = 10m }, TankbookEntryResult.InvalidUnitPrice },
            { valid with { TotalAmount = 74.361m }, TankbookEntryResult.InvalidTotalAmount },
            { valid with { TotalAmount = 80m }, TankbookEntryResult.AmountMismatch },
            { valid with { OdometerKm = -1 }, TankbookEntryResult.InvalidOdometer },
            { valid with { StationName = " " }, TankbookEntryResult.InvalidStationName },
            { valid with { StationName = new string('a', 61) }, TankbookEntryResult.InvalidStationName },
            { valid with { Note = new string('a', 501) }, TankbookEntryResult.NoteTooLong },
        };
    }

    /// <summary>
    /// Prüft das Ergebnis der Schutzprüfung.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <param name="expected">Das erwartete Ergebnis.</param>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate_Input_ReturnsResult(TankbookEntryInput input, TankbookEntryResult expected)
    {
        Assert.Equal(expected, TankbookEntryValidator.Validate(input, Now));
    }

    /// <summary>
    /// Prüft die erlaubten Arten je Kraftstoffart.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart des Fahrzeugs.</param>
    /// <param name="kind">Die Art des Vorgangs.</param>
    /// <param name="expected">Ob die Art erlaubt ist.</param>
    [Theory]
    [InlineData(VehicleFuelKind.Petrol, TankbookEntryKind.Fuel, true)]
    [InlineData(VehicleFuelKind.Petrol, TankbookEntryKind.Electricity, false)]
    [InlineData(VehicleFuelKind.Diesel, TankbookEntryKind.Fuel, true)]
    [InlineData(VehicleFuelKind.Diesel, TankbookEntryKind.Electricity, false)]
    [InlineData(VehicleFuelKind.Electric, TankbookEntryKind.Fuel, false)]
    [InlineData(VehicleFuelKind.Electric, TankbookEntryKind.Electricity, true)]
    [InlineData(VehicleFuelKind.Hybrid, TankbookEntryKind.Fuel, true)]
    [InlineData(VehicleFuelKind.Hybrid, TankbookEntryKind.Electricity, true)]
    public void IsKindAllowed_ByVehicleFuelKind(VehicleFuelKind fuelKind, TankbookEntryKind kind, bool expected)
    {
        Assert.Equal(expected, TankbookEntryValidator.IsKindAllowed(fuelKind, kind));
    }
}
