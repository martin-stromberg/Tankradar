using Tankradar.MAUI.Models.Search;
using EssentialsMap = Microsoft.Maui.ApplicationModel.Map;
using MauiLocation = Microsoft.Maui.Devices.Sensors.Location;

namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Öffnet die Karten-App des Geräts mit der Tankstelle als Ziel (nur auf ausdrücklichen Wunsch des Anwenders).
/// </summary>
public interface IMapOpener
{
    /// <summary>
    /// Öffnet die Karten-App mit einer Position als Ziel im Routenmodus (Auto).
    /// </summary>
    /// <param name="position">Die Zielposition.</param>
    /// <param name="name">Der Anzeigename des Ziels in der Karten-App.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenAsync(GeoPosition position, string name);

    /// <summary>
    /// Öffnet die Karten-App mit einer Adresse als Ziel im Routenmodus (Auto); das Geocoding übernimmt die Karten-App.
    /// </summary>
    /// <param name="address">Die Adresszeile des Ziels.</param>
    /// <param name="name">Der Anzeigename des Ziels in der Karten-App.</param>
    /// <returns>Ein Task, der nach dem Öffnen abgeschlossen ist.</returns>
    Task OpenAsync(string address, string name);
}

/// <summary>
/// Öffnet die Karten-App über <see cref="EssentialsMap"/> aus .NET MAUI Essentials; im Testmodus wird nie eine externe App gestartet.
/// </summary>
public sealed class EssentialsMapOpener : IMapOpener
{
    private readonly bool _testMode;

    /// <summary>
    /// Erstellt den Öffner.
    /// </summary>
    /// <param name="testMode">Gibt an, ob der Testmodus gilt (dann geschieht nichts).</param>
    public EssentialsMapOpener(bool testMode)
    {
        _testMode = testMode;
    }

    /// <inheritdoc />
    public async Task OpenAsync(GeoPosition position, string name)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(name);
        if (_testMode)
        {
            return;
        }

        var options = new MapLaunchOptions { Name = name, NavigationMode = NavigationMode.Driving };
        await EssentialsMap.Default.OpenAsync(new MauiLocation(position.Latitude, position.Longitude), options).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public async Task OpenAsync(string address, string name)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(name);
        if (_testMode || address.Trim().Length == 0)
        {
            return;
        }

        var placemark = new Placemark { Thoroughfare = address };
        var options = new MapLaunchOptions { Name = name, NavigationMode = NavigationMode.Driving };
        await EssentialsMap.Default.OpenAsync(placemark, options).ConfigureAwait(true);
    }
}
