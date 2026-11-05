using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Services.Location;

/// <summary>
/// Standortdienst auf Basis von MAUI Essentials (Berechtigung „bei Nutzung“, einmalige Positionsabfrage). Koordinaten werden weder protokolliert noch gespeichert.
/// </summary>
public sealed class MauiLocationService : ILocationService
{
    private static readonly TimeSpan PositionTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<MauiLocationService> _logger;
    private readonly Func<CancellationToken, Task<bool>> _ensurePermission;
    private readonly Func<CancellationToken, Task<GeoPosition?>> _getPosition;

    /// <summary>
    /// Erstellt den Dienst mit den Plattformfunktionen von MAUI Essentials.
    /// </summary>
    /// <param name="logger">Logger (protokolliert nur Statuswerte, nie Koordinaten).</param>
    public MauiLocationService(ILogger<MauiLocationService> logger)
        : this(logger, EnsurePermissionAsync, GetPositionAsync)
    {
    }

    /// <summary>
    /// Erstellt den Dienst mit austauschbaren Plattformfunktionen (für Tests).
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="ensurePermission">Prüft die Berechtigung und fragt sie bei Bedarf an; liefert, ob sie erteilt ist.</param>
    /// <param name="getPosition">Liefert die aktuelle Position oder <see langword="null"/>.</param>
    public MauiLocationService(
        ILogger<MauiLocationService> logger,
        Func<CancellationToken, Task<bool>> ensurePermission,
        Func<CancellationToken, Task<GeoPosition?>> getPosition)
    {
        _logger = logger;
        _ensurePermission = ensurePermission;
        _getPosition = getPosition;
    }

    /// <inheritdoc />
    public async Task<LocationResult> GetCurrentLocationAsync(GpsUsage gpsUsage, CancellationToken cancellationToken = default)
    {
        if (!gpsUsage.AllowsLocation())
        {
            return LocationResult.Failure(LocationStatus.DisabledBySetting);
        }

        try
        {
            if (!await _ensurePermission(cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Standortberechtigung nicht erteilt.");
                return LocationResult.Failure(LocationStatus.PermissionDenied);
            }

            var position = await _getPosition(cancellationToken).ConfigureAwait(false);
            return position is null
                ? LocationResult.Failure(LocationStatus.Unavailable)
                : LocationResult.Success(position);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PermissionException)
        {
            _logger.LogInformation("Standortberechtigung nicht erteilt.");
            return LocationResult.Failure(LocationStatus.PermissionDenied);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Standort nicht ermittelbar ({ExceptionType}).", ex.GetType().Name);
            return LocationResult.Failure(LocationStatus.Unavailable);
        }
    }

    private static async Task<bool> EnsurePermissionAsync(CancellationToken cancellationToken)
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
        if (status != PermissionStatus.Granted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
        }

        return status == PermissionStatus.Granted;
    }

    private static async Task<GeoPosition?> GetPositionAsync(CancellationToken cancellationToken)
    {
        var location = await Geolocation.Default
            .GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, PositionTimeout), cancellationToken)
            .ConfigureAwait(false);
        return location is null ? null : new GeoPosition(location.Latitude, location.Longitude);
    }
}
