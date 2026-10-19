namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet und schließt die Seite der Tank- und Ladevorgänge eines Fahrzeugs (in der App über die Shell-Navigation, in Tests ein Ersatz).
/// </summary>
public interface ITankbookNavigator
{
    /// <summary>
    /// Öffnet die Vorgänge eines Fahrzeugs.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenEntriesAsync(long vehicleId);

    /// <summary>
    /// Schließt die Seite der Vorgänge des Fahrzeugs und kehrt zum Tankbuch zurück (z. B. wenn das Fahrzeug nicht mehr existiert);
    /// die Seiten anderer Fahrzeuge bleiben unberührt.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs, dessen Seite geschlossen wird.</param>
    /// <returns>Ein Task, der nach der Rückkehr abgeschlossen ist.</returns>
    Task CloseAsync(long vehicleId);
}
