using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Factory für <see cref="TankradarDbContext"/> auf einer geöffneten SQLite-In-Memory-Verbindung; alle Kontexte teilen dieselbe Datenbank.
/// </summary>
public sealed class InMemoryDbContextFactory : IDbContextFactory<TankradarDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TankradarDbContext> _options;

    /// <summary>
    /// Öffnet die In-Memory-Datenbank.
    /// </summary>
    public InMemoryDbContextFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TankradarDbContext>().UseSqlite(_connection).Options;
    }

    /// <summary>
    /// Wenn größer als 0, schlägt die nächste Kontexterstellung fehl und der Zähler sinkt um 1.
    /// </summary>
    public int FailingCreations { get; set; }

    /// <summary>
    /// Anzahl der erfolgreich erstellten Kontexte.
    /// </summary>
    public int CreatedCount { get; private set; }

    /// <inheritdoc />
    public TankradarDbContext CreateDbContext()
    {
        if (FailingCreations > 0)
        {
            FailingCreations--;
            throw new InvalidOperationException("Kontexterstellung simuliert fehlgeschlagen.");
        }

        CreatedCount++;
        return new TankradarDbContext(_options);
    }

    /// <summary>
    /// Schließt die In-Memory-Datenbank.
    /// </summary>
    public void Dispose()
    {
        _connection.Dispose();
    }
}
