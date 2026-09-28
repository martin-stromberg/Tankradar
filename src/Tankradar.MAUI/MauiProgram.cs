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
    /// Erstellt die konfigurierte <see cref="MauiApp"/>-Instanz für Tankradar.
    /// </summary>
    /// <returns>Die gebaute <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Inter/JetBrains Mono sind als Ziel-Schriftarten vorgesehen; ohne verfügbare TTF-Dateien
                // (siehe README, Abschnitt "Bekannte Einschränkungen") wird auf die mitgelieferten
                // OpenSans-Schriften bzw. die Plattform-Systemschrift zurückgegriffen.
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AppConfiguration>();

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
