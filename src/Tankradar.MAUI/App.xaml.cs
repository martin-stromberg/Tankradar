using Tankradar.MAUI.Services;

namespace Tankradar.MAUI;

/// <summary>
/// Einstiegspunkt der Anwendung „Tankatlas“; initialisiert AppShell und Theme-Unterstützung.
/// </summary>
public partial class App : Application
{
    private readonly AppConfiguration _appConfiguration;

    /// <summary>
    /// Erstellt die App, lädt die Konfiguration nach und richtet die System-Theme-Unterstützung ein.
    /// </summary>
    /// <param name="appConfiguration">Die per Dependency Injection bereitgestellte App-Konfiguration.</param>
    public App(AppConfiguration appConfiguration)
    {
        InitializeComponent();

        _appConfiguration = appConfiguration;
        _ = _appConfiguration.LoadFromSettingsFileAsync();

        UserAppTheme = AppTheme.Unspecified;
        RequestedThemeChanged += OnRequestedThemeChanged;
        CurrentTheme = RequestedTheme;
    }

    /// <summary>
    /// Das aktuell wirksame Farbschema (Light/Dark), das bei Systemwechsel aktualisiert wird.
    /// </summary>
    public AppTheme CurrentTheme { get; private set; }

    /// <summary>
    /// Erstellt das Hauptfenster mit der AppShell als Inhalt.
    /// </summary>
    /// <param name="activationState">Der Aktivierungszustand der Plattform.</param>
    /// <returns>Das neu erstellte Anwendungsfenster.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell()) { Title = AppConfiguration.AppDisplayName };
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        CurrentTheme = e.RequestedTheme;
    }
}
