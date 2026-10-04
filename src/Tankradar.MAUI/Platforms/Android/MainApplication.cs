using Android.App;
using Android.Runtime;

namespace Tankradar.MAUI;

/// <summary>
/// Android-Anwendungsklasse der Tankatlas-App.
/// </summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <summary>
    /// Erstellt die <see cref="MainApplication"/> mit dem von Android übergebenen nativen Handle.
    /// </summary>
    /// <param name="handle">Das native JNI-Handle.</param>
    /// <param name="ownership">Die Besitzverhältnisse des JNI-Handles.</param>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <summary>
    /// Erstellt die gemeinsam genutzte <see cref="MauiApp"/>-Instanz über <see cref="MauiProgram"/>.
    /// </summary>
    /// <returns>Die gebaute <see cref="MauiApp"/>.</returns>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
