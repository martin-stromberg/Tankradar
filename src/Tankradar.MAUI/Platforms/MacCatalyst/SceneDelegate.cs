using Foundation;

namespace Tankradar.MAUI;

/// <summary>
/// MacCatalyst-Scene-Delegate der Tankatlas-App. Ab dem MacCatalyst-27-SDK
/// ist der UIKit-Scene-Lifecycle Pflicht; .NET MAUI verdrahtet die Szene
/// über <c>UIApplicationSceneManifest</c> in der Info.plist.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
