namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet und schließt die Gruppenansicht einer Favoritengruppe (in der App über die Shell-Navigation, in Tests ein Ersatz).
/// </summary>
public interface IFavoriteGroupNavigator
{
    /// <summary>
    /// Öffnet die Gruppenansicht.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenGroupAsync(long groupId);

    /// <summary>
    /// Schließt die Gruppenansicht und kehrt zur Gruppenübersicht zurück (z. B. nach dem Löschen der Gruppe).
    /// </summary>
    /// <returns>Ein Task, der nach der Rückkehr abgeschlossen ist.</returns>
    Task CloseAsync();
}
