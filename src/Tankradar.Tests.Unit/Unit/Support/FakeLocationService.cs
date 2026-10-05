using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Location;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="ILocationService"/> für Tests mit vorgegebenem Ergebnis; zählt die Aufrufe und hält den Standort nur für die Antwort.
/// </summary>
public sealed class FakeLocationService : ILocationService
{
    /// <summary>
    /// Das gelieferte Ergebnis (Standard: Berlin-Mitte).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public LocationResult Result { get; set; } = LocationResult.Success(new GeoPosition(StationFactory.CenterLatitude, StationFactory.CenterLongitude));

    /// <summary>
    /// Die <see cref="GpsUsage"/>-Werte aller Aufrufe in Reihenfolge.
    /// </summary>
    public List<GpsUsage> Calls { get; } = [];

    /// <summary>
    /// Wenn gesetzt, wartet der Aufruf auf diesen Task (zum Simulieren einer laufenden Abfrage).
    /// </summary>
    public Task? Gate { get; set; }

    /// <inheritdoc />
    public async Task<LocationResult> GetCurrentLocationAsync(GpsUsage gpsUsage, CancellationToken cancellationToken = default)
    {
        Calls.Add(gpsUsage);
        if (Gate is not null)
        {
            await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result;
    }
}
