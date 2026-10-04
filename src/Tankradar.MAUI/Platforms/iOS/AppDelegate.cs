using Foundation;

namespace Tankradar.MAUI;

/// <summary>
/// iOS-App-Delegate der Tankatlas-App.
/// </summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>
    /// Erstellt die gemeinsam genutzte <see cref="MauiApp"/>-Instanz über <see cref="MauiProgram"/>.
    /// </summary>
    /// <returns>Die gebaute <see cref="MauiApp"/>.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
