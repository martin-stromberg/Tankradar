using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Umsetzung von <see cref="IStationNavigator"/> über die Shell-Navigation der App.
/// </summary>
public sealed class ShellStationNavigator : IStationNavigator
{
    /// <summary>
    /// Die Route der Detailansicht.
    /// </summary>
    public const string DetailRoute = "stationdetail";

    /// <summary>
    /// Der Name des Navigationsparameters, der die Tankstelle aus der Ergebnisliste trägt.
    /// </summary>
    public const string StationParameter = "station";

    /// <inheritdoc />
    public Task OpenDetailAsync(StationListItem station)
    {
        ArgumentNullException.ThrowIfNull(station);
        var parameters = new Dictionary<string, object> { [StationParameter] = station };
        return Shell.Current.GoToAsync(DetailRoute, parameters);
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
