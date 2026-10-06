using Tankradar.MAUI.Models.Favorites;

namespace Tankradar.MAUI.Services.Favorites;

/// <summary>
/// Verwaltet Favoritengruppen und die Zuordnung von Tankstellen. Alles wird lokal gespeichert und steht offline zur Verfügung.
/// Tankstellen werden ausschließlich über die Detailansicht zugeordnet (<see cref="AddStationAsync"/>, <see cref="AddStationToNewGroupAsync"/>, <see cref="RemoveStationAsync"/>).
/// </summary>
public interface IFavoritesService
{
    /// <summary>
    /// Wird nach jeder gespeicherten Änderung an Gruppen oder Zuordnungen ausgelöst (möglicherweise nicht auf dem UI-Thread), damit geöffnete Ansichten ihren Stand aktualisieren.
    /// Anmeldungen von Ansichten erfolgen über <see cref="FavoritesChangeSubscription"/>.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Liefert alle Gruppen nach Namen geordnet (ohne Beachtung der Groß- und Kleinschreibung).
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Gruppen mit der Zahl ihrer Tankstellen.</returns>
    Task<IReadOnlyList<FavoriteGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert eine Gruppe.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Gruppe oder <see langword="null"/>, wenn es sie nicht gibt.</returns>
    Task<FavoriteGroup?> GetGroupAsync(long groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legt eine Gruppe an.
    /// </summary>
    /// <param name="name">Der Name (getrimmt, 1 bis <see cref="FavoriteLimits.MaxNameLength"/> Zeichen, eindeutig).</param>
    /// <param name="description">Die optionale Beschreibung (höchstens <see cref="FavoriteLimits.MaxDescriptionLength"/> Zeichen).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit der angelegten Gruppe.</returns>
    Task<FavoriteGroupResult> CreateGroupAsync(string name, string? description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ändert Name und Beschreibung einer Gruppe.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="name">Der neue Name.</param>
    /// <param name="description">Die neue Beschreibung; <see langword="null"/> oder leer entfernt sie.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis.</returns>
    Task<FavoriteResult> UpdateGroupAsync(long groupId, string name, string? description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht eine Gruppe samt ihren Zuordnungen (die Tankstellen bleiben lokal bekannt und in anderen Gruppen erhalten).
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis.</returns>
    Task<FavoriteResult> DeleteGroupAsync(long groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die Gruppen, denen eine Tankstelle angehört, nach Namen geordnet.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Gruppen.</returns>
    Task<IReadOnlyList<FavoriteGroup>> GetGroupsOfStationAsync(string stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ordnet eine Tankstelle einer bestehenden Gruppe zu.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis.</returns>
    Task<FavoriteResult> AddStationAsync(long groupId, string stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legt eine neue Gruppe an und ordnet ihr die Tankstelle in einem Schritt zu (entweder beides oder nichts).
    /// </summary>
    /// <param name="name">Der Name der neuen Gruppe.</param>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit der angelegten Gruppe.</returns>
    Task<FavoriteGroupResult> AddStationToNewGroupAsync(string name, string stationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entfernt eine Tankstelle aus den angegebenen Gruppen.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="groupIds">Die Gruppen, aus denen sie entfernt wird.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see cref="FavoriteResult.Ok"/>, wenn sie aus mindestens einer Gruppe entfernt wurde, sonst <see cref="FavoriteResult.NotMember"/>.</returns>
    Task<FavoriteResult> RemoveStationAsync(string stationId, IReadOnlyCollection<long> groupIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die Tankstellen einer Gruppe, geordnet nach Priorität (hoch zuerst), dann nach Namen.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Einträge; leer, wenn es die Gruppe nicht gibt.</returns>
    Task<IReadOnlyList<FavoriteEntry>> GetEntriesAsync(long groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ändert Notiz und Priorität eines Eintrags.
    /// </summary>
    /// <param name="groupId">Die Kennung der Gruppe.</param>
    /// <param name="stationId">Die Kennung der Tankstelle.</param>
    /// <param name="note">Die neue Notiz; <see langword="null"/> oder leer entfernt sie.</param>
    /// <param name="priority">Die neue Priorität.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis.</returns>
    Task<FavoriteResult> UpdateEntryAsync(long groupId, string stationId, string? note, FavoritePriority priority, CancellationToken cancellationToken = default);
}
