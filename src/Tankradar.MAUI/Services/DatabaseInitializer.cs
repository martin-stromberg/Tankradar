using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;

namespace Tankradar.MAUI.Services;

/// <summary>
/// Legt das Datenverzeichnis an, führt ausstehende Migrationen aus und setzt den Dateischutz. Das Ergebnis wird zwischengespeichert; nach einem Fehler ist ein neuer Versuch möglich.
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<TankradarDbContext> _contextFactory;
    private readonly IAppDataPathProvider _pathProvider;
    private readonly IDatabaseFileProtector _fileProtector;
    private readonly object _gate = new();
    private Task? _initialization;

    /// <summary>
    /// Erstellt den Initializer.
    /// </summary>
    /// <param name="contextFactory">Factory für Datenbankkontexte.</param>
    /// <param name="pathProvider">Liefert das App-Datenverzeichnis.</param>
    /// <param name="fileProtector">Setzt den Dateischutz nach der Migration.</param>
    public DatabaseInitializer(
        IDbContextFactory<TankradarDbContext> contextFactory,
        IAppDataPathProvider pathProvider,
        IDatabaseFileProtector fileProtector)
    {
        _contextFactory = contextFactory;
        _pathProvider = pathProvider;
        _fileProtector = fileProtector;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        lock (_gate)
        {
            _initialization ??= RunAsync();
            return _initialization;
        }
    }

    private async Task RunAsync()
    {
        // Stellt sicher, dass _initialization zugewiesen ist, bevor ein Fehler es wieder zurücksetzt.
        await Task.Yield();

        try
        {
            var dataDirectory = _pathProvider.GetDataDirectory();
            Directory.CreateDirectory(dataDirectory);

            await using (var context = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false))
            {
                await context.Database.MigrateAsync().ConfigureAwait(false);
            }

            _fileProtector.Protect(TankradarDbContext.GetDatabasePath(dataDirectory));
        }
        catch
        {
            lock (_gate)
            {
                _initialization = null;
            }

            throw;
        }
    }
}
