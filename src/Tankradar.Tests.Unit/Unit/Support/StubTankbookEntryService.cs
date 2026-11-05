using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Services.Tankbook;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Dekorator für <see cref="ITankbookEntryService"/> mit einstellbarem Ergebnis des Löschens; alle übrigen Aufrufe gehen an den echten Dienst.
/// </summary>
public sealed class StubTankbookEntryService : ITankbookEntryService
{
    private readonly ITankbookEntryService _inner;

    /// <summary>
    /// Erstellt den Dekorator.
    /// </summary>
    /// <param name="inner">Der echte Dienst.</param>
    public StubTankbookEntryService(ITankbookEntryService inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Das Ergebnis, das <see cref="DeleteEntryAsync"/> liefert; <see langword="null"/> ruft den echten Dienst auf.
    /// </summary>
    public TankbookEntryResult? DeleteResult { get; set; }

    /// <summary>
    /// Die Zahl der Aufrufe von <see cref="GetEntriesAsync"/>.
    /// </summary>
    public int GetEntriesCalls { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<TankbookEntryOverview>> GetEntriesAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        GetEntriesCalls++;
        return _inner.GetEntriesAsync(vehicleId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TankbookEntrySaveResult> CreateEntryAsync(long vehicleId, TankbookEntryInput input, CancellationToken cancellationToken = default)
    {
        return _inner.CreateEntryAsync(vehicleId, input, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TankbookEntrySaveResult> UpdateEntryAsync(long entryId, TankbookEntryInput input, CancellationToken cancellationToken = default)
    {
        return _inner.UpdateEntryAsync(entryId, input, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TankbookEntryResult> DeleteEntryAsync(long entryId, CancellationToken cancellationToken = default)
    {
        return DeleteResult is { } result ? Task.FromResult(result) : _inner.DeleteEntryAsync(entryId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<KnownStation>> GetKnownStationsAsync(CancellationToken cancellationToken = default)
    {
        return _inner.GetKnownStationsAsync(cancellationToken);
    }
}
