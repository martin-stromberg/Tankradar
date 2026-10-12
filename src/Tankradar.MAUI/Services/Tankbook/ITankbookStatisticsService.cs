using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Liefert die Auswertung der Tank- und Ladevorgänge eines Fahrzeugs. Die Werte werden beim Lesen berechnet, nichts wird gespeichert;
/// alles ist lokal und offline verfügbar.
/// </summary>
public interface ITankbookStatisticsService
{
    /// <summary>
    /// Berechnet die Auswertung eines Fahrzeugs.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Auswertung bzw. <see cref="TankbookStatisticsStatus.VehicleNotFound"/>.</returns>
    Task<TankbookStatisticsLoad> GetStatisticsAsync(long vehicleId, CancellationToken cancellationToken = default);
}
