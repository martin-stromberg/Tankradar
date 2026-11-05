namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Merkt sich gerätelokal die Kennung des im Tab „Tankbuch“ gewählten Fahrzeugs (nur die Kennung, keine Namen oder Werte).
/// </summary>
public interface IVehicleSelectionStore
{
    /// <summary>
    /// Liefert die gemerkte Kennung.
    /// </summary>
    /// <returns>Die Kennung oder <see langword="null"/>, wenn keine (gültige) gemerkt ist.</returns>
    long? Get();

    /// <summary>
    /// Merkt sich die Kennung.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    void Set(long vehicleId);

    /// <summary>
    /// Vergisst die gemerkte Kennung (z. B. nach einer Wiederherstellung).
    /// </summary>
    void Clear();
}
