using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testaufbau für Fahrzeuge: echter <see cref="VehicleService"/> auf In-Memory-SQLite mit angelegtem Schema, steuerbarer Uhr und aufzeichnendem Logger.
/// Vorgänge werden direkt als <see cref="TankbookEntryEntity"/> eingefügt, weil es in Schritt 12 keine Erfassung gibt.
/// </summary>
public sealed class VehicleFixture : IDisposable
{
    /// <summary>
    /// Legt Datenbank, Schema und Dienst an.
    /// </summary>
    public VehicleFixture()
    {
        using (var context = Factory.CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        Service = CreateService();
    }

    /// <summary>
    /// Die Kontext-Factory der Datenbank.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public InMemoryDbContextFactory Factory { get; } = new();

    /// <summary>
    /// Die Testuhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Der Logger aller von dieser Umgebung erstellten Dienste.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public RecordingLogger<VehicleService> Logger { get; } = new();

    /// <summary>
    /// Der Dienst auf der Datenbank.
    /// </summary>
    public VehicleService Service { get; }

    /// <summary>
    /// Erstellt einen weiteren Dienst auf derselben Datenbank (simuliert einen Neustart der App).
    /// </summary>
    /// <returns>Der neue Dienst.</returns>
    public VehicleService CreateService()
    {
        return new VehicleService(Factory, new NoOpDatabaseInitializer(), Clock, Logger);
    }

    /// <summary>
    /// Fügt einem Fahrzeug Tank- bzw. Ladevorgänge hinzu.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="count">Die Zahl der Vorgänge.</param>
    /// <returns>Ein Task, der nach dem Speichern abgeschlossen ist.</returns>
    public async Task AddEntriesAsync(long vehicleId, int count)
    {
        await using var context = Factory.CreateDbContext();
        for (var i = 0; i < count; i++)
        {
            context.TankbookEntries.Add(new TankbookEntryEntity { VehicleId = vehicleId, CreatedUtc = Clock.UtcNow });
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Zählt alle Vorgänge aller Fahrzeuge.
    /// </summary>
    /// <returns>Die Zahl der Vorgänge.</returns>
    public async Task<int> CountAllEntriesAsync()
    {
        await using var context = Factory.CreateDbContext();
        return await context.TankbookEntries.CountAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Factory.Dispose();
    }
}
