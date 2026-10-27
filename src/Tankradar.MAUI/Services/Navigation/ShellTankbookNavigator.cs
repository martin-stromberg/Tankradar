namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Umsetzung von <see cref="ITankbookNavigator"/> über die Shell-Navigation der App.
/// </summary>
public sealed class ShellTankbookNavigator : ITankbookNavigator
{
    /// <summary>
    /// Die Route der Fahrzeugverwaltung.
    /// </summary>
    public const string VehiclesRoute = "tankbookvehicles";

    /// <summary>
    /// Der Name des Navigationsparameters, der angibt, ob die Verwaltung sofort das Formular „Neues Fahrzeug“ öffnet.
    /// </summary>
    public const string StartCreateParameter = "startCreate";

    /// <inheritdoc />
    public Task OpenVehicleManagementAsync(bool startCreate)
    {
        var parameters = new Dictionary<string, object> { [StartCreateParameter] = startCreate };
        return Shell.Current.GoToAsync(VehiclesRoute, parameters);
    }
}
