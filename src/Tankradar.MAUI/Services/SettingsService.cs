using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Persistiert <see cref="AppSettings"/> über Entity Framework Core in den Tabellen <c>UserSettings</c> und <c>FuelTypeSettings</c>.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IDatabaseInitializer _initializer;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="initializer">Stellt vor jedem Zugriff die Datenbankinitialisierung sicher.</param>
    public SettingsService(IDbContextFactory<TankradarDbContext> contextFactory, IDatabaseInitializer initializer)
    {
        _contextFactory = contextFactory;
        _initializer = initializer;
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var user = await context.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == UserSettingsEntity.SingletonId, cancellationToken)
            .ConfigureAwait(false);
        var fuelRows = await context.FuelTypeSettings
            .AsNoTracking()
            .OrderBy(e => e.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var defaults = AppSettings.CreateDefault();
        return new AppSettings(
            MapFuelTypes(fuelRows, defaults.FuelTypes),
            ParseOrDefault(user?.GpsUsage, defaults.GpsUsage),
            ParseOrDefault(user?.ResultView, defaults.ResultView),
            ParseOrDefault(user?.ResultSortOrder, defaults.ResultSortOrder));
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalized = settings.Normalize();
        normalized.Validate();

        await _initializer.InitializeAsync().ConfigureAwait(false);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var user = await context.UserSettings
            .FirstOrDefaultAsync(e => e.Id == UserSettingsEntity.SingletonId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            user = new UserSettingsEntity();
            context.UserSettings.Add(user);
        }

        user.Update(normalized);

        var fuelRows = await context.FuelTypeSettings.ToListAsync(cancellationToken).ConfigureAwait(false);
        for (var position = 0; position < normalized.FuelTypes.Count; position++)
        {
            var selection = normalized.FuelTypes[position];
            var key = selection.FuelType.ToString();
            var row = fuelRows.FirstOrDefault(e => e.FuelTypeKey == key);
            if (row is null)
            {
                row = new FuelTypeSettingEntity { FuelTypeKey = key };
                context.FuelTypeSettings.Add(row);
            }

            row.SortOrder = position;
            row.IsSelected = selection.IsSelected;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<FuelTypeSelection> MapFuelTypes(
        IReadOnlyList<FuelTypeSettingEntity> rows,
        IReadOnlyList<FuelTypeSelection> defaults)
    {
        var known = new List<FuelTypeSelection>();
        foreach (var row in rows)
        {
            if (Enum.TryParse<FuelType>(row.FuelTypeKey, out var fuelType)
                && Enum.IsDefined(fuelType)
                && known.All(selection => selection.FuelType != fuelType))
            {
                known.Add(new FuelTypeSelection(fuelType, row.IsSelected));
            }
        }

        if (known.Count == 0 || !known.Any(selection => selection.IsSelected))
        {
            return defaults;
        }

        return AppSettings.NormalizeFuelTypes(known);
    }

    private static TEnum ParseOrDefault<TEnum>(string? text, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(text, out var value) && Enum.IsDefined(value) ? value : fallback;
    }
}
