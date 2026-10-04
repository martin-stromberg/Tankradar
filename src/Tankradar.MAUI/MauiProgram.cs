using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Services;
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

        builder.Services.AddTransient<FavoritesViewModel>();
        builder.Services.AddTransient<MapViewModel>();
        builder.Services.AddTransient<TankbookViewModel>();
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
}
