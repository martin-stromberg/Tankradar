using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Dienst-Tests: echter lokaler Preis-Cache auf In-Memory-SQLite, ersetzter API-Client, Verbindungszustand und Uhr.
/// </summary>
public abstract class FuelPriceServiceTestBase : BaseTest
{
    private readonly InMemoryDbContextFactory _factory = new();

    /// <summary>
    /// Erstellt den Aufbau und legt das Schema an.
    /// </summary>
    protected FuelPriceServiceTestBase()
    {
        using (var context = _factory.CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        Repository = new PriceRepository(_factory, new NoOpDatabaseInitializer());
        Service = new FuelPriceService(
            Client,
            Repository,
            Connection,
            new PriceApiOptions(),
            Clock,
            NullLogger<FuelPriceService>.Instance);
    }

    /// <summary>
    /// Der ersetzte API-Client.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected StubTankerkoenigClient Client { get; } = new();

    /// <summary>
    /// Der steuerbare Verbindungszustand.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeConnectionMonitor Connection { get; } = new();

    /// <summary>
    /// Die Testuhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Der lokale Preis-Cache.
    /// </summary>
    protected PriceRepository Repository { get; }

    /// <summary>
    /// Der zu testende Dienst.
    /// </summary>
    protected FuelPriceService Service { get; }

    /// <summary>
    /// Alle Sorten als Filter.
    /// </summary>
    protected static IReadOnlyList<FuelType> AllFuels { get; } = [FuelType.SuperE5, FuelType.SuperE10, FuelType.Diesel];

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
