using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Location;

/// <summary>
/// Standortdienst für den Testmodus: liefert einen festen Standort aus der Testkonfiguration; ohne gültigen Standort ist der Standort nicht verfügbar.
/// Die echte Plattform wird nie berührt.
/// </summary>
public sealed class TestLocationService : ILocationService
{
    private readonly GeoPosition? _position;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="position">Der feste Standort oder <see langword="null"/>, wenn keiner gültig konfiguriert ist.</param>
    public TestLocationService(GeoPosition? position)
    {
        _position = position;
    }

    /// <inheritdoc />
    public Task<LocationResult> GetCurrentLocationAsync(GpsUsage gpsUsage, CancellationToken cancellationToken = default)
    {
        if (!gpsUsage.AllowsLocation())
        {
            return Task.FromResult(LocationResult.Failure(LocationStatus.DisabledBySetting));
        }

        return Task.FromResult(_position is null
            ? LocationResult.Failure(LocationStatus.Unavailable)
            : LocationResult.Success(_position));
    }
}
