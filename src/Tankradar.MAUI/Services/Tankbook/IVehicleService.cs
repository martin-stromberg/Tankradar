using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Verwaltet die Fahrzeuge des Tankbuchs. Alles wird lokal gespeichert und steht offline zur Verfügung; es gibt keine Netzwerkzugriffe.
/// </summary>
public interface IVehicleService
{
    /// <summary>
    /// Liefert alle Fahrzeuge nach Namen geordnet (de-DE, ohne Beachtung der Groß- und Kleinschreibung).
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Fahrzeuge.</returns>
    Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Liefert ein Fahrzeug.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Fahrzeug oder <see langword="null"/>, wenn es das Fahrzeug nicht gibt.</returns>
    Task<Vehicle?> GetVehicleAsync(long vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Legt ein Fahrzeug an.
    /// </summary>
    /// <param name="input">Die Angaben (Name getrimmt, eindeutig ohne Beachtung der Groß- und Kleinschreibung).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit dem angelegten Fahrzeug.</returns>
    Task<VehicleSaveResult> CreateVehicleAsync(VehicleInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ändert alle Angaben eines Fahrzeugs; der eigene Name in anderer Schreibweise ist erlaubt.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="input">Die neuen Angaben.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis der Änderung.</returns>
    Task<VehicleResult> UpdateVehicleAsync(long vehicleId, VehicleInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zählt die Tank- und Ladevorgänge eines Fahrzeugs.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Zahl der Vorgänge; 0 bei einem unbekannten Fahrzeug.</returns>
    Task<int> CountEntriesAsync(long vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht ein Fahrzeug samt aller seiner Tank- und Ladevorgänge; Vorgänge anderer Fahrzeuge bleiben unberührt.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis der Änderung.</returns>
    Task<VehicleResult> DeleteVehicleAsync(long vehicleId, CancellationToken cancellationToken = default);
}
