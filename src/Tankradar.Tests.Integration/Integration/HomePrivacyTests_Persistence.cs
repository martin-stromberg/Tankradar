using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass die Startseite mit „In der Nähe“ den Standort weder in der Datenbank speichert noch protokolliert.
/// </summary>
public class HomePrivacyTests_Persistence : SearchMockServerTestBase
{
    private static readonly string[] PositionFragments = ["52.5123", "13.4123", "52,5123", "13,4123"];

    private readonly List<string> _homeLog = [];

    private async Task OpenHomeWithNearbyAsync()
    {
        var search = CreateViewModel(CreateService());
        search.OnAppearing();
        await search.LastSettingsTask;
        await search.SearchAsync();

        var factory = Database.CreateFactory();
        var initializer = Database.CreateInitializer(factory);
        var favorites = new FavoritesService(factory, initializer, Clock);
        var created = await favorites.AddStationToNewGroupAsync("Arbeitsweg", MockTankerkoenigServer.StationAlpha);
        Assert.NotNull(created.Group);

        var settingsService = new SettingsService(factory, initializer);
        var settings = await settingsService.LoadAsync();
        await settingsService.SaveAsync(settings with { ShowNearby = true });

        var priceService = CreateService();
        var nearby = new NearbySectionViewModel(priceService, new NullStationNavigator(), Clock, new RecordingLogger<NearbySectionViewModel>(_homeLog));
        var home = new FavoritesViewModel(
            favorites,
            new NullGroupNavigator(),
            settingsService,
            priceService,
            new TestLocationService(new GeoPosition(SearchLatitude, SearchLongitude)),
            Connection,
            new NullStationNavigator(),
            Clock,
            nearby,
            new RecordingLogger<FavoritesViewModel>(_homeLog));

        home.OnAppearing();
        await home.LastTask;

        Assert.True(nearby.IsVisible);
        Assert.NotEmpty(nearby.Entries);
        Assert.True(home.Groups[0].Stations[0].HasDistance);
        home.OnDisappearing();
    }

    /// <summary>
    /// Prüft, dass nach dem Öffnen der Startseite weder Datenbankzeilen noch die Datenbankdatei die Position enthalten.
    /// </summary>
    [Fact]
    public async Task AfterHomeWithNearby_DatabaseDoesNotContainPosition()
    {
        await OpenHomeWithNearbyAsync();

        var factory = Database.CreateFactory();
        await using (var context = factory.CreateDbContext())
        {
            var rows = JsonSerializer.Serialize(new
            {
                Stations = await context.Stations.AsNoTracking().ToListAsync(),
                Settings = await context.UserSettings.AsNoTracking().ToListAsync(),
                Entries = await context.FavoriteEntries.AsNoTracking().ToListAsync(),
                Groups = await context.FavoriteGroups.AsNoTracking().ToListAsync(),
            });
            foreach (var fragment in PositionFragments)
            {
                Assert.DoesNotContain(fragment, rows, StringComparison.Ordinal);
            }
        }

        await using var stream = new FileStream(Database.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);
        var asText = Encoding.Latin1.GetString(bytes);
        foreach (var fragment in PositionFragments)
        {
            Assert.DoesNotContain(fragment, asText, StringComparison.Ordinal);
        }

        Assert.True(bytes.AsSpan().IndexOf(BitConverter.GetBytes(SearchLatitude)) < 0);
        Assert.True(bytes.AsSpan().IndexOf(BitConverter.GetBytes(SearchLongitude)) < 0);
    }

    /// <summary>
    /// Prüft, dass Startseite und Bereich „In der Nähe“ weder Koordinaten noch Anfrageparameter protokollieren.
    /// </summary>
    [Fact]
    public async Task HomeWithNearby_LogsNoPositionOrRequestAddress()
    {
        await OpenHomeWithNearbyAsync();

        var log = LogText + "\n" + string.Join('\n', _homeLog);
        foreach (var fragment in (string[])[.. PositionFragments, "lat=", "lng=", "rad=", "list.php", "prices.php"])
        {
            Assert.DoesNotContain(fragment, log, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class NullGroupNavigator : IFavoriteGroupNavigator
    {
        public Task OpenGroupAsync(long groupId)
        {
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }
    }
}
