using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// EF-Core-Umsetzung von <see cref="ITankbookEntryService"/> auf der lokalen SQLite-Datenbank (Tabelle <c>TankbookEntries</c>).
/// Fachliche Fehler werden als <see cref="TankbookEntryResult"/> gemeldet; protokolliert werden nie Namen oder Werte, nur Ausnahmetypen.
/// </summary>
public sealed class TankbookEntryService : ITankbookEntryService
{
    private static readonly StringComparer GermanOrder = StringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: true);

    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IDatabaseInitializer _initializer;
    private readonly IReceiptPhotoStore _photoStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TankbookEntryService> _logger;
    private int _photosCleanedUp;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="initializer">Stellt vor jedem Zugriff die Datenbankinitialisierung sicher.</param>
    /// <param name="photoStore">Die Ablage der Belegfotos.</param>
    /// <param name="timeProvider">Die Zeitquelle für Anlagezeitpunkt und Zukunftsprüfung.</param>
    /// <param name="logger">Der Logger.</param>
    public TankbookEntryService(
        IDbContextFactory<TankradarDbContext> contextFactory,
        IDatabaseInitializer initializer,
        IReceiptPhotoStore photoStore,
        TimeProvider timeProvider,
        ILogger<TankbookEntryService> logger)
    {
        _contextFactory = contextFactory;
        _initializer = initializer;
        _photoStore = photoStore;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TankbookEntryOverview>> GetEntriesAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        if (Interlocked.Exchange(ref _photosCleanedUp, 1) == 0)
        {
            await DeleteUnreferencedPhotosAsync(context, cancellationToken).ConfigureAwait(false);
        }

        var entries = await LoadEntriesAsync(context, vehicleId, null, cancellationToken).ConfigureAwait(false);
        var vehicle = await context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
        {
            return ConsumptionCalculator.Calculate(entries).Reverse().ToList();
        }

        // Der Start-Kilometerstand ist die Basis des ersten vollen Vorgangs der Startart (Elektrofahrzeug: Strom, sonst Kraftstoff).
        var startKind = ParseFuelKind(vehicle.FuelKind) == VehicleFuelKind.Electric ? TankbookEntryKind.Electricity : TankbookEntryKind.Fuel;
        return ConsumptionCalculator.Calculate(entries, vehicle.StartOdometerKm, startKind).Reverse().ToList();
    }

    /// <inheritdoc />
    public async Task<TankbookEntrySaveResult> CreateEntryAsync(long vehicleId, TankbookEntryInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var validation = TankbookEntryValidator.Validate(input, Now());
        if (validation != TankbookEntryResult.Ok)
        {
            return new TankbookEntrySaveResult(validation, null, null);
        }

        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var vehicle = await context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.VehicleNotFound, null, null);
        }

        if (!TankbookEntryValidator.IsKindAllowed(ParseFuelKind(vehicle.FuelKind), input.Kind))
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.KindNotAllowed, null, null);
        }

        var others = await LoadEntriesAsync(context, vehicleId, null, cancellationToken).ConfigureAwait(false);
        if (CheckOdometer(vehicle.StartOdometerKm, others, ToCandidate(long.MaxValue, vehicleId, input)) is { } odometerError)
        {
            return odometerError;
        }

        var entity = new TankbookEntryEntity { VehicleId = vehicleId, CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime };
        entity.Apply(input);
        context.TankbookEntries.Add(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new TankbookEntrySaveResult(TankbookEntryResult.Ok, ToEntry(entity), null);
    }

    /// <inheritdoc />
    public async Task<TankbookEntrySaveResult> UpdateEntryAsync(long entryId, TankbookEntryInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var validation = TankbookEntryValidator.Validate(input, Now());
        if (validation != TankbookEntryResult.Ok)
        {
            return new TankbookEntrySaveResult(validation, null, null);
        }

        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var entity = await context.TankbookEntries.FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.EntryNotFound, null, null);
        }

        var vehicle = await context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == entity.VehicleId, cancellationToken).ConfigureAwait(false);
        if (vehicle is null)
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.VehicleNotFound, null, null);
        }

        // Eine gespeicherte Art bleibt erlaubt, auch wenn sie nach einem Wechsel der Kraftstoffart des Fahrzeugs nicht mehr passt.
        if (input.Kind != ParseKind(entity.Kind) && !TankbookEntryValidator.IsKindAllowed(ParseFuelKind(vehicle.FuelKind), input.Kind))
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.KindNotAllowed, null, null);
        }

        var others = await LoadEntriesAsync(context, entity.VehicleId, entryId, cancellationToken).ConfigureAwait(false);
        if (CheckOdometer(vehicle.StartOdometerKm, others, ToCandidate(entryId, entity.VehicleId, input)) is { } odometerError)
        {
            return odometerError;
        }

        var previousPhoto = entity.ReceiptPhotoFileName;
        entity.Apply(input);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (previousPhoto is not null && !string.Equals(previousPhoto, entity.ReceiptPhotoFileName, StringComparison.Ordinal))
        {
            _photoStore.Delete(previousPhoto);
        }

        return new TankbookEntrySaveResult(TankbookEntryResult.Ok, ToEntry(entity), null);
    }

    /// <inheritdoc />
    public async Task<TankbookEntryResult> DeleteEntryAsync(long entryId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var entity = await context.TankbookEntries.FirstOrDefaultAsync(e => e.Id == entryId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return TankbookEntryResult.EntryNotFound;
        }

        var photo = entity.ReceiptPhotoFileName;
        context.TankbookEntries.Remove(entity);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (photo is not null)
        {
            _photoStore.Delete(photo);
        }

        return TankbookEntryResult.Ok;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KnownStation>> GetKnownStationsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var favoriteRows = await context.FavoriteEntries.AsNoTracking()
            .Where(f => f.Station != null)
            .Select(f => new { f.StationId, f.Station!.Name, f.Station.Brand, f.Station.CustomName, f.Station.Place, f.Station.Street, f.Station.HouseNumber })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var favorites = favoriteRows
            .GroupBy(row => row.StationId, StringComparer.Ordinal)
            .Select(group => group.First())
            .Select(row => (Row: row, Display: StationNaming.GetPrimary(row.CustomName, row.Brand, row.Name)))
            .Where(item => item.Display.Length > 0)
            .OrderBy(item => item.Display, GermanOrder)
            .ThenBy(item => item.Row.Place ?? string.Empty, GermanOrder)
            .Select(item => new KnownStation(item.Row.StationId, item.Display, string.IsNullOrWhiteSpace(item.Row.Place) ? null : item.Row.Place.Trim(), GetDetail(item.Row.CustomName, item.Row.Brand, item.Row.Name, item.Row.Street, item.Row.HouseNumber), GetAddress(item.Row.Street, item.Row.HouseNumber)))
            .ToList();

        var favoriteNames = new HashSet<string>(favorites.Select(station => station.Name), StringComparer.OrdinalIgnoreCase);
        var usedNames = await context.TankbookEntries.AsNoTracking()
            .Where(e => e.StationId == null && e.StationName != string.Empty)
            .Select(e => e.StationName)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var used = usedNames
            .Select(name => name.Trim())
            .Where(name => name.Length > 0 && !favoriteNames.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, GermanOrder)
            .Select(name => new KnownStation(null, name, null));

        return favorites.Concat(used).Take(TankbookEntryLimits.MaxKnownStations).ToList();
    }

    private static string? GetDetail(string? customName, string? brand, string name, string? street, string? houseNumber)
    {
        var subName = StationNaming.GetSecondary(customName, brand, name);
        if (subName.Length > 0)
        {
            return subName;
        }

        return GetAddress(street, houseNumber);
    }

    private static string? GetAddress(string? street, string? houseNumber)
    {
        var address = $"{street?.Trim()} {houseNumber?.Trim()}".Trim();
        return address.Length > 0 ? address : null;
    }

    private static TankbookEntrySaveResult? CheckOdometer(int vehicleStartKm, IReadOnlyList<TankbookEntry> others, TankbookEntry candidate)
    {
        if (candidate.OdometerKm < vehicleStartKm)
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.OdometerBelowVehicleStart, null, vehicleStartKm);
        }

        var ordered = others.OrderBy(entry => entry, TankbookEntryOrder.Ascending).ToList();
        var previous = ordered.LastOrDefault(entry => TankbookEntryOrder.Compare(entry, candidate) < 0);
        if (previous is not null && candidate.OdometerKm < previous.OdometerKm)
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.OdometerBelowPrevious, null, previous.OdometerKm);
        }

        var next = ordered.FirstOrDefault(entry => TankbookEntryOrder.Compare(entry, candidate) > 0);
        if (next is not null && candidate.OdometerKm > next.OdometerKm)
        {
            return new TankbookEntrySaveResult(TankbookEntryResult.OdometerAboveNext, null, next.OdometerKm);
        }

        return null;
    }

    private static TankbookEntry ToCandidate(long id, long vehicleId, TankbookEntryInput input)
    {
        return new TankbookEntry(
            id,
            vehicleId,
            input.OccurredAt,
            input.Kind,
            input.Quantity,
            input.UnitPrice,
            input.TotalAmount,
            input.OdometerKm,
            input.IsPartial,
            input.StationName,
            input.StationId,
            input.Note,
            input.ReceiptPhotoFileName);
    }

    private static VehicleFuelKind ParseFuelKind(string value)
    {
        VehicleValidator.TryParseFuelKind(value, out var fuelKind);
        return fuelKind;
    }

    private async Task<List<TankbookEntry>> LoadEntriesAsync(TankradarDbContext context, long vehicleId, long? exceptEntryId, CancellationToken cancellationToken)
    {
        var rows = await context.TankbookEntries.AsNoTracking()
            .Where(e => e.VehicleId == vehicleId && (exceptEntryId == null || e.Id != exceptEntryId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ToEntry).ToList();
    }

    private async Task DeleteUnreferencedPhotosAsync(TankradarDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var referenced = await context.TankbookEntries.AsNoTracking()
                .Where(e => e.ReceiptPhotoFileName != null)
                .Select(e => e.ReceiptPhotoFileName!)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            _photoStore.DeleteUnreferenced(new HashSet<string>(referenced, StringComparer.Ordinal));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning("Nicht mehr verwendete Belegfotos konnten nicht bereinigt werden ({ExceptionType}).", ex.GetType().Name);
        }
    }

    private DateTime Now()
    {
        return _timeProvider.GetLocalNow().DateTime;
    }

    private async Task<TankradarDbContext> OpenAsync(CancellationToken cancellationToken)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);
        return await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    }

    private TankbookEntryKind ParseKind(string value)
    {
        if (Enum.TryParse(value, out TankbookEntryKind kind) && Enum.IsDefined(kind))
        {
            return kind;
        }

        _logger.LogWarning("Unbekannte Art eines gespeicherten Vorgangs; es wird Kraftstoff angenommen.");
        return TankbookEntryKind.Fuel;
    }

    private TankbookEntry ToEntry(TankbookEntryEntity entity)
    {
        return new TankbookEntry(
            entity.Id,
            entity.VehicleId,
            entity.OccurredAt,
            ParseKind(entity.Kind),
            entity.Quantity,
            entity.UnitPrice,
            entity.TotalAmount,
            entity.OdometerKm,
            entity.IsPartial,
            entity.StationName,
            entity.StationId,
            entity.Note,
            entity.ReceiptPhotoFileName);
    }
}
