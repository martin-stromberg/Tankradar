using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Services;
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

        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<TankbookViewModel>();
        builder.Services.AddTransient<DataSourceViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        builder.Services.AddTransient<FavoritesPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<TankbookPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
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
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Tankatlas/0.1");
            return client;
        });
#if WINDOWS
        services.AddSingleton<IApiKeyStore, Platforms.Windows.CredentialLockerApiKeyStore>();
#else
        services.AddSingleton<IApiKeyStore, SecureStorageApiKeyStore>();
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
