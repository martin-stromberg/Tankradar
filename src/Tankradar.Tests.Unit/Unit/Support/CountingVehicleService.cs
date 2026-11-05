using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Dekorator für <see cref="IVehicleService"/>, der Aufrufe von <see cref="GetVehiclesAsync"/> zählt und alle übrigen Methoden weiterreicht.
/// </summary>
public sealed class CountingVehicleService : IVehicleService
{
    private readonly IVehicleService _inner;

    /// <summary>
    /// Erstellt den Dekorator.
    /// </summary>
    /// <param name="inner">Der echte Fahrzeugdienst.</param>
    public CountingVehicleService(IVehicleService inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Die Zahl der Aufrufe von <see cref="GetVehiclesAsync"/>.
    /// </summary>
    public int GetVehiclesCalls { get; private set; }

    /// <summary>
    /// Wird gesetzt, wenn <see cref="GetVehiclesAsync"/> erreicht wurde.
    /// </summary>
    public ManualResetEventSlim? GetVehiclesCalled { get; set; }

    /// <summary>
    /// Blockiert <see cref="GetVehiclesAsync"/> synchron, wenn es gesetzt ist.
    /// </summary>
    public ManualResetEventSlim? ContinueGetVehicles { get; set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
    {
        GetVehiclesCalls++;
        GetVehiclesCalled?.Set();
        ContinueGetVehicles?.Wait(cancellationToken);
        return _inner.GetVehiclesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Vehicle?> GetVehicleAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        return _inner.GetVehicleAsync(vehicleId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VehicleSaveResult> CreateVehicleAsync(VehicleInput input, CancellationToken cancellationToken = default)
    {
        return _inner.CreateVehicleAsync(input, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VehicleResult> UpdateVehicleAsync(long vehicleId, VehicleInput input, CancellationToken cancellationToken = default)
    {
        return _inner.UpdateVehicleAsync(vehicleId, input, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountEntriesAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        return _inner.CountEntriesAsync(vehicleId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<VehicleDeleteResult> DeleteVehicleAsync(long vehicleId, int expectedEntryCount, CancellationToken cancellationToken = default)
    {
        return _inner.DeleteVehicleAsync(vehicleId, expectedEntryCount, cancellationToken);
    }
}
