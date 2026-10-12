namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Umsetzung von <see cref="ITankbookNavigator"/> über die Shell-Navigation der App.
/// </summary>
public sealed class ShellTankbookNavigator : ITankbookNavigator
{
    /// <summary>
    /// Die Route der Seite der Tank- und Ladevorgänge.
    /// </summary>
    public const string EntriesRoute = "tankbookentries";

    /// <summary>
    /// Der Name des Navigationsparameters, der die Kennung des Fahrzeugs trägt.
    /// </summary>
    public const string VehicleIdParameter = "vehicleId";

    /// <inheritdoc />
    public Task OpenEntriesAsync(long vehicleId)
    {
        var parameters = new Dictionary<string, object> { [VehicleIdParameter] = vehicleId };
        return Shell.Current.GoToAsync(EntriesRoute, parameters);
    }

    /// <inheritdoc />
    public Task CloseAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
