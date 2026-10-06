using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet die Detailansicht einer Tankstelle und kehrt zur vorherigen Ansicht zurück (in der App über die Shell-Navigation, in Tests ein Ersatz).
/// </summary>
public interface IStationNavigator
{
    /// <summary>
    /// Öffnet die Detailansicht der Tankstelle.
    /// </summary>
    /// <param name="station">Die Tankstelle aus der Ergebnisliste; ihre Angaben dienen als Anfangsanzeige.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenDetailAsync(StationListItem station);

    /// <summary>
    /// Kehrt von der Detailansicht zur vorherigen Ansicht zurück.
    /// </summary>
    /// <returns>Ein Task, der nach der Navigation abgeschlossen ist.</returns>
    Task GoBackAsync();
}
