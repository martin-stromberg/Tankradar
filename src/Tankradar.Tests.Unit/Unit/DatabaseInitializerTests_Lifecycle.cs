using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Task-Caching, Wiederholung nach Fehlern und Reihenfolge von Migration und Dateischutz im <see cref="DatabaseInitializer"/>.
/// </summary>
public class DatabaseInitializerTests_Lifecycle : BaseTest
{
    private readonly InMemoryDbContextFactory _factory = new();
    private readonly FakeDatabaseFileProtector _protector = new();
    private readonly DatabaseInitializer _initializer;

    /// <summary>
    /// Erstellt den Initializer mit Fake-Abhängigkeiten.
    /// </summary>
    public DatabaseInitializerTests_Lifecycle()
    {
        _initializer = new DatabaseInitializer(_factory, new FakeAppDataPathProvider(Path.GetTempPath()), _protector);
    }

    /// <summary>
    /// Prüft, dass mehrere Aufrufe nur einen Lauf auslösen.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_RunsOnce()
    {
        await _initializer.InitializeAsync();
        await _initializer.InitializeAsync();
        await Task.WhenAll(_initializer.InitializeAsync(), _initializer.InitializeAsync());

        Assert.Equal(1, _factory.CreatedCount);
        Assert.Single(_protector.ProtectedPaths);
    }

    /// <summary>
    /// Prüft, dass nach einem Fehler ein erneuter Aufruf den Lauf wiederholt.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_AfterFailure_Retries()
    {
        _factory.FailingCreations = 1;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _initializer.InitializeAsync());
        await _initializer.InitializeAsync();

        Assert.Single(_protector.ProtectedPaths);
    }

    /// <summary>
    /// Prüft, dass der Dateischutz erst nach abgeschlossener Migration gesetzt wird und auf den Datenbankpfad zeigt.
    /// </summary>
    [Fact]
    public async Task Protect_IsCalledAfterMigration()
    {
        var appliedAtProtect = -1;
        _protector.OnProtect = () =>
        {
            using var context = _factory.CreateDbContext();
            appliedAtProtect = context.Database.GetAppliedMigrations().Count();
        };

        await _initializer.InitializeAsync();

        Assert.Equal(4, appliedAtProtect);
        Assert.Equal(Path.Combine(Path.GetTempPath(), "tankatlas.db"), _protector.ProtectedPaths.Single());
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }

        base.Dispose(disposing);
    }
}
