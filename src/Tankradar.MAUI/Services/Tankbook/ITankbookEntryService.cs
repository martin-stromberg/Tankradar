using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Verwaltet die Tank- und Ladevorgänge der Fahrzeuge. Alles wird lokal gespeichert und steht offline zur Verfügung; es gibt keine Netzwerkzugriffe.
/// </summary>
public interface ITankbookEntryService
{
    /// <summary>
    /// Liefert die Vorgänge eines Fahrzeugs mit Strecke und Verbrauch, neueste zuerst. Beim ersten Aufruf je Prozess werden nicht mehr verwendete Belegfotos entfernt.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Vorgänge; leer bei einem unbekannten Fahrzeug.</returns>
    Task<IReadOnlyList<TankbookEntryOverview>> GetEntriesAsync(long vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert einen Vorgang.
    /// </summary>
    /// <param name="entryId">Die Kennung des Vorgangs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Der Vorgang oder <see langword="null"/>, wenn es ihn nicht gibt.</returns>
    Task<TankbookEntry?> GetEntryAsync(long entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legt einen Vorgang an. Geprüft werden die Eingabe, die Art je Kraftstoffart und der Kilometerstand gegenüber Fahrzeug, Vorgänger und Nachfolger.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="input">Die Angaben.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit dem angelegten Vorgang bzw. dem verletzten Kilometer-Grenzwert.</returns>
    Task<TankbookEntrySaveResult> CreateEntryAsync(long vehicleId, TankbookEntryInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ändert alle Angaben eines Vorgangs; die gespeicherte Art bleibt erlaubt. Ein ersetztes Belegfoto wird nach dem Speichern gelöscht.
    /// </summary>
    /// <param name="entryId">Die Kennung des Vorgangs.</param>
    /// <param name="input">Die neuen Angaben.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit dem geänderten Vorgang bzw. dem verletzten Kilometer-Grenzwert.</returns>
    Task<TankbookEntrySaveResult> UpdateEntryAsync(long entryId, TankbookEntryInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht einen Vorgang samt Belegfoto.
    /// </summary>
    /// <param name="entryId">Die Kennung des Vorgangs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis der Änderung.</returns>
    Task<TankbookEntryResult> DeleteEntryAsync(long entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert die bekannten Tankstellen: zuerst die Favoriten-Tankstellen, danach bereits verwendete, frei eingegebene Namen
    /// (jeweils nach Namen geordnet, ohne Duplikate ohne Beachtung der Groß- und Kleinschreibung), höchstens <see cref="TankbookEntryLimits.MaxKnownStations"/>.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die bekannten Tankstellen.</returns>
    Task<IReadOnlyList<KnownStation>> GetKnownStationsAsync(CancellationToken cancellationToken = default);
}
