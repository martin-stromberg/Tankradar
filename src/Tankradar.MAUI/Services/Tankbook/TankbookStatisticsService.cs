using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// Umsetzung von <see cref="ITankbookStatisticsService"/> auf Basis von <see cref="IVehicleService"/> und <see cref="ITankbookEntryService"/>.
/// Speicherfehler werden als Ausnahme weitergereicht.
/// </summary>
public sealed class TankbookStatisticsService : ITankbookStatisticsService
{
    private readonly IVehicleService _vehicleService;
    private readonly ITankbookEntryService _entryService;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="vehicleService">Der Fahrzeugdienst.</param>
    /// <param name="entryService">Der Dienst der Vorgänge.</param>
    /// <param name="timeProvider">Die Zeitquelle für das Referenzdatum.</param>
    public TankbookStatisticsService(IVehicleService vehicleService, ITankbookEntryService entryService, TimeProvider timeProvider)
    {
        _vehicleService = vehicleService;
        _entryService = entryService;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<TankbookStatisticsLoad> GetStatisticsAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleService.GetVehicleAsync(vehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
        {
            return new TankbookStatisticsLoad(TankbookStatisticsStatus.VehicleNotFound, null);
        }

        var overviews = await _entryService.GetEntriesAsync(vehicleId, cancellationToken).ConfigureAwait(false);
        var today = _timeProvider.GetLocalNow().DateTime;
        return new TankbookStatisticsLoad(TankbookStatisticsStatus.Ok, TankbookStatisticsCalculator.Calculate(vehicle, overviews, today));
    }
}
