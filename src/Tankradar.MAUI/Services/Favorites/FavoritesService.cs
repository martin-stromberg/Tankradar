using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.MAUI.Services.Favorites;

/// <summary>
/// EF-Core-Umsetzung von <see cref="IFavoritesService"/> auf der lokalen SQLite-Datenbank (Tabellen <c>FavoriteGroups</c> und <c>FavoriteEntries</c>).
/// </summary>
public sealed class FavoritesService : IFavoritesService
{
    private const int SqliteUniqueConstraintFailed = 2067;

    private static readonly StringComparer GermanOrder = StringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: true);

    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IDatabaseInitializer _initializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="initializer">Stellt vor jedem Zugriff die Datenbankinitialisierung sicher.</param>
    /// <param name="timeProvider">Die Zeitquelle für die Zeitstempel.</param>
    public FavoritesService(IDbContextFactory<TankradarDbContext> contextFactory, IDatabaseInitializer initializer, TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _initializer = initializer;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteGroup>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var groups = await LoadGroupsAsync(context, null, cancellationToken).ConfigureAwait(false);
        return groups;
    }

    /// <inheritdoc />
    public async Task<FavoriteGroup?> GetGroupAsync(long groupId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var groups = await LoadGroupsAsync(context, query => query.Where(g => g.Id == groupId), cancellationToken).ConfigureAwait(false);
        return groups.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<FavoriteGroupResult> CreateGroupAsync(string name, string? description, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await CreateAsync(context, name, description, stationId: null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteResult> UpdateGroupAsync(long groupId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var validation = Validate(name, description, out var trimmedName, out var trimmedDescription);
        if (validation != FavoriteResult.Ok)
        {
            return validation;
        }

        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var group = await context.FavoriteGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken).ConfigureAwait(false);
        if (group is null)
        {
            return FavoriteResult.GroupNotFound;
        }

        if (await NameTakenAsync(context, trimmedName, groupId, cancellationToken).ConfigureAwait(false))
        {
            return FavoriteResult.DuplicateName;
        }

        group.Name = trimmedName;
        group.Description = trimmedDescription;
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteResult> DeleteGroupAsync(long groupId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var group = await context.FavoriteGroups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken).ConfigureAwait(false);
        if (group is null)
        {
            return FavoriteResult.GroupNotFound;
        }

        context.FavoriteGroups.Remove(group);
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteGroup>> GetGroupsOfStationAsync(string stationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await LoadGroupsAsync(
            context,
            query => query.Where(g => g.Entries.Any(e => e.StationId == stationId)),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteResult> AddStationAsync(long groupId, string stationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        if (!await context.Stations.AnyAsync(s => s.Id == stationId, cancellationToken).ConfigureAwait(false))
        {
            return FavoriteResult.StationUnknown;
        }

        if (!await context.FavoriteGroups.AnyAsync(g => g.Id == groupId, cancellationToken).ConfigureAwait(false))
        {
            return FavoriteResult.GroupNotFound;
        }

        if (await context.FavoriteEntries.AnyAsync(e => e.GroupId == groupId && e.StationId == stationId, cancellationToken).ConfigureAwait(false))
        {
            return FavoriteResult.AlreadyMember;
        }

        context.FavoriteEntries.Add(NewEntry(groupId, stationId));
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteGroupResult> AddStationToNewGroupAsync(string name, string stationId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        return await CreateAsync(context, name, null, stationId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteResult> RemoveStationAsync(string stationId, IReadOnlyCollection<long> groupIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        ArgumentNullException.ThrowIfNull(groupIds);
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var ids = groupIds.ToList();
        var entries = await context.FavoriteEntries
            .Where(e => e.StationId == stationId && ids.Contains(e.GroupId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (entries.Count == 0)
        {
            return FavoriteResult.NotMember;
        }

        context.FavoriteEntries.RemoveRange(entries);
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteEntry>> GetEntriesAsync(long groupId, CancellationToken cancellationToken = default)
    {
        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var rows = await context.FavoriteEntries.AsNoTracking()
            .Where(e => e.GroupId == groupId)
            .Select(e => new
            {
                e.Id,
                e.GroupId,
                e.StationId,
                e.Note,
                e.Priority,
                e.Station!.Name,
                e.Station.Street,
                e.Station.HouseNumber,
                e.Station.PostCode,
                e.Station.Place,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => (Row: row, Priority: ParsePriority(row.Priority)))
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.Row.Name, GermanOrder)
            .ThenBy(item => item.Row.Id)
            .Select(item => new FavoriteEntry(
                item.Row.GroupId,
                item.Row.StationId,
                item.Row.Name,
                SearchTexts.FormatAddress(item.Row.Street, item.Row.HouseNumber, item.Row.PostCode, item.Row.Place),
                item.Row.Note,
                item.Priority))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<FavoriteResult> UpdateEntryAsync(long groupId, string stationId, string? note, FavoritePriority priority, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stationId);
        var trimmedNote = Normalize(note);
        if (trimmedNote is { Length: > FavoriteLimits.MaxNoteLength })
        {
            return FavoriteResult.InvalidText;
        }

        await using var context = await OpenAsync(cancellationToken).ConfigureAwait(false);
        var entry = await context.FavoriteEntries
            .FirstOrDefaultAsync(e => e.GroupId == groupId && e.StationId == stationId, cancellationToken)
            .ConfigureAwait(false);
        if (entry is null)
        {
            return FavoriteResult.NotMember;
        }

        entry.Note = trimmedNote;
        entry.Priority = priority.ToString();
        return await SaveAsync(context, cancellationToken).ConfigureAwait(false);
    }

    private static string? Normalize(string? text)
    {
        var trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static FavoritePriority ParsePriority(string value)
    {
        return Enum.TryParse(value, out FavoritePriority priority) && Enum.IsDefined(priority) ? priority : FavoritePriority.None;
    }

    private static FavoriteResult Validate(string? name, string? description, out string trimmedName, out string? trimmedDescription)
    {
        trimmedName = name?.Trim() ?? string.Empty;
        trimmedDescription = Normalize(description);
        if (trimmedName.Length == 0 || trimmedName.Length > FavoriteLimits.MaxNameLength)
        {
            return FavoriteResult.InvalidName;
        }

        return trimmedDescription is { Length: > FavoriteLimits.MaxDescriptionLength } ? FavoriteResult.InvalidText : FavoriteResult.Ok;
    }

    private static async Task<bool> NameTakenAsync(TankradarDbContext context, string name, long? exceptGroupId, CancellationToken cancellationToken)
    {
        // Der Vergleich ohne Beachtung der Groß- und Kleinschreibung erfolgt im Speicher (SQLite kennt dafür nur ASCII); die Namen sind wenige und kurz.
        var names = await context.FavoriteGroups.AsNoTracking()
            .Where(g => exceptGroupId == null || g.Id != exceptGroupId)
            .Select(g => g.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<IReadOnlyList<FavoriteGroup>> LoadGroupsAsync(
        TankradarDbContext context,
        Func<IQueryable<FavoriteGroupEntity>, IQueryable<FavoriteGroupEntity>>? filter,
        CancellationToken cancellationToken)
    {
        IQueryable<FavoriteGroupEntity> query = context.FavoriteGroups.AsNoTracking();
        if (filter is not null)
        {
            query = filter(query);
        }

        var rows = await query
            .Select(g => new { g.Id, g.Name, g.Description, Count = g.Entries.Count })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows
            .OrderBy(row => row.Name, GermanOrder)
            .ThenBy(row => row.Id)
            .Select(row => new FavoriteGroup(row.Id, row.Name, row.Description, row.Count))
            .ToList();
    }

    private async Task<FavoriteResult> SaveAsync(TankradarDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintFailed })
        {
            // Gleichzeitiges Anlegen desselben Namens oder derselben Zuordnung aus einer anderen Ansicht; andere Fehler (gesperrte Datenbank, voller Datenträger) werden weitergereicht und als Speicherfehler gemeldet.
            return FavoriteResult.DuplicateName;
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return FavoriteResult.Ok;
    }

    private async Task<TankradarDbContext> OpenAsync(CancellationToken cancellationToken)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);
        return await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    }

    private FavoriteEntryEntity NewEntry(long groupId, string stationId)
    {
        return new FavoriteEntryEntity
        {
            GroupId = groupId,
            StationId = stationId,
            Priority = FavoritePriority.None.ToString(),
            AddedUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };
    }

    private async Task<FavoriteGroupResult> CreateAsync(TankradarDbContext context, string name, string? description, string? stationId, CancellationToken cancellationToken)
    {
        var validation = Validate(name, description, out var trimmedName, out var trimmedDescription);
        if (validation != FavoriteResult.Ok)
        {
            return new FavoriteGroupResult(validation, null);
        }

        if (stationId is not null && !await context.Stations.AnyAsync(s => s.Id == stationId, cancellationToken).ConfigureAwait(false))
        {
            return new FavoriteGroupResult(FavoriteResult.StationUnknown, null);
        }

        if (await NameTakenAsync(context, trimmedName, null, cancellationToken).ConfigureAwait(false))
        {
            return new FavoriteGroupResult(FavoriteResult.DuplicateName, null);
        }

        var group = new FavoriteGroupEntity
        {
            Name = trimmedName,
            Description = trimmedDescription,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
        };
        if (stationId is not null)
        {
            group.Entries.Add(NewEntry(0, stationId));
        }

        context.FavoriteGroups.Add(group);
        var saved = await SaveAsync(context, cancellationToken).ConfigureAwait(false);
        return saved == FavoriteResult.Ok
            ? new FavoriteGroupResult(FavoriteResult.Ok, new FavoriteGroup(group.Id, group.Name, group.Description, group.Entries.Count))
            : new FavoriteGroupResult(saved, null);
    }
}
