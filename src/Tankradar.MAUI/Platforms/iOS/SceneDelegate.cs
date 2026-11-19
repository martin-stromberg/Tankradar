using Foundation;

namespace Tankradar.MAUI;

/// <summary>
/// iOS-Scene-Delegate der Tankatlas-App. Ab dem iOS-27-SDK ist der
/// UIKit-Scene-Lifecycle Pflicht; .NET MAUI verdrahtet die Szene über
/// <c>UIApplicationSceneManifest</c> in der Info.plist.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
