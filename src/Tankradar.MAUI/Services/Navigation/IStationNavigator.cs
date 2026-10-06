using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet die Detailansicht einer Tankstelle (in der App über die Shell-Navigation, in Tests ein Ersatz). Die Rückkehr übernimmt die Kopfleiste der Shell.
/// </summary>
public interface IStationNavigator
{
    /// <summary>
    /// Öffnet die Detailansicht der Tankstelle.
    /// </summary>
    /// <param name="station">Die Tankstelle aus der Ergebnisliste; ihre Angaben dienen als Anfangsanzeige.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenDetailAsync(StationListItem station);
}
