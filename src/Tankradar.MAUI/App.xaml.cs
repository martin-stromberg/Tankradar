using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Services;

namespace Tankradar.MAUI;

/// <summary>
/// Einstiegspunkt der Anwendung „Tankatlas“; initialisiert AppShell und Theme-Unterstützung.
/// </summary>
public partial class App : Application
{
    private readonly AppConfiguration _appConfiguration;
    private readonly Func<AppShell> _createShell;
    private readonly ILogger<App> _logger;

    /// <summary>
    /// Erstellt die App, lädt die Konfiguration nach, stößt die Datenbankinitialisierung an und richtet die System-Theme-Unterstützung ein.
    /// </summary>
    /// <param name="appConfiguration">Die per Dependency Injection bereitgestellte App-Konfiguration.</param>
    /// <param name="databaseInitializer">Initialisiert die lokale Datenbank im Hintergrund.</param>
    /// <param name="logger">Logger für Fehler der Hintergrundinitialisierung.</param>
    /// <param name="createShell">Erzeugt die <see cref="AppShell"/> samt ihren Abhängigkeiten aus der Dependency Injection.</param>
    public App(AppConfiguration appConfiguration, IDatabaseInitializer databaseInitializer, ILogger<App> logger, Func<AppShell> createShell)
    {
        InitializeComponent();

        _appConfiguration = appConfiguration;
        _createShell = createShell;
        _logger = logger;
        _ = _appConfiguration.LoadFromSettingsFileAsync();
        _ = InitializeDatabaseAsync(databaseInitializer);

        UserAppTheme = AppTheme.Unspecified;
        RequestedThemeChanged += OnRequestedThemeChanged;
        CurrentTheme = RequestedTheme;
    }

    /// <summary>
    /// Das aktuell wirksame Farbschema (Light/Dark), das bei Systemwechsel aktualisiert wird.
    /// </summary>
    public AppTheme CurrentTheme { get; private set; }

    /// <summary>
    /// Erstellt das Hauptfenster mit der AppShell als Inhalt; die Abhängigkeiten der Shell kommen aus der Dependency Injection.
    /// </summary>
    /// <param name="activationState">Der Aktivierungszustand der Plattform.</param>
    /// <returns>Das neu erstellte Anwendungsfenster.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_createShell()) { Title = AppConfiguration.AppDisplayName };
    }

    private async Task InitializeDatabaseAsync(IDatabaseInitializer databaseInitializer)
    {
        try
        {
            await databaseInitializer.InitializeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Die Datenbankinitialisierung beim App-Start ist fehlgeschlagen.");
        }
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        CurrentTheme = e.RequestedTheme;
    }
}
