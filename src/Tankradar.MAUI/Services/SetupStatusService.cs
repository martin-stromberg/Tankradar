using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Erkennt den ersten Start am Fehlen der Zeile <see cref="UserSettingsEntity.SingletonId"/> in der Tabelle <c>UserSettings</c>.
/// Die Zeile entsteht erst durch <see cref="ISettingsService.SaveAsync"/> (Einrichtung gespeichert oder übersprungen, Optionen geändert).
/// </summary>
public class SetupStatusService : ISetupStatusService
{
    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IDatabaseInitializer _initializer;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="initializer">Stellt vor der Abfrage die Datenbankinitialisierung sicher.</param>
    public SetupStatusService(IDbContextFactory<TankradarDbContext> contextFactory, IDatabaseInitializer initializer)
    {
        _contextFactory = contextFactory;
        _initializer = initializer;
    }

    /// <inheritdoc />
    public async Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default)
    {
        await _initializer.InitializeAsync().ConfigureAwait(false);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var exists = await context.UserSettings
            .AsNoTracking()
            .AnyAsync(e => e.Id == UserSettingsEntity.SingletonId, cancellationToken)
            .ConfigureAwait(false);
        return !exists;
    }
}
