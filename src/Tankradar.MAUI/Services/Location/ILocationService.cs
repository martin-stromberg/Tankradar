using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Location;

/// <summary>
/// Liefert den Standort des Geräts nur auf Anforderung und gemäß der Einstellung zur Standortnutzung. Positionen werden nie gespeichert.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Ermittelt den aktuellen Standort. Bei <see cref="GpsUsage.Never"/> wird weder eine Berechtigung noch ein Standort abgefragt.
    /// Fehler führen zu einem Status, nicht zu einer Ausnahme.
    /// </summary>
    /// <param name="gpsUsage">Die Einstellung zur Standortnutzung.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit Status und gegebenenfalls Position.</returns>
    Task<LocationResult> GetCurrentLocationAsync(GpsUsage gpsUsage, CancellationToken cancellationToken = default);
}
