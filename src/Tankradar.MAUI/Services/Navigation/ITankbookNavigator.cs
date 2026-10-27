namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet die Fahrzeugverwaltung des Tankbuchs (in der App über die Shell-Navigation, in Tests ein Ersatz).
/// </summary>
public interface ITankbookNavigator
{
    /// <summary>
    /// Öffnet die Fahrzeugverwaltung.
    /// </summary>
    /// <param name="startCreate">Gibt an, ob dort sofort das Formular „Neues Fahrzeug“ geöffnet wird.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenVehicleManagementAsync(bool startCreate);
}
