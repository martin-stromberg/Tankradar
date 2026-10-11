namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Kraftstoffart eines Fahrzeugs im Tankbuch. Die Namen der Werte sind Persistenzvertrag (Spalte <c>Vehicles.FuelKind</c>).
/// </summary>
public enum VehicleFuelKind
{
    /// <summary>
    /// Benzin.
    /// </summary>
    Petrol,

    /// <summary>
    /// Diesel.
    /// </summary>
    Diesel,

    /// <summary>
    /// Elektro; der Verbrauch wird in kWh/100 km angegeben.
    /// </summary>
    Electric,

    /// <summary>
    /// Hybrid; der Verbrauch wird wie bei Benzin in L/100 km angegeben.
    /// </summary>
    Hybrid,
}
