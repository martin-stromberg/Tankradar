using Tankradar.MAUI.Data;
using Tankradar.MAUI.Services.Favorites;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testaufbau für Favoriten: echter <see cref="FavoritesService"/> auf In-Memory-SQLite mit angelegtem Schema und steuerbarer Uhr.
/// </summary>
public sealed class FavoritesFixture : IDisposable
{
    /// <summary>
    /// Die Kennung der ersten Teststation.
    /// </summary>
    public const string StationA = "00000001-0000-4000-8000-000000000000";

    /// <summary>
    /// Die Kennung der zweiten Teststation.
    /// </summary>
    public const string StationB = "00000002-0000-4000-8000-000000000000";

    /// <summary>
    /// Die Kennung der dritten Teststation.
    /// </summary>
    public const string StationC = "00000003-0000-4000-8000-000000000000";

    /// <summary>
    /// Legt Datenbank, Schema, Teststationen (Alpha, Beta, Citra) und Dienst an.
    /// </summary>
    public FavoritesFixture()
    {
        using (var context = Factory.CreateDbContext())
        {
            context.Database.EnsureCreated();
            context.Stations.AddRange(
                new StationEntity { Id = StationA, Name = "Alpha Tankstelle", Street = "Hauptstraße", HouseNumber = "1", PostCode = "10115", Place = "Berlin", Latitude = 52.52, Longitude = 13.40 },
                new StationEntity { Id = StationB, Name = "Beta Tankstelle", Latitude = 52.53, Longitude = 13.41 },
                new StationEntity { Id = StationC, Name = "Citra Tankstelle", Latitude = 52.54, Longitude = 13.42 });
            context.SaveChanges();
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
    /// Der Dienst auf der Datenbank.
    /// </summary>
    public FavoritesService Service { get; }

    /// <summary>
    /// Erstellt einen weiteren Dienst auf derselben Datenbank (simuliert einen Neustart der App).
    /// </summary>
    /// <returns>Der neue Dienst.</returns>
    public FavoritesService CreateService()
    {
        return new FavoritesService(Factory, new NoOpDatabaseInitializer(), Clock);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Factory.Dispose();
    }
}
