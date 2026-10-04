using Microsoft.EntityFrameworkCore;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Stellt sicher, dass Modell und Migrationsstand nicht auseinanderlaufen.
/// </summary>
public class MigrationsTests_ModelSync : IDisposable
{
    private readonly TestDatabase _database = new();

    /// <summary>
    /// Prüft, dass das Modell keine Änderungen enthält, für die noch eine Migration fehlt.
    /// </summary>
    [Fact]
    public void Model_HasNoPendingChanges()
    {
        using var context = _database.CreateFactory().CreateDbContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    /// <summary>
    /// Räumt die Testumgebung auf.
    /// </summary>
    public void Dispose()
    {
        _database.Dispose();
        GC.SuppressFinalize(this);
    }
}
