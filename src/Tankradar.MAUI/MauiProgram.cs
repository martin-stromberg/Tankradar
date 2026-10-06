using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
#endif
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Services;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Location;
using Tankradar.MAUI.Services.Map;
using Tankradar.MAUI.Services.Navigation;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.ViewModels;
using Tankradar.MAUI.Views;

namespace Tankradar.MAUI;

/// <summary>
/// Baut und konfiguriert die MAUI-App inklusive Dependency Injection, Schriftarten und Diensten.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Erstellt die konfigurierte <see cref="MauiApp"/>-Instanz für die App „Tankatlas“.
    /// </summary>
    /// <returns>Die gebaute <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Vollständige Font-Registrierung: OpenSans (Fallback-Schrift), Inter (Fließtext/Headlines)
                // und JetBrains Mono (Code-Darstellung), jeweils Regular- und SemiBold-Schnitt.
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-SemiBold.ttf", "InterSemibold");
                fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMonoRegular");
                fonts.AddFont("JetBrainsMono-SemiBold.ttf", "JetBrainsMonoSemibold");
            });

#if WINDOWS
        if (TestWindowMode.ShouldHideWindow(Environment.GetEnvironmentVariable))
        {
            // Testmodus mit Off-Screen-Betrieb: Fenster außerhalb des Bildschirms, ohne Vordergrundwechsel.
            builder.ConfigureLifecycleEvents(events => events.AddWindows(windows =>
                windows.OnWindowCreated(window => Platforms.Windows.OffscreenWindow.Apply(window))));
        }
#endif

        builder.Services.AddSingleton<AppConfiguration>();
        builder.Services.AddSingleton<IAppDataPathProvider, AppDataPathProvider>();
        builder.Services.AddSingleton<IDatabaseFileProtector, DatabaseFileProtector>();
        builder.Services.AddDbContextFactory<TankradarDbContext>((serviceProvider, options) =>
        {
            var dataDirectory = serviceProvider.GetRequiredService<IAppDataPathProvider>().GetDataDirectory();
            options.UseSqlite($"Data Source={TankradarDbContext.GetDatabasePath(dataDirectory)}");
        });
        builder.Services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        AddPriceServices(builder.Services);
        builder.Services.AddSingleton<IFavoritesService, FavoritesService>();
        AddGeocodingServices(builder.Services);
        AddMapServices(builder.Services);
        builder.Services.AddSingleton<ILocationService>(provider => LocationServiceSelector.Create(
            Environment.GetEnvironmentVariable,
            () => new MauiLocationService(provider.GetRequiredService<ILogger<MauiLocationService>>())));

        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<TankbookViewModel>();
        builder.Services.AddTransient<DataSourceViewModel>();
        builder.Services.AddTransient<StationDetailViewModel>();
        builder.Services.AddTransient<StationFavoritesViewModel>();
        builder.Services.AddTransient<FavoriteGroupViewModel>();
        builder.Services.AddSingleton<IStationNavigator, ShellStationNavigator>();
        builder.Services.AddSingleton<IFavoriteGroupNavigator, ShellFavoriteGroupNavigator>();
        builder.Services.AddTransient<SettingsViewModel>();

        builder.Services.AddTransient<FavoritesPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<TankbookPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<StationDetailPage>();
        builder.Services.AddTransient<FavoriteGroupPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void AddGeocodingServices(IServiceCollection services)
    {
        services.AddSingleton(_ => GeocodingOptions.FromEnvironment(Environment.GetEnvironmentVariable, AppIdentity.GetAppVersion()));
        services.AddSingleton<IGeocodingService>(provider =>
        {
            var options = provider.GetRequiredService<GeocodingOptions>();

            // Eigener HTTP-Client und eigene Drosselung: Die Nutzungsrichtlinie von Nominatim (höchstens eine Anfrage je Sekunde)
            // gilt unabhängig vom Preisdienst; keine automatischen Weiterleitungen, begrenzte Antwortgröße.
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = Timeout.InfiniteTimeSpan,
                MaxResponseContentBufferSize = 256 * 1024,
            };
            var throttle = new RequestThrottle(options.MinRequestInterval, provider.GetRequiredService<IDelay>(), provider.GetRequiredService<TimeProvider>());
            return new NominatimGeocodingService(client, options, throttle, provider.GetRequiredService<ILogger<NominatimGeocodingService>>());
        });
    }

    private static void AddMapServices(IServiceCollection services)
    {
        services.AddSingleton(_ => TileServerOptions.FromEnvironment(Environment.GetEnvironmentVariable, AppIdentity.GetAppVersion()));
        services.AddSingleton<ITileSource>(provider =>
        {
            var options = provider.GetRequiredService<TileServerOptions>();

            // Eigener HTTP-Client: keine automatischen Weiterleitungen, begrenzte Wartezeit; die Kennung wird je Anfrage gesetzt.
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
            var paths = provider.GetRequiredService<IAppDataPathProvider>();
            LegacyTileCache.DeleteInBackground(Path.Combine(paths.GetDataDirectory(), "tiles"), Path.Combine(paths.GetCacheDirectory(), "tiles"));
            return new HttpTileSource(
                client,
                options,
                () => Path.Combine(paths.GetCacheDirectory(), "tiles"),
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<ILogger<HttpTileSource>>());
        });
    }

    private static void AddPriceServices(IServiceCollection services)
    {
        services.AddSingleton(_ => PriceApiOptions.FromEnvironment(Environment.GetEnvironmentVariable));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IDelay, TaskDelay>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<PriceApiOptions>();
            return new RequestThrottle(options.MinRequestInterval, provider.GetRequiredService<IDelay>(), provider.GetRequiredService<TimeProvider>());
        });
        services.AddSingleton(_ =>
        {
            // Keine automatischen Weiterleitungen: Der Schlüssel steht in der Anfrageadresse und darf nie an einen anderen Host gehen.
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(AppIdentity.BuildUserAgent(AppIdentity.GetAppVersion()));
            return client;
        });
#if WINDOWS
        services.AddSingleton<IApiKeyStore>(_ => ApiKeyStoreSelector.Create(
            Environment.GetEnvironmentVariable,
            () => new Platforms.Windows.CredentialLockerApiKeyStore()));
#else
        services.AddSingleton<IApiKeyStore>(_ => ApiKeyStoreSelector.Create(
            Environment.GetEnvironmentVariable,
            () => new SecureStorageApiKeyStore()));
#endif
        services.AddSingleton<IApiKeyProvider>(provider => new ApiKeyProvider(
            provider.GetRequiredService<IApiKeyStore>(),
            ApiKeyProvider.ReadBuildTimeKey,
            Environment.GetEnvironmentVariable,
            provider.GetRequiredService<ILogger<ApiKeyProvider>>()));
        services.AddSingleton<ITankerkoenigClient, TankerkoenigClient>();
        services.AddSingleton<IPriceRepository, PriceRepository>();
        services.AddSingleton<INetworkStatusSource, MauiNetworkStatusSource>();
        services.AddSingleton<IConnectionMonitor, ConnectionMonitor>();
        services.AddSingleton<IFuelPriceService, FuelPriceService>();
    }
}
