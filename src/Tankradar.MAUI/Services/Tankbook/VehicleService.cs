using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Tankbook;

/// <summary>
/// EF-Core-Umsetzung von <see cref="IVehicleService"/> auf der lokalen SQLite-Datenbank (Tabellen <c>Vehicles</c> und <c>TankbookEntries</c>); beim Löschen eines Fahrzeugs werden auch die Belegfotos seiner Vorgänge entfernt.
/// Fachliche Fehler werden als <see cref="VehicleResult"/> gemeldet; protokolliert werden nie Namen oder Werte.
/// </summary>
public sealed class VehicleService : IVehicleService
{
    private const int SqliteUniqueConstraintFailed = 2067;

    private static readonly StringComparer GermanOrder = StringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: true);

    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IDatabaseInitializer _initializer;
    private readonly IReceiptPhotoStore _photoStore;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VehicleService> _logger;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="initializer">Stellt vor jedem Zugriff die Datenbankinitialisierung sicher.</param>
    /// <param name="photoStore">Die Ablage der Belegfotos; die Fotos mitgelöschter Vorgänge werden nach dem Löschen entfernt.</param>
    /// <param name="timeProvider">Die Zeitquelle für die Zeitstempel.</param>
    /// <param name="logger">Der Logger.</param>
    public VehicleService(
        IDbContextFactory<TankradarDbContext> contextFactory,
        IDatabaseInitializer initializer,
        IReceiptPhotoStore photoStore,
        TimeProvider timeProvider,
        ILogger<VehicleService> logger)
    {
        _contextFactory = contextFactory;
        _initializer = initializer;
        _photoStore = photoStore;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await context.Vehicles.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows
            .OrderBy(row => row.Name, GermanOrder)
            .ThenBy(row => row.Id)
            .Select(ToVehicle)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Vehicle?> GetVehicleAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var row = await context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken).ConfigureAwait(false);
        return row is null ? null : ToVehicle(row);
    }

    /// <inheritdoc />
    public async Task<VehicleSaveResult> CreateVehicleAsync(VehicleInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var validation = VehicleValidator.Validate(input);
        if (validation != VehicleResult.Ok)
        {
            return new VehicleSaveResult(validation, null);
        }

        var name = input.Name.Trim();
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        if (await NameTakenAsync(context, name, null, cancellationToken).ConfigureAwait(false))
        {
            return new VehicleSaveResult(VehicleResult.DuplicateName, null);
        }

        var entity = new VehicleEntity
        {
            Name = name,
            FuelKind = input.FuelKind.ToString(),
            StartOdometerKm = input.StartOdometerKm,
            StartConsumption = input.StartConsumption,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };
        context.Vehicles.Add(entity);
        var saved = await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        return saved == VehicleResult.Ok
            ? new VehicleSaveResult(VehicleResult.Ok, ToVehicle(entity))
            : new VehicleSaveResult(saved, null);
    }

    /// <inheritdoc />
    public async Task<VehicleResult> UpdateVehicleAsync(long vehicleId, VehicleInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var validation = VehicleValidator.Validate(input);
        if (validation != VehicleResult.Ok)
        {
            return validation;
        }

        var name = input.Name.Trim();
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var entity = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return VehicleResult.VehicleNotFound;
        }

        if (await NameTakenAsync(context, name, vehicleId, cancellationToken).ConfigureAwait(false))
        {
            return VehicleResult.DuplicateName;
        }

        // Der Start-Kilometerstand darf nicht über dem niedrigsten Kilometerstand der vorhandenen Vorgänge liegen.
        var lowestEntryKm = await context.TankbookEntries.AsNoTracking()
            .Where(e => e.VehicleId == vehicleId)
            .Select(e => (int?)e.OdometerKm)
            .MinAsync(cancellationToken)
            .ConfigureAwait(false);
        if (lowestEntryKm is { } lowest && input.StartOdometerKm > lowest)
        {
            return VehicleResult.StartOdometerAboveEntries;
        }

        entity.Name = name;
        entity.FuelKind = input.FuelKind.ToString();
        entity.StartOdometerKm = input.StartOdometerKm;
        entity.StartConsumption = input.StartConsumption;
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountEntriesAsync(long vehicleId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await context.TankbookEntries.CountAsync(e => e.VehicleId == vehicleId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<VehicleDeleteResult> DeleteVehicleAsync(long vehicleId, int expectedEntryCount, CancellationToken cancellationToken = default)
    {
        List<string> photos;
        await using (var context = await OpenAsync(cancellationToken).ConfigureAwait(false))
        {
            // Zählen und Löschen in einer Transaktion: Ein inzwischen hinzugekommener Vorgang wird nicht stillschweigend mitgelöscht.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var entity = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return new VehicleDeleteResult(VehicleResult.VehicleNotFound, 0);
            }

            var entryCount = await context.TankbookEntries.CountAsync(e => e.VehicleId == vehicleId, cancellationToken).ConfigureAwait(false);
            if (entryCount != expectedEntryCount)
            {
                return new VehicleDeleteResult(VehicleResult.EntryCountChanged, entryCount);
            }

            photos = await context.TankbookEntries.AsNoTracking()
                .Where(e => e.VehicleId == vehicleId && e.ReceiptPhotoFileName != null)
                .Select(e => e.ReceiptPhotoFileName!)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Die Vorgänge des Fahrzeugs löscht die Datenbank per ON DELETE CASCADE.
            context.Vehicles.Remove(entity);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var photo in photos)
        {
            try
            {
                _photoStore.Delete(photo);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("Ein Belegfoto konnte nicht gelöscht werden ({ExceptionType}).", ex.GetType().Name);
            }
        }

        return new VehicleDeleteResult(VehicleResult.Ok, expectedEntryCount);
    }

    private static async Task<bool> NameTakenAsync(TankradarDbContext context, string name, long? exceptVehicleId, CancellationToken cancellationToken)
    {
        // Der Vergleich ohne Beachtung der Groß- und Kleinschreibung erfolgt im Speicher (SQLite kennt dafür nur ASCII); die Namen sind wenige und kurz.
        var names = await context.Vehicles.AsNoTracking()
            .Where(v => exceptVehicleId == null || v.Id != exceptVehicleId)
            .Select(v => v.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<VehicleResult> SaveAsync(TankradarDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintFailed })
        {
            // Gleichzeitiges Speichern desselben Namens; andere Fehler werden weitergereicht und als Speicherfehler gemeldet.
            return VehicleResult.DuplicateName;
        }

        return VehicleResult.Ok;
    }

    private async Task<TankradarDbContext> OpenAsync(CancellationToken cancellationToken)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);
        return await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    }

    private Vehicle ToVehicle(VehicleEntity entity)
    {
        if (!Enum.TryParse(entity.FuelKind, out VehicleFuelKind fuelKind) || !Enum.IsDefined(fuelKind))
        {
            _logger.LogWarning("Unbekannte Kraftstoffart eines gespeicherten Fahrzeugs; es wird Benzin angenommen.");
            fuelKind = VehicleFuelKind.Petrol;
        }

        return new Vehicle(entity.Id, entity.Name, fuelKind, entity.StartOdometerKm, entity.StartConsumption);
    }
}
